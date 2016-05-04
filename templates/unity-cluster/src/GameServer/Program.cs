using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new GameService();
            var cts = new CancellationTokenSource();
            var runTask = Task.Run(() => service.RunAsync(cts.Token));

            Console.WriteLine("Enter to stop system.");
            Console.ReadLine();

            cts.Cancel();
            runTask.Wait();
        }
    }
}
