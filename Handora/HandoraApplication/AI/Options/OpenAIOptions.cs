using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.AI.Options
{
    public class OpenAIOptions
    {
        public const string SectionName = "OpenAI";

        public string ApiKey { get; set; } = string.Empty;

        public string ChatModel { get; set; } = string.Empty;

        public string EmbeddingModel { get; set; } = string.Empty;
    }
}
