using HandoraApplication.IServices.AI;
using HandoraApplication.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services.AIServices
{
    public class VectorDbService : IVectorDbService
    {
        private readonly QdrantClient _client;

        public VectorDbService(IOptions<RagSettings> settings)
        {
            _client = new QdrantClient(
            host: settings.Value.QdrantHost,
            https: true,
            apiKey: settings.Value.QdrantApiKey
            );
        }

        public async Task EnsureCollectionExistsAsync(string collectionName, ulong vectorSize)
        {
            var collections = await _client.ListCollectionsAsync();
            bool exists = collections.Any(c => c == collectionName);

            if (!exists)
            {
                await _client.CreateCollectionAsync(collectionName,
                    new VectorParams
                    {
                        Size = vectorSize,
                        Distance = Distance.Cosine
                    });
            }
        }

        public async Task UpsertVectorAsync(string collectionName, Guid id,
            float[] vector, Dictionary<string, string> payload)
        {
            var point = new PointStruct
            {
                Id = new PointId { Uuid = id.ToString() },
                Vectors = vector,
            };

            foreach (var kv in payload)
                point.Payload[kv.Key] = kv.Value;

            await _client.UpsertAsync(collectionName, new[] { point });
        }

        public async Task<List<(Guid Id, float Score, Dictionary<string, string> Payload)>>
            SearchAsync(string collectionName, float[] queryVector, int topK = 5)
        {
            var results = await _client.SearchAsync(
                collectionName,
                queryVector,
                limit: (ulong)topK,
                scoreThreshold: 0.5f
            );

            return results.Select(r => (
                Id: Guid.Parse(r.Id.Uuid),
                Score: r.Score,
                Payload: r.Payload.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.StringValue)
            )).ToList();
        }
    }
}
