using System;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Domain;

namespace GameServer
{
    public class ClusterContextFixture : IDisposable
    {
        public ClusterNodeContext Context { get; private set; }

        public ClusterContextFixture()
        {
            // force interface assembly to be loaded before creating ProtobufSerializer

            var type = typeof(IUser);
            if (type == null)
                throw new InvalidProgramException("!");
        }

        public void Initialize(ActorSystem system)
        {
            DeadRequestProcessingActor.Install(system);

            var context = new ClusterNodeContext { System = system };

            context.ClusterActorDiscovery = system.ActorOf(Props.Create(
                      () => new ClusterActorDiscovery(null)));

            context.UserTable = new DistributedActorTableRef<long>(system.ActorOf(
                Props.Create(() => new DistributedActorTable<long>(
                    "User", context.ClusterActorDiscovery, null, null)),
                "UserTable"));

            context.UserTableContainer = new DistributedActorTableContainerRef<long>(system.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                    "User", context.ClusterActorDiscovery, null, null, InterfacedPoisonPill.Instance)),
                "UserTableContainer"));

            Context = context;
        }

        public void Dispose()
        {
            if (Context == null)
                return;

            Context.System.Terminate();
            Context = null;
        }
    }
}
