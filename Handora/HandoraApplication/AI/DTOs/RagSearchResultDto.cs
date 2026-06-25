using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.DTOs
{
    public class RagSearchResultDto
    {
        public string Id { get; set; } = default!;

        public string Text { get; set; } = default!;

        public double Score { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }
}
