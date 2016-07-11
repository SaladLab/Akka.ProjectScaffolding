using Aim.ClusterNode;
using Akka.Cluster.Utility;

namespace GameServer
{
    public class ClusterNodeContext : ClusterNodeContextBase
    {
        [ClusterActor("User")]
        public DistributedActorTableRef<long> UserTable;
    }
}
