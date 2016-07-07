using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimServer;
using Common.Logging;
using Domain;
using TrackableData;

namespace GameServer
{
    [Log]
    [ResponsiveException(typeof(ResultException))]
    public class UserActor : InterfacedActor, IActorBoundChannelObserver, IUserInitiator, IUser
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private ActorBoundChannelRef _channel;
        private long _id;
        private TrackableUserContext _userContext;
        private TrackableUserContextTracker _userContextSaveTracker;
        private UserEventObserver _userEventObserver;

        public UserActor(ClusterNodeContext clusterContext, long id)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _id = id;
        }

        protected override Task OnGracefulStop()
        {
            return SaveUserContextChangeToDb();
        }

        void IActorBoundChannelObserver.ChannelOpen(IActorBoundChannel channel, object tag)
        {
            _channel = (ActorBoundChannelRef)channel;
        }

        void IActorBoundChannelObserver.ChannelOpenTimeout(object tag)
        {
            Self.Tell(InterfacedPoisonPill.Instance);
        }

        void IActorBoundChannelObserver.ChannelClose(IActorBoundChannel channel, object tag)
        {
            _channel = null;
        }

        private void FlushUserContext()
        {
            if (_userEventObserver != null)
                _userEventObserver.UserContextChange(_userContext.Tracker);

            _userContext.Tracker.ApplyTo(_userContextSaveTracker);
            _userContext.Tracker = new TrackableUserContextTracker();
        }

        private Task SaveUserContextChangeToDb()
        {
            return (_userContextSaveTracker != null && _userContextSaveTracker.HasChange)
                ? RedisStorage.UserContextMapper.SaveAsync(RedisStorage.Db, _userContextSaveTracker, _userContext, "User_" + _id)
                : Task.CompletedTask;
        }

        async Task<TrackableUserContext> IUserInitiator.Create(IUserEventObserver observer, string nickname)
        {
            // create context

            var userContext = new TrackableUserContext
            {
                Data = new TrackableUserData
                {
                    Nickname = nickname,
                    RegisterTime = DateTime.UtcNow,
                },
                Notes = new TrackableDictionary<int, string>()
            };
            await RedisStorage.UserContextMapper.CreateAsync(RedisStorage.Db, userContext, "User_" + _id);

            await OnUserInitiated(userContext, observer);
            return userContext;
        }

        async Task<TrackableUserContext> IUserInitiator.Load(IUserEventObserver observer)
        {
            // load context

            var userContext = (TrackableUserContext)(await RedisStorage.UserContextMapper.LoadAsync(RedisStorage.Db, "User_" + _id));
            if (userContext == null)
                throw new ResultException(ResultCodeType.UserNeedToBeCreated);

            await OnUserInitiated(userContext, observer);
            return userContext;
        }

        private async Task OnUserInitiated(TrackableUserContext userContext, IUserEventObserver observer)
        {
            userContext.SetDefaultTracker();

            _userContext = userContext;
            _userContextSaveTracker = new TrackableUserContextTracker();
            _userEventObserver = (UserEventObserver)observer;

            _channel.WithNoReply().UnbindType(Self, new[] { typeof(IUserInitiator) });
            await _channel.BindType(Self, new TaggedType[] { typeof(IUser) });
        }

        Task IUser.SetNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
                throw new ResultException(ResultCodeType.NicknameInvalid);

            _userContext.Data.Nickname = nickname;
            FlushUserContext();

            return Task.CompletedTask;
        }

        Task IUser.AddNote(int id, string note)
        {
            if (string.IsNullOrEmpty(note))
                throw new ResultException(ResultCodeType.NicknameInvalid);

            if (_userContext.Notes.ContainsKey(id))
                throw new ResultException(ResultCodeType.NoteDuplicate);

            _userContext.Notes.Add(id, note);
            FlushUserContext();

            return Task.CompletedTask;
        }

        Task IUser.RemoveNote(int id)
        {
            if (_userContext.Notes.Remove(id) == false)
                throw new ResultException(ResultCodeType.NoteNotFound);

            FlushUserContext();

            return Task.CompletedTask;
        }
    }
}
