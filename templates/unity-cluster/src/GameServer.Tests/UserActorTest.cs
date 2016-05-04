using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Domain.Interface;
using Xunit;

namespace GameServer.Tests
{
    public class UserActorTest : TestKit, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private MockClient _client;

        public UserActorTest(ClusterContextFixture clusterContextFixture)
        {
            clusterContextFixture.Initialize(Sys);
            _clusterContext = clusterContextFixture.Context;
            _client = new MockClient(_clusterContext);
        }

        [Fact]
        public async Task Test_SetNickname()
        {
            await _client.LoginAsync();

            await _client.User.SetNickname("SuperPower");

            Assert.Equal("SuperPower", _client.UserContext.Data.Nickname);
        }

        [Fact]
        public async Task Test_AddNote()
        {
            await _client.LoginAsync();

            await _client.User.AddNote(1, "One");
            await _client.User.AddNote(2, "Two");

            Assert.Equal(2, _client.UserContext.Notes.Count);
            Assert.Equal("One", _client.UserContext.Notes[1]);
            Assert.Equal("Two", _client.UserContext.Notes[2]);
        }

        [Fact]
        public async Task Test_RemoveNote()
        {
            await _client.LoginAsync();

            await _client.User.AddNote(1, "One");
            await _client.User.AddNote(2, "Two");
            await _client.User.RemoveNote(2);

            Assert.Equal(1, _client.UserContext.Notes.Count);
            Assert.Equal("One", _client.UserContext.Notes[1]);
        }
    }
}
