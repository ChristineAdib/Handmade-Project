using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.DTOs
{
    public class RagDocumentDto
    {
        public string Id { get; set; } = default!;

        public string Collection { get; set; } = default!;

        public string Text { get; set; } = default!;

        public Dictionary<string, object>? Metadata { get; set; }
    }
}
