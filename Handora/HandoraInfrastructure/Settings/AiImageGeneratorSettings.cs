using System;

namespace HandoraInfrastructure.Settings
{
    public class AiImageGeneratorSettings
    {
        public const string SectionName = "AiImageGenerator";
        
        public string ActiveProvider { get; set; } = "GoogleAIStudio";
        public int GenerateCount { get; set; } = 3;
        public bool EnableQualityValidation { get; set; } = true;
        public int MinImageSizeBytes { get; set; } = 10000;
        
        public GoogleAIStudioSettings GoogleAIStudio { get; set; } = new();
        public PollinationsSettings Pollinations { get; set; } = new();
        public OpenAISettings OpenAI { get; set; } = new();
        public MockProviderSettings MockProvider { get; set; } = new();
    }

    public class GoogleAIStudioSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "imagen-4.0-generate-001";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    }

    public class PollinationsSettings
    {
        public string BaseUrl { get; set; } = "https://image.pollinations.ai";
        public string Model { get; set; } = "flux";
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
