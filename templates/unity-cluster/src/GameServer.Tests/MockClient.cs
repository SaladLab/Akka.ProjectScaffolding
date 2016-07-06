using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Domain;

namespace GameServer
{
    public class MockClient : IUserEventObserver
    {
        private ClusterNodeContext _clusterContext;
        private UserEventObserver _userEventObserver;

        public TestActorBoundChannel Channel { get; private set; }
        public ActorBoundChannelRef ChannelRef { get; private set; }
        public UserLoginRef UserLogin { get; private set; }
        public long UserId { get; private set; }
        public UserRef User { get; private set; }
        public TrackableUserContext UserContext { get; private set; }

        public MockClient(ClusterNodeContext clusterContex)
        {
            _clusterContext = clusterContex;

            var channel = new TestActorRef<TestActorBoundChannel>(
                _clusterContext.System,
                Props.Create(() => new TestActorBoundChannel(CreateInitialActor)));
            Channel = channel.UnderlyingActor;
            ChannelRef = channel.Cast<ActorBoundChannelRef>();

            UserLogin = Channel.CreateRef<UserLoginRef>();
        }

        private Tuple<IActorRef, TaggedType[], ActorBindingFlags>[] CreateInitialActor(IActorContext context) =>
            new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(() =>
                        new UserLoginActor(_clusterContext, context.Self.Cast<ActorBoundChannelRef>(), new IPEndPoint(IPAddress.None, 0)))),
                    new TaggedType[] { typeof(IUserLogin) },
                    (ActorBindingFlags)0)
            };

        public async Task<LoginResult> LoginAsync()
        {
            if (User != null)
                throw new InvalidOperationException("Already logined");

            _userEventObserver = (UserEventObserver)Channel.CreateObserver<IUserEventObserver>(this);

            var ret = await UserLogin.Login(_userEventObserver);
            UserId = ret.UserId;
            User = (UserRef)ret.User;
            UserContext = new TrackableUserContext();
            return ret;
        }

        void IUserEventObserver.UserContextChange(TrackableUserContextTracker userContextTracker)
        {
            // this method is called by a worker thread of TestActorBoundSession actor
            // which is not same with with a test thread but invocation is serialized.
            // so if you access _userContext carefully, it could be safe :)
            userContextTracker.ApplyTo(UserContext);
        }
    }
}
