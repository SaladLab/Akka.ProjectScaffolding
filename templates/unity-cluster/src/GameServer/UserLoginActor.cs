using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain.Data;
using Domain.Interface;
using TrackableData;

namespace GameServer
{
    [Log]
    public class UserLoginActor : InterfacedActor<UserLoginActor>, IUserLogin
    {
        private readonly ILog _logger;
        private readonly ClusterNodeContext _clusterContext;
        private readonly IActorRef _clientSession;

        public UserLoginActor(ClusterNodeContext clusterContext,
                              IActorRef clientSession,
                              EndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger($"UserLoginActor({clientRemoteEndPoint})");
            _clusterContext = clusterContext;
            _clientSession = clientSession;
        }

        [MessageHandler]
        private void OnMessage(ActorBoundSessionMessage.SessionTerminated message)
        {
            Context.Stop(Self);
        }

        async Task<LoginResult> IUserLogin.Login(int observerId)
        {
            // create user context

            var userContext = new TrackableUserContext
            {
                Data = new TrackableUserData
                {
                    Nickname = "",
                    RegisterTime = DateTime.UtcNow,
                },
                Notes = new TrackableDictionary<int, string>()
            };
            userContext.SetDefaultTracker();

            // make UserActor

            var userId = CreateUserId();
            IActorRef user;
            try
            {
                user = Context.System.ActorOf(
                    Props.Create<UserActor>(_clusterContext, _clientSession, userId, userContext, observerId),
                    "user_" + userId);
            }
            catch (Exception e)
            {
                _logger.Error($"Exception in creating UserActor({userId}", e);
                throw new ResultException(ResultCodeType.LoginFailed);
            }

            // register User in UserTable

            var reply = await _clusterContext.UserTableContainer.Ask<DistributedActorTableMessage<long>.AddReply>(
                new DistributedActorTableMessage<long>.Add(userId, user));

            if (reply.Added == false)
            {
                user.Tell(PoisonPill.Instance);
                throw new ResultException(ResultCodeType.LoginFailed);
            }

            // bind user actor with client session, which makes client to communicate with this actor.

            var reply2 = await _clientSession.Ask<ActorBoundSessionMessage.BindReply>(
                new ActorBoundSessionMessage.Bind(user, typeof(IUser), null));

            return new LoginResult { UserId = userId, UserContext = userContext, UserActorBindId = reply2.ActorId };
        }

        private long CreateUserId()
        {
            // a native int64 unique value generator.
            // if you want to get a strong one, consider 128 bits key or external unique id generator.

            var scratches = new byte[8];
            var bytes = Guid.NewGuid().ToByteArray();
            for (var i = 0; i < bytes.Length; i++)
                scratches[i % scratches.Length] = bytes[i];
            return BitConverter.ToInt64(scratches, 0);
        }
    }
}
