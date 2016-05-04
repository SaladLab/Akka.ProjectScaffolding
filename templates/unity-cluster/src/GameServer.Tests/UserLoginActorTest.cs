using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Domain.Interface;
using Xunit;

namespace GameServer.Tests
{
    public class UserLoginActorTest : TestKit, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private TestActorRef<TestActorBoundSession> _clientSession;

        public UserLoginActorTest(ClusterContextFixture clusterContextFixture)
        {
            clusterContextFixture.Initialize(Sys);
            _clusterContext = clusterContextFixture.Context;
        }

        private UserLoginRef CreateUserLogin()
        {
            var system = _clusterContext.System;

            _clientSession = new TestActorRef<TestActorBoundSession>(
                system, Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            return new UserLoginRef(null, _clientSession.UnderlyingActor.GetRequestWaiter(1), null);
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context)
        {
            return new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(
                        () => new UserLoginActor(_clusterContext, context.Self, new IPEndPoint(0, 0)))),
                    typeof(IUserLogin))
            };
        }

        [Fact]
        public async Task Test_UserLogin_Succeed()
        {
            var userLogin = CreateUserLogin();

            var observer = _clientSession.UnderlyingActor.AddTestObserver();
            var ret = await userLogin.Login(observer.Id);

            Assert.NotEqual(0, ret.UserId);
            Assert.NotEqual(0, ret.UserActorBindId);
            Assert.NotNull(ret.UserContext);
            Assert.NotNull(ret.UserContext.Data);
            Assert.NotNull(ret.UserContext.Notes);

            var tableRet = await _clusterContext.UserTable.Ask<DistributedActorTableMessage<long>.GetReply>(
                new DistributedActorTableMessage<long>.Get(ret.UserId));
            Assert.NotNull(tableRet.Actor);
        }
    }
}
