using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;
using ProtoBuf.Meta;
using TypeAlias;

namespace GameServer
{
    public class GameService
    {
        private TcpConnectionSettings _tcpConnectionSettings;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var system = CreateActorSystem();

            StartListen(system, 5000);

            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation exception
            }
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

        private void StartListen(ActorSystem system, int port)
        {
            var logger = LogManager.GetLogger("ClientGateway");

            _tcpConnectionSettings = new TcpConnectionSettings();
            _tcpConnectionSettings.PacketSerializer = PacketSerializer.CreatePacketSerializer();

            var clientGateway = system.ActorOf(Props.Create(() => new ClientGateway(logger, CreateSession)));
            clientGateway.Tell(new ClientGatewayMessage.Start(new IPEndPoint(IPAddress.Any, port)));
        }

        private IActorRef CreateSession(IActorContext context, Socket socket)
        {
            var logger = LogManager.GetLogger($"Client({socket.RemoteEndPoint})");
            return context.ActorOf(Props.Create(() =>
                new ClientSession(logger, socket, _tcpConnectionSettings, CreateInitialActor)));
        }

        private static Tuple<IActorRef, ActorBoundSessionMessage.InterfaceType[]>[] CreateInitialActor(IActorContext context, Socket socket) =>
            new[]
            {
                Tuple.Create(context.ActorOf(Props.Create(() => new Greeter(context.Self, socket.RemoteEndPoint))),
                             new[] { new ActorBoundSessionMessage.InterfaceType(typeof(IGreeter)) })
            };
    }
}
