using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain.Interface;

namespace GameServer
{
    public abstract class ClusterRoleWorker
    {
        public ClusterNodeContext Context { get; }

        public ClusterRoleWorker(ClusterNodeContext context)
        {
            Context = context;
        }

        public abstract Task Start();
        public abstract Task Stop();
    }

    public class UserTableWorker : ClusterRoleWorker
    {
        private IActorRef _userTable;

        public UserTableWorker(ClusterNodeContext context)
            : base(context)
        {
        }

        public override Task Start()
        {
            _userTable = Context.System.ActorOf(
                Props.Create(() => new DistributedActorTable<long>(
                                        "User", Context.ClusterActorDiscovery, null, null)),
                "UserTable");
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            await _userTable.GracefulStop(
                TimeSpan.FromMinutes(1),
                new DistributedActorTableMessage<long>.GracefulStop(InterfacedPoisonPill.Instance));
            _userTable = null;
        }
    }

    public class UserGatewayWorker : ClusterRoleWorker
    {
        private ChannelType _channelType;
        private IPEndPoint _listenEndPoint;
        private GatewayRef _gateway;

        public UserGatewayWorker(ClusterNodeContext context, ChannelType channelType, IPEndPoint listenEndPoint)
            : base(context)
        {
            _channelType = channelType;
            _listenEndPoint = listenEndPoint;
        }

        public override async Task Start()
        {
            // create UserTableContainer

            var container = Context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>("User", Context.ClusterActorDiscovery, null, null)),
                "UserTableContainer");
            Context.UserTableContainer = container;

            // create gateway for users to connect to

            var serializer = PacketSerializer.CreatePacketSerializer();

            var initiator = new GatewayInitiator
            {
                ListenEndPoint = _listenEndPoint,
                GatewayLogger = LogManager.GetLogger($"Gateway({_channelType})"),
                CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                PacketSerializer = serializer,
                CreateInitialActors = (context, connection) => new[]
                {
                    Tuple.Create(
                        context.ActorOf(Props.Create(() =>
                            new UserLoginActor(Context, context.Self.Cast<ActorBoundChannelRef>(), GatewayInitiator.GetRemoteEndPoint(connection)))),
                        new TaggedType[] { typeof(IUserLogin) },
                        (ActorBindingFlags)0)
                }
            };

            var gateway = (_channelType == ChannelType.Tcp)
                ? Context.System.ActorOf(Props.Create(() => new TcpGateway(initiator)), "TcpGateway").Cast<GatewayRef>()
                : Context.System.ActorOf(Props.Create(() => new UdpGateway(initiator)), "UdpGateway").Cast<GatewayRef>();

            await gateway.Start();

            _gateway = gateway;
        }

        public override async Task Stop()
        {
            if (_gateway != null)
            {
                // stop and wait for being stopped.
                await _gateway.Stop();
                await _gateway.CastToIActorRef().GracefulStop(TimeSpan.FromSeconds(10), new Identify(0));
                _gateway = null;
            }
        }
    }
}
