using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;
using Topshelf;

namespace GameServer
{
    public class GameService : ServiceControl
    {
        private ActorSystem _system;
        private GatewayRef _gateway;

        public GameService()
        {
        }

        bool ServiceControl.Start(HostControl hostControl)
        {
            // initialize actor system
            _system = ActorSystem.Create("GameServer");
            DeadRequestProcessingActor.Install(_system);

            // start gateway to accept clients
            _gateway = StartListen(_system, ChannelType.Tcp, 5000).Result;
            return true;
        }

        bool ServiceControl.Stop(HostControl hostControl)
        {
            // stop gateway
            _gateway.CastToIActorRef().GracefulStop(
                TimeSpan.FromSeconds(10),
                InterfacedMessageBuilder.Request<IGateway>(x => x.Stop())).Wait();

            // terminate actor system
            _system.Terminate().Wait();
            return true;
        }

        private async Task<GatewayRef> StartListen(ActorSystem system, ChannelType type, int port)
        {
            var serializer = PacketSerializer.CreatePacketSerializer();

            var name = $"Gateway({type})";
            var initiator = new GatewayInitiator
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Any, port),
                GatewayLogger = LogManager.GetLogger(name),
                CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                PacketSerializer = serializer,
                CreateInitialActors = (context, connection) => new[]
                {
                    Tuple.Create(
                        context.ActorOf(Props.Create(() =>
                            new Greeter(context.Self.Cast<ActorBoundChannelRef>(), GatewayInitiator.GetRemoteEndPoint(connection)))),
                        new TaggedType[] { typeof(IGreeter) },
                        (ActorBindingFlags)0)
                }
            };

            var gateway = (type == ChannelType.Tcp)
                ? system.ActorOf(Props.Create(() => new TcpGateway(initiator)), name).Cast<GatewayRef>()
                : system.ActorOf(Props.Create(() => new UdpGateway(initiator)), name).Cast<GatewayRef>();
            await gateway.Start();
            return gateway;
        }
    }
}
