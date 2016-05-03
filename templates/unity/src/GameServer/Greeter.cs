using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    public class Greeter : InterfacedActor<Greeter>, IGreeter
    {
        private readonly ILog _logger;
        private HelloGenerator _helloGenerator;
        private int _count;

        public Greeter(IActorRef clientSession,
                       EndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger($"UserLoginActor({clientRemoteEndPoint})");
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
