using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.AI.Options
{
    public class QdrantOptions
    {
        public const string SectionName = "Qdrant";

        public string Url { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
        
        public string Collection { get; set; } = "handora-documents";
    }
}
