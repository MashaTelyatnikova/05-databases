using System.Linq;
using System.Net;
using SimpleStorage.Infrastructure;

namespace SimpleStorage
{
    public class Configuration : IConfiguration
    {
        public Configuration(ITopology topology)
        {
            IsMaster = !topology.Replicas.Any();
            if (!IsMaster)
                MasterEndpoint = topology.Replicas.First();
            OtherReplicas = topology.Replicas.ToArray();
        }

        public bool IsMaster { get; private set; }
        public IPEndPoint MasterEndpoint { get; private set; }

        public int CurrentNodePort { get; set; }
        public int[] OtherShardsPorts { get; set; }
        public IPEndPoint[] OtherReplicas { get; set; }
    }
}