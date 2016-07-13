using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using Domain;
using Xunit;
using Xunit.Abstractions;

namespace GameServer
{
    public class UserActorTest : TestKit, IClassFixture<ClusterContextFixture>, IClassFixture<RedisStorageFixture>
    {
        private ClusterNodeContext _clusterContext;
        private MockClient _client;
        private UserRef _user => _client.User;
        private TrackableUserContext _userContext => _client.UserContext;

        public UserActorTest(ITestOutputHelper output, ClusterContextFixture clusterContextFixture)
            : base(output: output)
        {
            clusterContextFixture.Initialize(Sys);
            _clusterContext = clusterContextFixture.Context;
            _client = new MockClient(_clusterContext);
        }

        [Fact]
        public async Task UserCreate()
        {
            // Act
            await _client.PrepareUserAsync();

            // Assert
            Assert.Equal("Created", _userContext.Data.Nickname);
        }

        [Fact]
        public async Task UserCreateAndLoad()
        {
            // Arrange
            await _client.PrepareUserAsync();
            _client.ChannelRef.WithNoReply().Close();
            var loginCredential = _client.LoginCredential;
            var registerTime = _client.UserContext.Data.RegisterTime;
            await Task.Delay(100);

            // Act
            _client = new MockClient(_clusterContext);
            await _client.PrepareUserAsync(loginCredential);

            // Assert
            Assert.Equal(registerTime, _userContext.Data.RegisterTime);
        }

        [Fact]
        public async Task ChannelClose_UserActorStopped()
        {
            await _client.PrepareUserAsync();
            var userActor = _client.Channel.GetBoundActorRef(_client.User);

            Watch(userActor);

            _client.ChannelRef.WithNoReply().Close();

            ExpectTerminated(userActor);
            await Task.Delay(100);

            var tableRet = await _clusterContext.UserTable.Get(_client.UserId);
            Assert.Null(tableRet.Actor);
        }

        [Fact]
        public async Task SetNickname_Succeed()
        {
            await _client.PrepareUserAsync();

            await _user.SetNickname("SuperPower");

            Assert.Equal("SuperPower", _userContext.Data.Nickname);
        }

        [Fact]
        public async Task SetNickname_Fail()
        {
            await _client.PrepareUserAsync();

            var e = await Record.ExceptionAsync(() => _user.SetNickname(null));
            var r = e as ResultException;
            Assert.NotNull(e);
            Assert.Equal(ResultCodeType.NicknameInvalid, r.ResultCode);
        }

        [Fact]
        public async Task AddNote_Succeed()
        {
            await _client.PrepareUserAsync();

            await _user.AddNote(1, "One");
            await _user.AddNote(2, "Two");

            Assert.Equal(2, _userContext.Notes.Count);
            Assert.Equal("One", _userContext.Notes[1]);
            Assert.Equal("Two", _userContext.Notes[2]);
        }

        [Fact]
        public async Task AddNote_Fail()
        {
            await _client.PrepareUserAsync();

            await _user.AddNote(1, "One");

            var e = await Record.ExceptionAsync(() => _user.AddNote(1, "One2"));
            var r = e as ResultException;
            Assert.NotNull(e);
            Assert.Equal(ResultCodeType.NoteDuplicate, r.ResultCode);
        }

        [Fact]
        public async Task RemoveNote_Succeed()
        {
            await _client.PrepareUserAsync();

            await _user.AddNote(1, "One");
            await _user.AddNote(2, "Two");
            await _user.RemoveNote(2);

            Assert.Equal(1, _userContext.Notes.Count);
            Assert.Equal("One", _userContext.Notes[1]);
        }

        [Fact]
        public async Task RemoveNote_Fail()
        {
            await _client.PrepareUserAsync();

            var e = await Record.ExceptionAsync(() => _user.RemoveNote(1));
            var r = e as ResultException;
            Assert.NotNull(e);
            Assert.Equal(ResultCodeType.NoteNotFound, r.ResultCode);
        }
    }
}
