using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices.AI
{
    public interface IVectorDbService
    {
        Task EnsureCollectionExistsAsync(string collectionName, ulong vectorSize);
        Task UpsertVectorAsync(string collectionName, Guid id, float[] vector,
                               Dictionary<string, string> payload);
        Task<List<(Guid Id, float Score, Dictionary<string, string> Payload)>>
            SearchAsync(string collectionName, float[] queryVector, int topK = 5);
    }
}
