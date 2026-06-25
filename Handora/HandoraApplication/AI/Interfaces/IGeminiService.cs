using HandoraApplication.AI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Interfaces
{
    public interface IGeminiService
    {
        /// <summary>
        /// Analyzes the conversation context, merges user input, updates state, and outputs the next conversational reply plus whether RAG catalog query should trigger.
        /// </summary>
        Task<GeminiAnalysisResult> AnalyzeConversationAsync(GiftRequestState currentState, string userMessage, int questionsAsked = 0);

        /// <summary>
        /// Takes candidate products retrieved via RAG and explains how each fits the user's gift preferences, returning structured reasons.
        /// </summary>
        Task<GeminiRecommendationResult> ExplainRecommendationsAsync(GiftRequestState state, List<GiftProductDto> candidateProducts);
    }

    public class GeminiAnalysisResult
    {
        public GiftRequestState State { get; set; } = new();
        public string Reply { get; set; } = string.Empty;
        public bool ReadyToRecommend { get; set; }
        public string? SearchQuery { get; set; }
    }

    public class GeminiRecommendationResult
    {
        public string Reply { get; set; } = string.Empty;
        public List<RecommendationExplanation> Recommendations { get; set; } = new();
    }

    public class RecommendationExplanation
    {
        public string Id { get; set; } = string.Empty;
        public string WhyRecommended { get; set; } = string.Empty;
    }
}
