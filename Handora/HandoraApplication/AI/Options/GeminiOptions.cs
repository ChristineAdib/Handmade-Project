using System;

namespace HandoraInfrastructure.AI.Options
{
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string ApiKey { get; set; } = string.Empty;

        public string ChatModel { get; set; } = "gemini-1.5-flash";
    }
}
