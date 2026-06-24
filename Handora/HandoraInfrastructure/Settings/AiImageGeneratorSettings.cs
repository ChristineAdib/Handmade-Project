using System;

namespace HandoraInfrastructure.Settings
{
    public class AiImageGeneratorSettings
    {
        public const string SectionName = "AiImageGenerator";
        
        public string ActiveProvider { get; set; } = "GoogleAIStudio";
        
        public GoogleAIStudioSettings GoogleAIStudio { get; set; } = new();
        
        public OpenAISettings OpenAI { get; set; } = new();
        
        public MockProviderSettings MockProvider { get; set; } = new();
    }

    public class GoogleAIStudioSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "imagen-3.0-generate-002";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    }

    public class OpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "dall-e-3";
    }

    public class MockProviderSettings
    {
        public string MockImageUrl { get; set; } = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
    }
}
