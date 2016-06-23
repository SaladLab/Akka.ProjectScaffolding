using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Utility;
using Akka.Configuration;
using Akka.Interfaced;
using Akka.Interfaced.ProtobufSerializer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain.Interface;
using ProtoBuf.Meta;
using TypeAlias;
using Akka.Interfaced.SlimServer;

namespace GameServer
{
    public class ClusterRunner
    {
        private readonly Config _commonConfig;

        public class Node
        {
            public ActorSystem System;

            public class RoleActor
            {
                public string Role { get; }
                public IActorRef[] Actors { get; }

                public RoleActor(string role, IActorRef[] actors)
                {
                    Role = role;
                    Actors = actors;
                }
            }

            public RoleActor[] RoleActors;
        }

        private readonly List<Node> _nodes = new List<Node>();

        public ClusterRunner(Config commonConfig)
        {
            _commonConfig = commonConfig;
        }

        public void LaunchNode(int port, int clientPort, params string[] roles)
        {
            var config = _commonConfig
                .WithFallback("akka.remote.helios.tcp.port = " + port)
                .WithFallback("akka.cluster.roles = " + "[" + string.Join(",", roles) + "]");

            var system = ActorSystem.Create("GameCluster", config);

            DeadRequestProcessingActor.Install(system);

            var cluster = Cluster.Get(system);
            var context = new ClusterNodeContext { System = system };

            context.ClusterActorDiscovery =
                system.ActorOf(Props.Create(() => new ClusterActorDiscovery(cluster)), "ClusterActorDiscovery");
            context.ClusterNodeContextUpdater =
                system.ActorOf(Props.Create(() => new ClusterNodeContextUpdater(context)), "ClusterNodeContextUpdater");

            var roleActors = new List<Node.RoleActor>();
            foreach (var role in roles)
            {
                switch (role)
                {
                    case "user-table":
                        roleActors.Add(new Node.RoleActor(role, InitUserTable(context)));
                        break;

                    case "user":
                        roleActors.Add(new Node.RoleActor(role, InitUser(context, clientPort)));
                        break;

                    default:
                        throw new InvalidOperationException("Invalid role: " + role);
                }
            }

            _nodes.Add(new Node
            {
                System = system,
                RoleActors = roleActors.ToArray()
            });
        }

        public void Shutdown()
        {
            Console.WriteLine("Shutdown: User Gateway");
            {
                var tasks = GetRoleActors("user").Select(
                    actors => actors[0].GracefulStop(TimeSpan.FromMinutes(1),
                                                     new ClientGatewayMessage.Stop()));
                Task.WhenAll(tasks.ToArray()).Wait();
            }

            Console.WriteLine("Shutdown: User Table");
            {
                var tasks = GetRoleActors("user-table").Select(
                    actors => actors[0].GracefulStop(TimeSpan.FromMinutes(1),
                                                     new DistributedActorTableMessage<long>.GracefulStop(
                                                         InterfacedPoisonPill.Instance)));
                Task.WhenAll(tasks.ToArray()).Wait();
            }

            Console.WriteLine("Shutdown: Systems");
            {
                foreach (var node in Enumerable.Reverse(_nodes))
                    node.System.Terminate();
            }
        }

        private IEnumerable<IActorRef[]> GetRoleActors(string role)
        {
            foreach (var node in _nodes)
            {
                foreach (var ra in node.RoleActors.Where(ra => ra.Role == role))
                {
                    yield return ra.Actors;
                }
            }
        }

        private IActorRef[] InitUserTable(ClusterNodeContext context)
        {
            return new[]
            {
                context.System.ActorOf(
                    Props.Create(() => new DistributedActorTable<long>(
                                           "User", context.ClusterActorDiscovery, null, null)),
                    "UserTable")
            };
        }

        private IActorRef[] InitUser(ClusterNodeContext context, int clientPort)
        {
            var container = context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                                       "User", context.ClusterActorDiscovery, null, null)),
                "UserTableContainer");
            context.UserTableContainer = container;

            var userSystem = new UserClusterSystem(context);
            var gateway = userSystem.Start(clientPort);

            return new[] { gateway, container };
        }
    }

    public class UserClusterSystem
    {
        private readonly ClusterNodeContext _context;

        public UserClusterSystem(ClusterNodeContext context)
        {
            _context = context;
        }

        public async Task<GatewayRef> Start(ChannelType type, int port)
        {
            var serializer = PacketSerializer.CreatePacketSerializer();

            var initiator = new GatewayInitiator
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Any, port),
                GatewayLogger = LogManager.GetLogger($"Gateway({type})"),
                CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                PacketSerializer = serializer,
                CreateInitialActors = (context, connection) => new[]
                {
                    Tuple.Create(
                        context.ActorOf(Props.Create(() =>
                            new UserLoginActor(_context, context.Self.Cast<ActorBoundChannelRef>(), GatewayInitiator.GetRemoteEndPoint(connection)))),
                        new TaggedType[] { typeof(IUserLogin) },
                        (ActorBindingFlags)0)
                }
            };

            var gateway = (type == ChannelType.Tcp)
                ? _context.System.ActorOf(Props.Create(() => new TcpGateway(initiator)), "TcpGateway").Cast<GatewayRef>()
                : _context.System.ActorOf(Props.Create(() => new UdpGateway(initiator)), "UdpGateway").Cast<GatewayRef>();

            await gateway.Start();

            return gateway;
        }
    }
}
