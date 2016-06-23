using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;

namespace GameServer
{
    public class GameService
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var system = CreateActorSystem();

            var tcpGateway = await StartListen(system, ChannelType.Tcp, 5000);
            var udpGateway = await StartListen(system, ChannelType.Udp, 5000);

            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation exception
            }

            await tcpGateway.Stop();
            await udpGateway.Stop();
        }

        private ActorSystem CreateActorSystem()
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {
                  actor {
                    serializers {
                      wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
                      proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced.ProtobufSerializer""
                    }
                    serialization-bindings {
                      ""Akka.Interfaced.NotificationMessage, Akka.Interfaced-Base"" = proto
                      ""Akka.Interfaced.RequestMessage, Akka.Interfaced-Base"" = proto
                      ""Akka.Interfaced.ResponseMessage, Akka.Interfaced-Base"" = proto
                      ""System.Object"" = wire
                    }
                  }
                }");

            var system  = ActorSystem.Create("GameServer", config);
            DeadRequestProcessingActor.Install(system);

            return system;
        }

        private async Task<GatewayRef> StartListen(ActorSystem system, ChannelType type, int port)
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
                            new Greeter(context.Self.Cast<ActorBoundChannelRef>(), GatewayInitiator.GetRemoteEndPoint(connection)))),
                        new TaggedType[] { typeof(IGreeter) },
                        (ActorBindingFlags)0)
                }
            };

            var gateway = (type == ChannelType.Tcp)
                ? system.ActorOf(Props.Create(() => new TcpGateway(initiator)), "TcpGateway").Cast<GatewayRef>()
                : system.ActorOf(Props.Create(() => new UdpGateway(initiator)), "UdpGateway").Cast<GatewayRef>();

            await gateway.Start();

            return gateway;
        }
    }
}
