using HandoraApplication.AI.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Interfaces
{
    public interface IVectorStoreService
    {
        Task EnsureCollectionExistsAsync(string collectionName, ulong vectorSize);

        Task UpsertAsync(
            string collectionName,
            string id,
            float[] embedding,
            string text,
            Dictionary<string, object>? metadata = null);

        Task<IReadOnlyList<RagSearchResultDto>> SearchAsync(
            string collectionName,
            float[] embedding,
            int topK,
            Dictionary<string, object>? filter = null);

        Task DeleteAsync(string collectionName, string id);

        Task DeleteByFilterAsync(string collectionName, Dictionary<string, object> filter);
    }
}
