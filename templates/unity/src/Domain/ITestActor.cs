using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IGreeter : IInterfacedActor
    {
        Task<string> Hello(string who);
        Task<int> GetHelloCount();
    }
}
