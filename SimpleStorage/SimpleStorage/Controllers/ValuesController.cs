using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private string shardFormat = "http://{0}/";
        private readonly int quorum;
        public ValuesController(IStorage storage, IStateRepository stateRepository, IConfiguration configuration)
        {
            this.storage = storage;
            this.stateRepository = stateRepository;
            this.configuration = configuration;
            this.quorum = (configuration.OtherReplicas.Count() + 1) / 2 + 1;
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
            var count = 1;
            foreach (var shardPort in configuration.OtherReplicas)
            {
                if (count >= quorum)
                    break;

                var client = new InternalClient(string.Format(shardFormat, shardPort));

                try
                {
                    var result = client.Get(id);
                    results.Add(result);
                    count++;
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        results.Add(null);
                        count++;
                    }
                }
                catch (Exception)
                {
                    
                }
            }

            if (count < quorum)
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            var res  = results.OrderByDescending(v => v, new ValueComparer()).First();
            if (res == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            return res;
        }

        // PUT api/values/5
        public void Put(string id, [FromBody] Value value)
        {
            CheckState();
            storage.Set(id, value);

            var count = 1;
            foreach (var shardPort in configuration.OtherReplicas)
            {
                if (count >= quorum)
                    break;

                var client = new InternalClient(string.Format(shardFormat, shardPort));

                try
                {
                    client.Put(id, value);
                    count++;
                }
                catch (Exception)
                {
                
                }
            }
        }

    }
}