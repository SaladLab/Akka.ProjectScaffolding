using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.TestKit.Xunit2;
using Domain.Data;
using Domain.Interface;
using Xunit;

namespace GameServer.Tests
{
    public class UserActorTest : TestKit, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private MockClient _client;
        private UserRef _user => _client.User;
        private TrackableUserContext _userContext => _client.UserContext;

        public UserActorTest(ClusterContextFixture clusterContextFixture)
        {
            clusterContextFixture.Initialize(Sys);
            _clusterContext = clusterContextFixture.Context;
            _client = new MockClient(_clusterContext);
        }

        [Fact]
        public async Task Test_UserDisconnect_ActorStopped()
        {
            var ret = await _client.LoginAsync();
            var userActor = _client.ClientSession.GetBoundActorRef((UserRef)ret.User);

            Watch(userActor);

            _client.ClientSessionActor.Tell(PoisonPill.Instance);

            ExpectTerminated(userActor);

            var tableRet = await _clusterContext.UserTable.Ask<DistributedActorTableMessage<long>.GetReply>(
                new DistributedActorTableMessage<long>.Get(_client.UserId));
            Assert.Null(tableRet.Actor);
        }

        [Fact]
        public async Task Test_SetNickname_Succeed()
        {
            await _client.LoginAsync();

            await _user.SetNickname("SuperPower");

            Assert.Equal("SuperPower", _userContext.Data.Nickname);
        }

        [Fact]
        public async Task Test_SetNickname_Fail()
        {
            await _client.LoginAsync();

            var e = await Record.ExceptionAsync(() => _user.SetNickname(null));
            var r = e as ResultException;
            Assert.NotNull(e);
            Assert.Equal(ResultCodeType.NicknameInvalid, r.ResultCode);
        }

        [Fact]
        public async Task Test_AddNote_Succeed()
        {
            await _client.LoginAsync();

            await _user.AddNote(1, "One");
            await _user.AddNote(2, "Two");

            Assert.Equal(2, _userContext.Notes.Count);
            Assert.Equal("One", _userContext.Notes[1]);
            Assert.Equal("Two", _userContext.Notes[2]);
        }

        [Fact]
        public async Task Test_AddNote_Fail()
        {
            await _client.LoginAsync();

            await _user.AddNote(1, "One");

            var e = await Record.ExceptionAsync(() => _user.AddNote(1, "One2"));
            var r = e as ResultException;
            Assert.NotNull(e);
            Assert.Equal(ResultCodeType.NoteDuplicate, r.ResultCode);
        }

        [Fact]
        public async Task Test_RemoveNote_Succeed()
        {
            await _client.LoginAsync();

            await _user.AddNote(1, "One");
            await _user.AddNote(2, "Two");
            await _user.RemoveNote(2);

            Assert.Equal(1, _userContext.Notes.Count);
            Assert.Equal("One", _userContext.Notes[1]);
        }

        [Fact]
        public async Task Test_RemoveNote_Fail()
        {
            await _client.LoginAsync();

            var e = await Record.ExceptionAsync(() => _user.RemoveNote(1));
            var r = e as ResultException;
            Assert.NotNull(e);
            Assert.Equal(ResultCodeType.NoteNotFound, r.ResultCode);
        }
    }
}
