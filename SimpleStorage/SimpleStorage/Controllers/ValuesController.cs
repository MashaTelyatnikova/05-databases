using System;
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
        private const string replica = "http://127.0.0.1:{0}/";
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
            var port = GetPort(id);
            if (port != configuration.CurrentNodePort)
            {
                var client = new SimpleStorageClient(string.Format(replica, port));
                var res = client.Get(id);
                if (res == null)
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                return res;
            }
            CheckState();
            var result = storage.Get(id);
            if (result == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return result;
        }

        // PUT api/values/5
        public void Put(string id, [FromBody] Value value)
        {
            var port = GetPort(id);
            if (port != configuration.CurrentNodePort)
            {
                var client = new SimpleStorageClient(string.Format(replica, port));
                client.Put(id, value);
            }
            else
            {
                CheckState();
                storage.Set(id, value);
            }

        }

        private int GetPort(string id)
        {
            var ports = configuration.OtherShardsPorts.Concat(new[] { configuration.CurrentNodePort }).OrderBy(port => port).ToList();
            var index = Math.Abs(id.GetHashCode()) % (configuration.OtherShardsPorts.Count() + 1);

            return ports[index];
        }
    }
}