using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimServer;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    public class Greeter : InterfacedActor, IGreeter
    {
        private readonly ILog _logger;
        private HelloGenerator _helloGenerator;
        private int _count;

        public Greeter(ActorBoundChannelRef channel, IPEndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger($"Greeter({clientRemoteEndPoint})");
            _helloGenerator = new HelloGenerator(who => $"Hello {who}!");
        }

        Task<string> IGreeter.Hello(string who)
        {
            if (string.IsNullOrEmpty(who))
                throw new ArgumentException(nameof(who));

            _count += 1;
            return Task.FromResult(_helloGenerator.GenerateHello(who));
        }

        Task<int> IGreeter.GetHelloCount()
        {
            return Task.FromResult(_count);
        }
    }
}
