using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Aim.ClusterNode;
using Akka.Actor;
using Akka.Configuration;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;

namespace GameServer
{
    [ClusterRole("UserTable")]
    public class UserTableWorker : ClusterRoleWorker
    {
        private ClusterNodeContext _context;
        private IActorRef _userTable;

        public UserTableWorker(ClusterNodeContext context, Config config)
        {
            _context = context;
        }

        public override Task Start()
        {
            _userTable = _context.System.ActorOf(
                Props.Create(() => new DistributedActorTable<long>("User", _context.ClusterActorDiscovery, null, null)),
                "UserTable");
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            await _userTable.GracefulStop(
                TimeSpan.FromMinutes(1),
                new DistributedActorTableMessage<long>.GracefulStop(InterfacedPoisonPill.Instance));
        }
    }

    [ClusterRole("User")]
    public class UserWorker : ClusterRoleWorker
    {
        private ClusterNodeContext _context;
        private IActorRef _userContainer;
        private ChannelType _channelType;
        private IPEndPoint _listenEndPoint;
        private IPEndPoint _connectEndPoint;
        private GatewayRef _gateway;

        public UserWorker(ClusterNodeContext context, Config config)
        {
            _context = context;
            _channelType = (ChannelType)Enum.Parse(typeof(ChannelType), config.GetString("type", "Tcp"), true);
            _listenEndPoint = new IPEndPoint(IPAddress.Any, config.GetInt("port", 0));

            var connectAddress = config.GetString("connect-address");
            var connectPort = config.GetInt("connect-port", _listenEndPoint.Port);
            _connectEndPoint = new IPEndPoint(connectAddress != null ? IPAddress.Parse(connectAddress) : IPAddress.Loopback, connectPort);
        }

        public override async Task Start()
        {
            // create UserTableContainer

            _userContainer = _context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                    "User", _context.ClusterActorDiscovery, typeof(UserActorFactory), new object[] { _context }, InterfacedPoisonPill.Instance)),
                "UserTableContainer");

            // create gateway for users to connect to

            if (_listenEndPoint.Port != 0)
            {
                var serializer = PacketSerializer.CreatePacketSerializer();

                var name = "UserGateway";
                var initiator = new GatewayInitiator
                {
                    ListenEndPoint = _listenEndPoint,
                    ConnectEndPoint = _connectEndPoint,
                    TokenRequired = true,
                    GatewayLogger = LogManager.GetLogger(name),
                    CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                    ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                    PacketSerializer = serializer,
                };

                _gateway = (_channelType == ChannelType.Tcp)
                    ? _context.System.ActorOf(Props.Create(() => new TcpGateway(initiator)), name).Cast<GatewayRef>()
                    : _context.System.ActorOf(Props.Create(() => new UdpGateway(initiator)), name).Cast<GatewayRef>();
                await _gateway.Start();
            }
        }

        public override async Task Stop()
        {
            // stop gateway

            if (_gateway != null)
            {
                await _gateway.CastToIActorRef().GracefulStop(
                    TimeSpan.FromSeconds(10),
                    InterfacedMessageBuilder.Request<IGateway>(x => x.Stop()));
            }

            // stop user container

            await _userContainer.GracefulStop(TimeSpan.FromSeconds(10), PoisonPill.Instance);
        }
    }

    public class UserActorFactory : IActorFactory
    {
        private ClusterNodeContext _clusterContext;

        public void Initialize(object[] args)
        {
            _clusterContext = (ClusterNodeContext)args[0];
        }

        public IActorRef CreateActor(IActorRefFactory actorRefFactory, object id, object[] args)
        {
            return actorRefFactory.ActorOf(Props.Create(() => new UserActor(_clusterContext, (long)id)));
        }
    }

    [ClusterRole("UserLogin")]
    public class UserLoginWorker : ClusterRoleWorker
    {
        private ClusterNodeContext _context;
        private ChannelType _channelType;
        private IPEndPoint _listenEndPoint;
        private GatewayRef _gateway;

        public UserLoginWorker(ClusterNodeContext context, Config config)
        {
            _context = context;
            _channelType = (ChannelType)Enum.Parse(typeof(ChannelType), config.GetString("type", "Tcp"), true);
            _listenEndPoint = new IPEndPoint(IPAddress.Any, config.GetInt("port", 0));
        }

        public override async Task Start()
        {
            var serializer = PacketSerializer.CreatePacketSerializer();

            var name = "UserLoginGateway";
            var initiator = new GatewayInitiator
            {
                ListenEndPoint = _listenEndPoint,
                GatewayLogger = LogManager.GetLogger(name),
                CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                PacketSerializer = serializer,
                CreateInitialActors = (context, connection) => new[]
                {
                    Tuple.Create(
                        context.ActorOf(Props.Create(() =>
                            new UserLoginActor(_context, context.Self.Cast<ActorBoundChannelRef>(), GatewayInitiator.GetRemoteEndPoint(connection)))),
                        new TaggedType[] { typeof(IUserLogin) },
                        ActorBindingFlags.CloseThenStop | ActorBindingFlags.StopThenCloseChannel)
                }
            };

            _gateway = (_channelType == ChannelType.Tcp)
                ? _context.System.ActorOf(Props.Create(() => new TcpGateway(initiator)), name).Cast<GatewayRef>()
                : _context.System.ActorOf(Props.Create(() => new UdpGateway(initiator)), name).Cast<GatewayRef>();
            await _gateway.Start();
        }

        public override async Task Stop()
        {
            // stop gateway

            await _gateway.CastToIActorRef().GracefulStop(
                TimeSpan.FromSeconds(10),
                InterfacedMessageBuilder.Request<IGateway>(x => x.Stop()));
        }
    }
}
