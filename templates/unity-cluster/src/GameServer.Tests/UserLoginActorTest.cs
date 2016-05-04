using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.TestKit.Xunit2;
using Xunit;

namespace GameServer.Tests
{
    public class UserLoginActorTest : TestKit, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private MockClient _client;

        public UserLoginActorTest(ClusterContextFixture clusterContextFixture)
        {
            clusterContextFixture.Initialize(Sys);
            _clusterContext = clusterContextFixture.Context;
            _client = new MockClient(_clusterContext);
        }

        [Fact]
        public async Task Test_UserLogin_Succeed()
        {
            var ret = await _client.LoginAsync();

            Assert.NotEqual(0, _client.UserId);
            Assert.NotNull(_client.User);
            Assert.NotNull(_client.UserContext);
            Assert.NotNull(_client.UserContext.Data);
            Assert.NotNull(_client.UserContext.Notes);

            var tableRet = await _clusterContext.UserTable.Ask<DistributedActorTableMessage<long>.GetReply>(
                new DistributedActorTableMessage<long>.Get(_client.UserId));
            Assert.Equal(_client.ClientSession.UnderlyingActor.GetBoundActorRef(ret.UserActorBindId).Path,
                         tableRet.Actor.Path);
        }

        [Fact]
        public void Test_UserDisconnect_ActorStopped()
        {
            Watch(_client.UserLogin.Actor);

            _client.ClientSession.Tell(PoisonPill.Instance);

            ExpectTerminated(_client.UserLogin.Actor);
        }
    }
}
