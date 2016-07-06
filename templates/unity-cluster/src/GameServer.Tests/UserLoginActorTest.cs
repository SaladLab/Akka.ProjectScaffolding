using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Domain;
using Xunit;
using Xunit.Abstractions;

namespace GameServer
{
    public class UserLoginActorTest : TestKit, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private MockClient _client;

        public UserLoginActorTest(ITestOutputHelper output, ClusterContextFixture clusterContextFixture)
            : base(output: output)
        {
            clusterContextFixture.Initialize(Sys);
            _clusterContext = clusterContextFixture.Context;
            _client = new MockClient(_clusterContext);
        }

        [Fact]
        public async Task UserLogin_Succeed()
        {
            var ret = await _client.LoginAsync();

            Assert.NotEqual(0, _client.UserId);
            Assert.NotNull(_client.User);
            Assert.NotNull(_client.UserContext);
            Assert.NotNull(_client.UserContext.Data);
            Assert.NotNull(_client.UserContext.Notes);

            var tableRet = await _clusterContext.UserTable.Get(_client.UserId);
            Assert.Equal(_client.Channel.GetBoundActorRef((UserRef)ret.User),
                         tableRet.Actor);
        }
    }
}
