using System;
using System.Net;
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
    public class UserLoginActor : InterfacedActor, IUserLogin
    {
        private readonly ILog _logger;
        private readonly ClusterNodeContext _clusterContext;
        private readonly ActorBoundChannelRef _channel;

        public UserLoginActor(ClusterNodeContext clusterContext,
                              ActorBoundChannelRef channel,
                              EndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger($"UserLoginActor({clientRemoteEndPoint})");
            _clusterContext = clusterContext;
            _channel = channel;
        }

        async Task<LoginResult> IUserLogin.Login(IUserEventObserver observer)
        {
            // create user context

            var userContext = new TrackableUserContext
            {
                Data = new TrackableUserData
                {
                    Nickname = "",
                    RegisterTime = DateTime.UtcNow,
                },
                Notes = new TrackableDictionary<int, string>()
            };
            userContext.SetDefaultTracker();

            // make UserActor

            var userId = CreateUserId();
            IActorRef user;
            try
            {
                user = Context.ActorOf(
                    Props.Create(() => new UserActor(_clusterContext, _channel, userId, userContext, observer)),
                    "user_" + userId);
            }
            catch (Exception e)
            {
                _logger.Error($"Exception in creating UserActor({userId})", e);
                throw new ResultException(ResultCodeType.LoginFailed);
            }

            // register User in UserTable

            var reply = await _clusterContext.UserTableContainer.Add(userId, user);
            if (reply == null || reply.Added == false)
            {
                _logger.Error($"Failed in registering user to user-table. ({userId})");
                user.Tell(PoisonPill.Instance);
                throw new ResultException(ResultCodeType.LoginFailed);
            }

            // bind user actor with client session, which makes client to communicate with this actor.

            var boundActor = await _channel.BindActor(user.Cast<UserRef>(), ActorBindingFlags.CloseThenStop | ActorBindingFlags.StopThenCloseChannel);
            return new LoginResult { UserId = userId, UserContext = userContext, User = boundActor.Cast<UserRef>() };
        }

        private long CreateUserId()
        {
            // a native int64 unique value generator.
            // if you want to get a strong one, consider 128 bits key or external unique id generator.

            var scratches = new byte[8];
            var bytes = Guid.NewGuid().ToByteArray();
            for (var i = 0; i < bytes.Length; i++)
                scratches[i % scratches.Length] ^= bytes[i];
            return BitConverter.ToInt64(scratches, 0);
        }
    }
}
