using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Domain;
using Xunit;
using Xunit.Abstractions;

namespace GameServer
{
    public class UserLoginActorTest : TestKit, IClassFixture<ClusterContextFixture>, IClassFixture<RedisStorageFixture>
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
            // Act
            await _client.LoginAsync();

            // Assert
            Assert.NotEqual(0, _client.UserId);
            Assert.Equal(_client.Channel.GetBoundActorRef(_client.UserInitiator),
                         (await _clusterContext.UserTable.Get(_client.UserId)).Actor);
        }

        [Fact]
        public async Task UserLogin_WrongCredential_Fail()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _client.LoginAsync("WRONG"));

            // Assert
            Assert.Equal(ResultCodeType.LoginCredentialError, ((ResultException)exception).ResultCode);
        }
    }
}
