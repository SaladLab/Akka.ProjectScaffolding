using Akka.Cluster.Utility;
using Akka.Interfaced;

namespace GameServer
{
    public class ClusterNodeContextUpdater : InterfacedActor<ClusterNodeContextUpdater>
    {
        private readonly ClusterNodeContext _clusterContext;

        public ClusterNodeContextUpdater(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;
        }

        protected override void PreStart()
        {
            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor("User"), Self);
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorUp m)
        {
            switch (m.Tag)
            {
                case "User":
                    _clusterContext.UserTable = m.Actor;
                    break;
            }
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorDown m)
        {
            switch (m.Tag)
            {
                case "User":
                    _clusterContext.UserTable = null;
                    break;
            }
        }
    }
}
