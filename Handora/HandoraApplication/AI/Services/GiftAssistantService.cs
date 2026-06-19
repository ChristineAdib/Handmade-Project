using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Services
{
    public class GiftAssistantService(
        IGiftConversationManager conversationManager,
        IGeminiService geminiService,
        IVectorStoreService vectorStoreService,
        IEmbeddingService embeddingService,
        IOptions<QdrantOptions> qdrantOptions) : IGiftAssistantService
    {
        private readonly IGiftConversationManager _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        private readonly IGeminiService _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        private readonly IVectorStoreService _vectorStoreService = vectorStoreService ?? throw new ArgumentNullException(nameof(vectorStoreService));
        private readonly IEmbeddingService _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        private readonly QdrantOptions _qdrantOptions = qdrantOptions?.Value ?? throw new ArgumentNullException(nameof(qdrantOptions));

        public async Task<GiftChatResponseDto> ProcessChatAsync(GiftChatRequestDto request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // 1. Retrieve current conversation state and history
            var currentState = await _conversationManager.GetStateAsync(request.SessionId);
            var history = await _conversationManager.GetHistoryAsync(request.SessionId);

            // 2. Add user message to history
            history.Add(new ChatHistoryEntry { Role = "user", Content = request.Message });

            // 3. Run LLM reasoning to update state and determine next step
            var analysis = await _geminiService.AnalyzeConversationAsync(currentState, request.Message);

            var reply = analysis.Reply;
            var finalProducts = new List<GiftProductDto>();

            // 4. If ready to recommend, perform RAG search
            if (analysis.ReadyToRecommend && !string.IsNullOrWhiteSpace(analysis.SearchQuery))
            {
                finalProducts = await SearchAndExplainProductsAsync(analysis);
                if (finalProducts.Count == 0)
                {
                    reply = "I searched our catalog but couldn't find an exact match for your criteria. Could you try broadening your preferences a bit? For example, a wider budget range or different interests might help me find something special.";
                }
            }

            // 5. Add assistant reply to history
            history.Add(new ChatHistoryEntry { Role = "assistant", Content = reply });

            // 6. Persist updated state and history
            await _conversationManager.SaveStateAsync(request.SessionId, analysis.State);
            await _conversationManager.SaveHistoryAsync(request.SessionId, history);

            return new GiftChatResponseDto
            {
                Reply = reply,
                Products = finalProducts,
                State = analysis.State
            };
        }

        public async Task ResetSessionAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            await _conversationManager.ClearStateAsync(sessionId);
        }

        private async Task<List<GiftProductDto>> SearchAndExplainProductsAsync(GeminiAnalysisResult analysis)
        {
            var collectionName = GetProductsCollectionName();
            var finalProducts = new List<GiftProductDto>();

            try
            {
                // Generate vector embedding for the LLM-optimized search query
                var queryVector = await _embeddingService.GetEmbeddingAsync(analysis.SearchQuery!);

                // Retrieve top 20 candidates from Qdrant vector store
                var searchResults = await _vectorStoreService.SearchAsync(
                    collectionName: collectionName,
                    embedding: queryVector,
                    topK: 20
                );

                // Map & filter candidate products
                var candidateProducts = new List<GiftProductDto>();
                foreach (var hit in searchResults)
                {
                    if (hit.Metadata == null) continue;
                    if (!hit.Metadata.TryGetValue("product_id", out var idObj) || idObj == null) continue;

                    var productId = idObj.ToString() ?? string.Empty;
                    var title = hit.Metadata.TryGetValue("title", out var titleObj) ? titleObj?.ToString() ?? string.Empty : string.Empty;
                    var price = hit.Metadata.TryGetValue("price", out var priceObj) ? Convert.ToDecimal(priceObj) : 0m;
                    var description = hit.Metadata.TryGetValue("description", out var descObj) ? descObj?.ToString() ?? string.Empty : string.Empty;
                    var imageUrl = hit.Metadata.TryGetValue("imageUrl", out var imageObj) ? imageObj?.ToString() ?? string.Empty : string.Empty;

                    // Apply price filters extracted by the AI reasoning engine
                    if (analysis.State.MinPrice.HasValue && price < analysis.State.MinPrice.Value) continue;
                    if (analysis.State.MaxPrice.HasValue && price > analysis.State.MaxPrice.Value) continue;

                    candidateProducts.Add(new GiftProductDto
                    {
                        Id = productId,
                        Title = title,
                        Price = price,
                        Description = description,
                        ImageUrl = imageUrl
                    });
                }

                // Select top 3 matching products
                var filteredProductsList = candidateProducts.Take(3).ToList();

                if (filteredProductsList.Count > 0)
                {
                    // Run LLM reasoning to explain why these products fit
                    var explanationResult = await _geminiService.ExplainRecommendationsAsync(analysis.State, filteredProductsList);

                    // Merge explanations back into product DTOs
                    foreach (var prod in filteredProductsList)
                    {
                        var matchedExp = explanationResult.Recommendations.FirstOrDefault(r => r.Id == prod.Id);
                        prod.WhyRecommended = matchedExp?.WhyRecommended ?? "Fits your specified preferences.";
                        finalProducts.Add(prod);
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle search failures — return empty list
                // The caller will show a graceful "no matches" message
            }

            return finalProducts;
        }

        private string GetProductsCollectionName()
        {
            var baseCollection = string.IsNullOrWhiteSpace(_qdrantOptions.Collection)
                ? "handora-documents"
                : _qdrantOptions.Collection;

            return $"{baseCollection}-products";
        }
    }
}
