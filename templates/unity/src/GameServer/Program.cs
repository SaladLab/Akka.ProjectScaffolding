using System;
using Topshelf;

namespace GameServer
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return (int)HostFactory.Run(x =>
            {
                x.SetServiceName("GameServer");
                x.SetDisplayName("GameServer for YourProject");
                x.SetDescription("GameServer for YourProject using Akka.NET and Akka.Interfaced.");

                x.UseAssemblyInfoForServiceInfo();
                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.Service(() => new GameService());
                x.EnableServiceRecovery(r => r.RestartService(1));
            });
        }
    }
}
