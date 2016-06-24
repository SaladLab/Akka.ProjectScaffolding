using System;
using Akka.Interfaced;

namespace Domain
{
    public interface IUserEventObserver : IInterfacedObserver
    {
        void UserContextChange(TrackableUserContextTracker userContextTracker);
    }
}
