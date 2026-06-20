using System.Collections.Generic;

namespace HandoraDomain.Models.ProductEntities
{
    public class ReviewSummaryResult
    {
        public string OverallSummary { get; set; } = string.Empty;
        public List<string> Pros { get; set; } = new();
        public List<string> Cons { get; set; } = new();
    }
}
