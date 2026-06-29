using System;

namespace HandoraApplication.AI.DTOs
{
    public class AiImageGenerationResult
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public long GenerationTimeMs { get; set; }
        public string MetadataJson { get; set; } = string.Empty;
    }
}
