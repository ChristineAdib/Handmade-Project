using System;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class CustomStudioSetting : BaseEntity<Guid>
    {
        public int MaxAiGenerations { get; set; } = 2;
        public int MaxReferenceImageSizeMb { get; set; } = 5;
        public string AllowedImageTypes { get; set; } = ".jpg,.jpeg,.png";
        public int DefaultDeliveryTimeDays { get; set; } = 14;
        public int DefaultRevisionCount { get; set; } = 3;
        public string ActiveAiProvider { get; set; } = "GoogleAIStudio";
        public string PromptBuilderInstructions { get; set; } = "A premium, high-quality, professional studio photo of a handmade amigurumi crochet doll.";
        public bool IsFeatureEnabled { get; set; } = true;
    }
}
