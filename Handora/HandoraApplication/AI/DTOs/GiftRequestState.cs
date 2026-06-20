using System.Collections.Generic;

namespace HandoraApplication.AI.DTOs
{
    public class GiftRequestState
    {
        public string? RecipientType { get; set; }
        public string? AgeRange { get; set; }
        public List<string> Interests { get; set; } = new();
        public string? StylePreferences { get; set; }
        public List<string> ColorPreferences { get; set; } = new();
        public string? Budget { get; set; }
        public string? Occasion { get; set; }
        public string? AdditionalNotes { get; set; }
        
        // Internal parsed values to execute SQL/Vector price filtering
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}
