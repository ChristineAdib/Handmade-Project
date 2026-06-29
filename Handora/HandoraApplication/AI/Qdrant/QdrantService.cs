using HandoraApplication.AI.Interfaces;
using HandoraApplication.AI.DTOs;
using HandoraInfrastructure.AI.Options;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value = Qdrant.Client.Grpc.Value;
using Struct = Qdrant.Client.Grpc.Struct;
using ListValue = Qdrant.Client.Grpc.ListValue;
using PointId = Qdrant.Client.Grpc.PointId;

namespace HandoraInfrastructure.AI.Qdrant
{
    public class QdrantService : IVectorStoreService
    {
        private readonly QdrantClient? _client;
        private readonly bool _useInMemoryMode;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<InMemoryPoint>> InMemoryCollections = new();
        private static readonly string DbFilePath = Path.Combine(AppContext.BaseDirectory, "vector_store_db.json");
        private static readonly object FileLock = new object();

        private class InMemoryPoint
        {
            public string Id { get; set; } = string.Empty;
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public string Text { get; set; } = string.Empty;
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        public QdrantService(IOptions<QdrantOptions> options)
        {
            var config = options.Value;

            if (string.IsNullOrWhiteSpace(config.Url) || 
                config.Url.Contains("YOUR-CLUSTER-URL") || 
                config.Url.Contains("YOUR_") ||
                string.IsNullOrWhiteSpace(config.ApiKey) ||
                config.ApiKey.Contains("YOUR-API-KEY"))
            {
                _useInMemoryMode = true;
                LoadInMemoryDb();
                return;
            }

            try
            {
                var uriBuilder = new UriBuilder(config.Url);
                if (uriBuilder.Port == -1 || uriBuilder.Port == 80 || uriBuilder.Port == 443)
                {
                    uriBuilder.Port = 6334;
                }

                _client = new QdrantClient(
                    uriBuilder.Uri,
                    apiKey: config.ApiKey);
            }
            catch
            {
                _useInMemoryMode = true;
                LoadInMemoryDb();
            }
        }

        private void LoadInMemoryDb()
        {
            lock (FileLock)
            {
                try
                {
                    if (File.Exists(DbFilePath))
                    {
                        var json = File.ReadAllText(DbFilePath);
                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<InMemoryPoint>>>(json);
                        if (data != null)
                        {
                            foreach (var kvp in data)
                            {
                                InMemoryCollections[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to empty if reading fails
                }
            }
        }

        private void SaveInMemoryDb()
        {
            lock (FileLock)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(InMemoryCollections);
                    File.WriteAllText(DbFilePath, json);
                }
                catch
                {
                    // Ignore saving failures in dev environment
                }
            }
        }

        public async Task EnsureCollectionExistsAsync(string collectionName, ulong vectorSize)
        {
            if (_useInMemoryMode)
            {
                InMemoryCollections.GetOrAdd(collectionName, _ => new List<InMemoryPoint>());
                return;
            }

            if (_client == null) throw new InvalidOperationException("Qdrant client is not initialized.");

            var collections = await _client.ListCollectionsAsync();
            bool exists = collections.Contains(collectionName);

            if (!exists)
            {
                await _client.CreateCollectionAsync(
                    collectionName: collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = vectorSize,
                        Distance = Distance.Cosine
                    });
            }
        }

        public async Task UpsertAsync(
            string collectionName,
            string id,
            float[] embedding,
            string text,
            Dictionary<string, object>? metadata = null)
        {
            if (_useInMemoryMode)
            {
                var collection = InMemoryCollections.GetOrAdd(collectionName, _ => new List<InMemoryPoint>());
                lock (collection)
                {
                    collection.RemoveAll(p => p.Id == id);
                    collection.Add(new InMemoryPoint
                    {
                        Id = id,
                        Embedding = embedding,
                        Text = text,
                        Metadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>()
                    });
                }
                SaveInMemoryDb();
                return;
            }

            if (_client == null) throw new InvalidOperationException("Qdrant client is not initialized.");

            var pointId = ToPointId(id);

            var point = new PointStruct
            {
                Id = pointId,
                Vectors = embedding
            };

            point.Payload.Add("text", text);

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    if (kvp.Key.Equals("text", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    point.Payload.Add(kvp.Key, ToProtobufValue(kvp.Value));
                    await EnsurePayloadIndexExistsAsync(collectionName, kvp.Key, kvp.Value);
                }
            }

            await _client.UpsertAsync(collectionName, new[] { point });
        }

        public async Task<IReadOnlyList<RagSearchResultDto>> SearchAsync(
            string collectionName,
            float[] embedding,
            int topK,
            Dictionary<string, object>? filter = null)
        {
            if (_useInMemoryMode)
            {
                var collection = InMemoryCollections.GetOrAdd(collectionName, _ => new List<InMemoryPoint>());
                List<InMemoryPoint> candidates;
                lock (collection)
                {
                    candidates = collection.ToList();
                }

                // Apply dynamic filters
                if (filter != null && filter.Count > 0)
                {
                    candidates = candidates.Where(p =>
                    {
                        foreach (var kvp in filter)
                        {
                            if (!p.Metadata.TryGetValue(kvp.Key, out var val))
                            {
                                return false;
                            }
                            
                            // Handle list of strings (e.g. tags contain keyword)
                            if (kvp.Value is IEnumerable<string> strList)
                            {
                                var storedStr = val.ToString() ?? string.Empty;
                                if (val is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var storedList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(elem.GetRawText()) ?? new List<string>();
                                    if (!strList.Any(s => storedList.Contains(s, StringComparer.OrdinalIgnoreCase)))
                                    {
                                        return false;
                                    }
                                }
                                else if (!strList.Any(s => storedStr.Equals(s, StringComparison.OrdinalIgnoreCase)))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                var filterValStr = kvp.Value?.ToString() ?? string.Empty;
                                var valStr = val?.ToString() ?? string.Empty;
                                if (!valStr.Equals(filterValStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }).ToList();
                }

                var results = candidates.Select(p =>
                {
                    double score = ComputeCosineSimilarity(embedding, p.Embedding);
                    return new RagSearchResultDto
                    {
                        Id = p.Id,
                        Text = p.Text,
                        Score = (float)score,
                        Metadata = p.Metadata.Count > 0 ? p.Metadata : null
                    };
                })
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();

                return results;
            }

            if (_client == null) throw new InvalidOperationException("Qdrant client is not initialized.");

            Filter? qdrantFilter = null;

            if (filter != null && filter.Count > 0)
            {
                qdrantFilter = new Filter();
                foreach (var kvp in filter)
                {
                    var condition = BuildCondition(kvp.Key, kvp.Value);
                    if (condition != null)
                    {
                        qdrantFilter.Must.Add(condition);
                    }
                }
            }

            var searchResults = await _client.SearchAsync(
                collectionName: collectionName,
                vector: embedding,
                filter: qdrantFilter,
                limit: (ulong)topK);

            var list = new List<RagSearchResultDto>();

            foreach (var hit in searchResults)
            {
                string text = string.Empty;
                if (hit.Payload.TryGetValue("text", out var textValue) && textValue.KindCase == Value.KindOneofCase.StringValue)
                {
                    text = textValue.StringValue;
                }

                var resMetadata = new Dictionary<string, object>();
                foreach (var kvp in hit.Payload)
                {
                    if (kvp.Key == "text")
                    {
                        continue;
                    }
                    var val = FromProtobufValue(kvp.Value);
                    if (val != null)
                    {
                        resMetadata[kvp.Key] = val;
                    }
                }

                list.Add(new RagSearchResultDto
                {
                    Id = hit.Id.ToString(),
                    Text = text,
                    Score = hit.Score,
                    Metadata = resMetadata.Count > 0 ? resMetadata : null
                });
            }

            return list;
        }

        private double ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length || vectorA.Length == 0)
            {
                return 0.0;
            }

            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0.0 || normB == 0.0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        public async Task DeleteAsync(string collectionName, string id)
        {
            if (_useInMemoryMode)
            {
                var collection = InMemoryCollections.GetOrAdd(collectionName, _ => new List<InMemoryPoint>());
                lock (collection)
                {
                    collection.RemoveAll(p => p.Id == id);
                }
                SaveInMemoryDb();
                return;
            }

            if (_client == null) throw new InvalidOperationException("Qdrant client is not initialized.");

            var qdrantFilter = new Filter();
            if (Guid.TryParse(id, out var guid))
            {
                qdrantFilter.Must.Add(Conditions.HasId(new[] { guid }));
            }
            else if (ulong.TryParse(id, out var num))
            {
                qdrantFilter.Must.Add(Conditions.HasId(new[] { num }));
            }
            else
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(id));
                    var deterministicGuid = new Guid(hash);
                    qdrantFilter.Must.Add(Conditions.HasId(new[] { deterministicGuid }));
                }
            }

            await _client.DeleteAsync(collectionName, qdrantFilter);
        }

        public async Task DeleteByFilterAsync(string collectionName, Dictionary<string, object> filter)
        {
            if (filter == null || filter.Count == 0)
            {
                throw new ArgumentException("Filter cannot be null or empty for deletion.", nameof(filter));
            }

            if (_useInMemoryMode)
            {
                var collection = InMemoryCollections.GetOrAdd(collectionName, _ => new List<InMemoryPoint>());
                lock (collection)
                {
                    collection.RemoveAll(p =>
                    {
                        foreach (var kvp in filter)
                        {
                            if (!p.Metadata.TryGetValue(kvp.Key, out var val))
                            {
                                return false;
                            }
                            var filterValStr = kvp.Value?.ToString() ?? string.Empty;
                            var valStr = val?.ToString() ?? string.Empty;
                            if (!valStr.Equals(filterValStr, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }
                        return true;
                    });
                }
                SaveInMemoryDb();
                return;
            }

            if (_client == null) throw new InvalidOperationException("Qdrant client is not initialized.");

            var qdrantFilter = new Filter();
            foreach (var kvp in filter)
            {
                var condition = BuildCondition(kvp.Key, kvp.Value);
                if (condition != null)
                {
                    qdrantFilter.Must.Add(condition);
                }
            }

            await _client.DeleteAsync(collectionName, qdrantFilter);
        }

        #region Helpers

        private PointId ToPointId(string id)
        {
            if (Guid.TryParse(id, out var guid))
            {
                return guid;
            }
            if (ulong.TryParse(id, out var num))
            {
                return num;
            }

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(id));
                return new Guid(hash);
            }
        }

        private Condition? BuildCondition(string key, object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is bool b)
            {
                return Conditions.Match(key, b);
            }
            if (value is int i)
            {
                return Conditions.Match(key, (long)i);
            }
            if (value is long l)
            {
                return Conditions.Match(key, l);
            }
            if (value is IEnumerable<string> strList)
            {
                var filter = new Filter();
                foreach (var s in strList)
                {
                    filter.Should.Add(Conditions.MatchKeyword(key, s));
                }
                return new Condition { Filter = filter };
            }
            if (value is IEnumerable<long> longList)
            {
                var filter = new Filter();
                foreach (var val in longList)
                {
                    filter.Should.Add(Conditions.Match(key, val));
                }
                return new Condition { Filter = filter };
            }
            if (value is string str)
            {
                return Conditions.MatchKeyword(key, str);
            }

            return Conditions.MatchKeyword(key, value.ToString() ?? string.Empty);
        }

        private Value ToProtobufValue(object? obj)
        {
            if (obj == null)
            {
                return new Value();
            }

            if (obj is string s)
            {
                return new Value { StringValue = s };
            }

            if (obj is bool b)
            {
                return new Value { BoolValue = b };
            }

            if (obj is int i)
            {
                return new Value { IntegerValue = i };
            }

            if (obj is long l)
            {
                return new Value { IntegerValue = l };
            }

            if (obj is double d)
            {
                return new Value { DoubleValue = d };
            }

            if (obj is float f)
            {
                return new Value { DoubleValue = f };
            }

            if (obj is decimal dec)
            {
                return new Value { DoubleValue = (double)dec };
            }

            if (obj is DateTime dt)
            {
                return new Value { StringValue = dt.ToString("o") };
            }

            if (obj is Guid g)
            {
                return new Value { StringValue = g.ToString() };
            }

            if (obj is System.Collections.IDictionary dict)
            {
                var structVal = new Struct();
                foreach (var key in dict.Keys)
                {
                    var keyStr = key.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(keyStr))
                    {
                        structVal.Fields[keyStr] = ToProtobufValue(dict[key]);
                    }
                }
                return new Value { StructValue = structVal };
            }

            if (obj is System.Collections.IEnumerable list)
            {
                var listVal = new ListValue();
                foreach (var item in list)
                {
                    listVal.Values.Add(ToProtobufValue(item));
                }
                return new Value { ListValue = listVal };
            }

            return new Value { StringValue = obj.ToString() ?? string.Empty };
        }

        private object? FromProtobufValue(Value val)
        {
            switch (val.KindCase)
            {
                case Value.KindOneofCase.None:
                    return null;
                case Value.KindOneofCase.IntegerValue:
                    return val.IntegerValue;
                case Value.KindOneofCase.DoubleValue:
                    return val.DoubleValue;
                case Value.KindOneofCase.StringValue:
                    return val.StringValue;
                case Value.KindOneofCase.BoolValue:
                    return val.BoolValue;
                case Value.KindOneofCase.StructValue:
                    var dict = new Dictionary<string, object?>();
                    foreach (var field in val.StructValue.Fields)
                    {
                        dict[field.Key] = FromProtobufValue(field.Value);
                    }
                    return dict;
                case Value.KindOneofCase.ListValue:
                    var list = new List<object?>();
                    foreach (var item in val.ListValue.Values)
                    {
                        list.Add(FromProtobufValue(item));
                    }
                    return list;
                default:
                    return null;
            }
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string, string), bool> CreatedIndexes = new();

        private async Task EnsurePayloadIndexExistsAsync(string collectionName, string fieldName, object value)
        {
            if (_useInMemoryMode) return;
            if (_client == null) return;

            var key = (collectionName, fieldName);
            if (CreatedIndexes.ContainsKey(key))
            {
                return;
            }

            PayloadSchemaType schemaType;
            if (value is bool)
            {
                schemaType = PayloadSchemaType.Bool;
            }
            else if (value is int || value is long || value is short || value is byte)
            {
                schemaType = PayloadSchemaType.Integer;
            }
            else if (value is float || value is double || value is decimal)
            {
                schemaType = PayloadSchemaType.Float;
            }
            else
            {
                schemaType = PayloadSchemaType.Keyword;
            }

            try
            {
                await _client.CreatePayloadIndexAsync(
                    collectionName: collectionName,
                    fieldName: fieldName,
                    schemaType: schemaType
                );
                CreatedIndexes.TryAdd(key, true);
            }
            catch (Exception)
            {
                CreatedIndexes.TryAdd(key, true);
            }
        }

        #endregion
    }
}
