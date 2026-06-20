using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.DTOs
{
    public class RagSearchRequestDto
    {
        public string Collection { get; set; } = default!;

        public string Query { get; set; } = default!;

        public int TopK { get; set; } = 5;

        public Dictionary<string, object>? Filter { get; set; }
    }
}
