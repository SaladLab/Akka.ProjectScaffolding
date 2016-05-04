using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain.Data;
using Domain.Interface;

namespace GameServer
{
    [Log]
    public class UserActor : InterfacedActor<UserActor>, IUser
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;
        private long _id;
        private TrackableUserContext _userContext;
        private UserEventObserver _userEventObserver;

        public UserActor(ClusterNodeContext clusterContext, IActorRef clientSession,
                         long id, TrackableUserContext userContext, int observerId)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _clientSession = clientSession;
            _id = id;
            _userContext = userContext;
            _userEventObserver = new UserEventObserver(clientSession, observerId);
        }

        [MessageHandler]
        protected void OnMessage(ActorBoundSessionMessage.SessionTerminated message)
        {
            Context.Stop(Self);
        }

        Task IUser.SetNickname(string nickname)
        {
            _userContext.Data.Nickname = nickname;
            FlushUserContext();
            return Task.CompletedTask;
        }

        Task IUser.AddNote(int id, string note)
        {
            _userContext.Notes.Add(id, note);
            FlushUserContext();
            return Task.CompletedTask;
        }

        Task IUser.RemoveNote(int id)
        {
            _userContext.Notes.Remove(id);
            FlushUserContext();
            return Task.CompletedTask;
        }

        private void FlushUserContext()
        {
            _userEventObserver.UserContextChange(_userContext.Tracker);
            _userContext.Tracker = new TrackableUserContextTracker();
        }
    }
}
