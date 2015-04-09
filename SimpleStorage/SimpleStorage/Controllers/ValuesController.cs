using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Client;
using Domain;
using SimpleStorage.Infrastructure;

namespace SimpleStorage.Controllers
{
    public class ValuesController : ApiController
    {
        private readonly IConfiguration configuration;
        private readonly IStateRepository stateRepository;
        private readonly IStorage storage;
        private string shardFormat = "http://127.0.0.1:{0}/";

        public ValuesController(IStorage storage, IStateRepository stateRepository, IConfiguration configuration)
        {
            this.storage = storage;
            this.stateRepository = stateRepository;
            this.configuration = configuration;
        }

        private void CheckState()
        {
            if (stateRepository.GetState() != State.Started)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }

        // GET api/values/5 
        public Value Get(string id)
        {
            CheckState();
            var results = new List<Value> { storage.Get(id) };
            results.AddRange(configuration.OtherShardsPorts
                                            .Select(port => string.Format(shardFormat, port))
                                            .Select(shard => new InternalClient(shard).Get(id))
                     );

            if (results.All(res => res == null))
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return results.OrderByDescending(v => v, new ValueComparer()).First();
        }

        // PUT api/values/5
        public void Put(string id, [FromBody] Value value)
        {
            CheckState();
            storage.Set(id, value);

            foreach (var shardPort in configuration.OtherShardsPorts)
            {
                var shard = string.Format(shardFormat, shardPort);
                var internalClient = new InternalClient(shard);
                internalClient.Put(id, value);
            }
        }
    }
}