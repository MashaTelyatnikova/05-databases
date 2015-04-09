using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Domain;

namespace Client
{
    public class SimpleStorageClient : ISimpleStorageClient
    {
        private readonly IEnumerable<string> endpoints;
        private const int AttemptsCount = 5;
        public SimpleStorageClient(params string[] endpoints)
        {
            if (endpoints == null || !endpoints.Any())
                throw new ArgumentException("Empty endpoints!", "endpoints");
            this.endpoints = endpoints;
        }

        public void Put(string id, Value value)
        {
            var attemptNumber = 1;
            while (attemptNumber <= AttemptsCount)
            {
                foreach (var shard in endpoints)
                {
                    try
                    {
                        var putUri = shard + "api/values/" + id;
                        using (var client = new HttpClient())
                        using (var response = client.PutAsJsonAsync(putUri, value).Result)
                            response.EnsureSuccessStatusCode();
                    }
                    catch (Exception)
                    {

                    }
                }
                attemptNumber++;
            }
        }

        public Value Get(string id)
        {
            var attemptNumber = 1;
            while (attemptNumber <= AttemptsCount)
            {
                foreach (var shard in endpoints)
                {
                    var requestUri = shard + "api/values/" + id;
                    try
                    {
                        using (var client = new HttpClient())
                        using (var response = client.GetAsync(requestUri).Result)
                        {
                            response.EnsureSuccessStatusCode();
                            return response.Content.ReadAsAsync<Value>().Result;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                attemptNumber++;
            }

            return null;
        }
    }
}