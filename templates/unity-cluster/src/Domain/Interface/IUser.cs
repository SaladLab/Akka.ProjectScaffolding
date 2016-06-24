using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IUser : IInterfacedActor
    {
        Task SetNickname(string nickname);
        Task AddNote(int id, string note);
        Task RemoveNote(int id);
    }
}
