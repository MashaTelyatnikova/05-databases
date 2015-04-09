using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Domain;

namespace Client
{
    public class SimpleStorageClient : ISimpleStorageClient
    {
        private readonly List<string> endpoints;
        private const string CoordinatorAddress = "http://127.0.0.1:17000/";
        public SimpleStorageClient(params string[] endpoints)
        {
            if (endpoints == null || !endpoints.Any())
                throw new ArgumentException("Empty endpoints!", "endpoints");
            this.endpoints = endpoints.OrderBy(i => i).ToList();
        }

        public void Put(string id, Value value)
        {
            var coordinatorClient = new CoordinatorClient(CoordinatorAddress);
            var replica = endpoints.ElementAt(coordinatorClient.Get(id));
            var putUri = replica + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.PutAsJsonAsync(putUri, value).Result)
                response.EnsureSuccessStatusCode();
        }

        public Value Get(string id)
        {
            var coordinatorClient = new CoordinatorClient(CoordinatorAddress);
            var replica = endpoints.ElementAt(coordinatorClient.Get(id));

            var requestUri = replica + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.GetAsync(requestUri).Result)
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<Value>().Result;
            }
        }
    }
}