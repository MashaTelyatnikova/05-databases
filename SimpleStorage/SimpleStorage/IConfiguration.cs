using System.Net;

namespace SimpleStorage
{
    public interface IConfiguration
    {
        int CurrentNodePort { get; }
        int[] OtherShardsPorts { get; }
        bool IsMaster { get; }
        IPEndPoint MasterEndpoint { get; }
        IPEndPoint[] OtherReplicas { get; set; }
    }
}