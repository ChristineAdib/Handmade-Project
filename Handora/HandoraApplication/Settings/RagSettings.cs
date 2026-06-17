using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Settings
{
    public class RagSettings
    {
        public string OpenAiApiKey { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
        public string ChatModel { get; set; } = "gpt-4o-mini";
        public string QdrantHost { get; set; } = "localhost";
        public int QdrantPort { get; set; } = 6334;
        public string CollectionName { get; set; } = "handora_products";
        public string QdrantApiKey { get; set; } = string.Empty;
    }
}
