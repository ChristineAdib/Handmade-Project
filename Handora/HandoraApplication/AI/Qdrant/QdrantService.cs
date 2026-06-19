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
        private readonly QdrantClient _client;

        public QdrantService(IOptions<QdrantOptions> options)
        {
            var config = options.Value;

            var uriBuilder = new UriBuilder(config.Url);
            if (uriBuilder.Port == -1 || uriBuilder.Port == 80 || uriBuilder.Port == 443)
            {
                uriBuilder.Port = 6334;
            }

            _client = new QdrantClient(
                uriBuilder.Uri,
                apiKey: config.ApiKey);
        }

        public async Task EnsureCollectionExistsAsync(string collectionName, ulong vectorSize)
        {
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

            var results = await _client.SearchAsync(
                collectionName: collectionName,
                vector: embedding,
                filter: qdrantFilter,
                limit: (ulong)topK);

            var list = new List<RagSearchResultDto>();

            foreach (var hit in results)
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

        public async Task DeleteAsync(string collectionName, string id)
        {
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
                // Suppress if already exists or concurrent creation occurs
                CreatedIndexes.TryAdd(key, true);
            }
        }

        #endregion
    }
}
