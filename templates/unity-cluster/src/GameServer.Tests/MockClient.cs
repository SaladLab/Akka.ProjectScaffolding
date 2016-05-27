using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Domain.Data;
using Domain.Interface;
using Akka.Interfaced;

namespace GameServer.Tests
{
    public class MockClient : IUserEventObserver
    {
        private ClusterNodeContext _clusterContext;
        private TestActorRef<TestActorBoundSession> _clientSession;
        private UserLoginRef _userLogin;
        private long _userId;
        private UserRef _user;
        private TestObserver _userEventObserver;
        private TrackableUserContext _userContext;

        public TestActorRef<TestActorBoundSession> ClientSession
        {
            get { return _clientSession; }
        }

        public UserLoginRef UserLogin
        {
            get { return _userLogin; }
        }

        public long UserId
        {
            get { return _userId; }
        }

        public UserRef User
        {
            get { return _user; }
        }

        public TestObserver UserEventObserver
        {
            get { return _userEventObserver; }
        }

        public TrackableUserContext UserContext
        {
            get { return _userContext; }
        }

        public MockClient(ClusterNodeContext clusterContex)
        {
            _clusterContext = clusterContex;
            _clientSession = new TestActorRef<TestActorBoundSession>(
                _clusterContext.System,
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context)
        {
            var actor = context.ActorOf(Props.Create(
                () => new UserLoginActor(_clusterContext, context.Self, new IPEndPoint(0, 0))));

            _userLogin = new UserLoginRef(actor);

            return new[] { Tuple.Create(actor, typeof(IUserLogin)) };
        }

        public async Task<LoginResult> LoginAsync()
        {
            if (_user != null)
                throw new InvalidOperationException("Already logined");

            _userEventObserver = _clientSession.UnderlyingActor.AddTestObserver();
            _userEventObserver.Notified += OnUserEvent;

            var observer = new UserEventObserver(_clientSession, _userEventObserver.Id);
            var ret = await _userLogin.Login(observer);

            var actorId = ((BoundActorRef)(((UserRef)ret.User).Actor)).Id;
            _userId = ret.UserId;
            _user = new UserRef(null, _clientSession.UnderlyingActor.GetRequestWaiter(actorId), null);
            _userContext = new TrackableUserContext();

            return ret;
        }

        private void OnUserEvent(IInvokable e)
        {
            e.Invoke(this);
        }

        void IUserEventObserver.UserContextChange(TrackableUserContextTracker userContextTracker)
        {
            userContextTracker.ApplyTo(_userContext);
        }
    }
}
