using HandoraApplication.AI.DTOs;
using HandoraDomain.Models.CustomStudioEntities;

namespace HandoraApplication.AI.Interfaces
{
    public interface IAIPromptBuilder
    {
        PromptBuildResult BuildPrompt(CustomConfiguration configuration);
        PromptBuildResult BuildPromptWithVariation(CustomConfiguration configuration, int variationIndex);
    }
}
