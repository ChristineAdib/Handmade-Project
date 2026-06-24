using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HandoraApplication.IServices;

namespace HandoraApplication.AI.Services
{
    public class GiftAssistantService(
        IGiftConversationManager conversationManager,
        IGeminiService geminiService,
        IVectorStoreService vectorStoreService,
        IEmbeddingService embeddingService,
        IOptions<QdrantOptions> qdrantOptions,
        IProductService productService) : IGiftAssistantService
    {
        private readonly IGiftConversationManager _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        private readonly IGeminiService _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        private readonly IVectorStoreService _vectorStoreService = vectorStoreService ?? throw new ArgumentNullException(nameof(vectorStoreService));
        private readonly IEmbeddingService _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        private readonly QdrantOptions _qdrantOptions = qdrantOptions?.Value ?? throw new ArgumentNullException(nameof(qdrantOptions));
        private readonly IProductService _productService = productService ?? throw new ArgumentNullException(nameof(productService));

        public async Task<GiftChatResponseDto> ProcessChatAsync(GiftChatRequestDto request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // 1. Retrieve current conversation state and history
            var currentState = await _conversationManager.GetStateAsync(request.SessionId);
            var history = await _conversationManager.GetHistoryAsync(request.SessionId);

            // Count the number of assistant questions asked so far in this session
            var questionsAsked = history.Count(h => h.Role == "assistant");

            // 2. Add user message to history
            history.Add(new ChatHistoryEntry { Role = "user", Content = request.Message });

            // 3. Run LLM reasoning to update state and determine next step
            var analysis = await _geminiService.AnalyzeConversationAsync(currentState, request.Message, questionsAsked);

            // Deterministically enforce the 5-question limit in the application layer
            if (questionsAsked >= 5 && !analysis.ReadyToRecommend)
            {
                analysis.ReadyToRecommend = true;
                if (string.IsNullOrWhiteSpace(analysis.SearchQuery))
                {
                    var interestsText = analysis.State.Interests.Count > 0 ? string.Join(", ", analysis.State.Interests.Select(i => i.ToLower())) : "handmade items";
                    var styleText = !string.IsNullOrEmpty(analysis.State.StylePreferences) ? $" {analysis.State.StylePreferences.ToLower()} style" : "";
                    var recipientText = !string.IsNullOrEmpty(analysis.State.RecipientType) ? $" for {analysis.State.RecipientType}" : "";
                    var occasionText = !string.IsNullOrEmpty(analysis.State.Occasion) ? $" {analysis.State.Occasion}" : "special occasion";
                    var budgetText = !string.IsNullOrEmpty(analysis.State.Budget) ? $" budget {analysis.State.Budget}" : "";
                    analysis.SearchQuery = $"{occasionText} gift{recipientText} who likes {interestsText}{styleText}{budgetText}";
                }
            }

            var reply = analysis.Reply;
            var finalProducts = new List<GiftProductDto>();

            // 4. If ready to recommend, perform RAG search
            if (analysis.ReadyToRecommend && !string.IsNullOrWhiteSpace(analysis.SearchQuery))
            {
                var (products, explanationReply) = await SearchAndExplainProductsAsync(analysis);
                finalProducts = products;
                if (finalProducts.Count == 0)
                {
                    reply = "I searched our catalog but couldn't find an exact match for your criteria. Could you try broadening your preferences a bit? For example, a wider budget range or different interests might help me find something special.";
                }
                else if (!string.IsNullOrWhiteSpace(explanationReply))
                {
                    reply = explanationReply;
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

        private async Task<(List<GiftProductDto> Products, string ExplanationReply)> SearchAndExplainProductsAsync(GeminiAnalysisResult analysis)
        {
            var collectionName = GetProductsCollectionName();
            var finalProducts = new List<GiftProductDto>();
            var explanationReply = string.Empty;

            try
            {
                // Generate vector embedding for the LLM-optimized search query
                var queryVector = await _embeddingService.GetEmbeddingAsync(analysis.SearchQuery!);

                // Retrieve top 50 candidates from Qdrant vector store
                var searchResults = await _vectorStoreService.SearchAsync(
                    collectionName: collectionName,
                    embedding: queryVector,
                    topK: 50
                );

                // Map & filter candidate products, querying the ProductService sequentially to prevent EF Core concurrency exceptions
                var validCandidates = new List<(GiftProductDto Dto, double RankingScore, Guid CategoryId, Guid? ParentCategoryId)>();

                foreach (var hit in searchResults)
                {
                    if (hit.Metadata == null || !hit.Metadata.TryGetValue("product_id", out var idObj) || idObj == null)
                        continue;

                    var productId = idObj.ToString() ?? string.Empty;
                    if (!Guid.TryParse(productId, out var prodGuid))
                        continue;

                    try
                    {
                        var productResult = await _productService.GetProduct(prodGuid);
                        if (productResult != null && productResult.IsSuccess && productResult.Data != null)
                        {
                            var prod = productResult.Data;
                            // Ensure the product is active and has Quantity > 0 (in-stock)
                            if (prod.Quantity > 0 && !prod.IsSoldOut && prod.IsActive && prod.Status == "Active")
                            {
                                var title = hit.Metadata.TryGetValue("title", out var titleObj) ? titleObj?.ToString() ?? string.Empty : string.Empty;
                                var price = hit.Metadata.TryGetValue("price", out var priceObj) ? Convert.ToDecimal(priceObj) : 0m;
                                var description = hit.Metadata.TryGetValue("description", out var descObj) ? descObj?.ToString() ?? string.Empty : string.Empty;
                                var imageUrl = hit.Metadata.TryGetValue("imageUrl", out var imageObj) ? imageObj?.ToString() ?? string.Empty : string.Empty;

                                // 1. Category match (20% weight)
                                double categoryScore = 0.0;
                                if (analysis.State.Interests != null && analysis.State.Interests.Count > 0)
                                {
                                    int matches = 0;
                                    foreach (var interest in analysis.State.Interests)
                                    {
                                        bool matchCategory = (!string.IsNullOrEmpty(prod.CategoryNameEn) && prod.CategoryNameEn.Contains(interest, StringComparison.OrdinalIgnoreCase)) ||
                                                             (!string.IsNullOrEmpty(prod.ParentCategoryNameEn) && prod.ParentCategoryNameEn.Contains(interest, StringComparison.OrdinalIgnoreCase));
                                        if (matchCategory)
                                        {
                                            matches++;
                                        }
                                    }
                                    categoryScore = (double)matches / analysis.State.Interests.Count;
                                }
                                if (!string.IsNullOrEmpty(analysis.SearchQuery))
                                {
                                    bool queryMatch = (!string.IsNullOrEmpty(prod.CategoryNameEn) && analysis.SearchQuery.Contains(prod.CategoryNameEn, StringComparison.OrdinalIgnoreCase)) ||
                                                       (!string.IsNullOrEmpty(prod.ParentCategoryNameEn) && analysis.SearchQuery.Contains(prod.ParentCategoryNameEn, StringComparison.OrdinalIgnoreCase));
                                    if (queryMatch)
                                    {
                                        categoryScore = Math.Max(categoryScore, 1.0);
                                    }
                                }

                                // 2. Tag match (20% weight)
                                double tagScore = 0.0;
                                if (prod.Tags != null && prod.Tags.Count > 0)
                                {
                                    int matchingTagsCount = 0;
                                    if (analysis.State.Interests != null)
                                    {
                                        foreach (var tag in prod.Tags)
                                        {
                                            if (analysis.State.Interests.Any(interest => interest.Contains(tag, StringComparison.OrdinalIgnoreCase) || tag.Contains(interest, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                matchingTagsCount++;
                                            }
                                        }
                                    }
                                    tagScore = (double)matchingTagsCount / prod.Tags.Count;

                                    if (!string.IsNullOrEmpty(analysis.SearchQuery))
                                    {
                                        int queryMatchingTags = prod.Tags.Count(tag => analysis.SearchQuery.Contains(tag, StringComparison.OrdinalIgnoreCase));
                                        double queryTagScore = (double)queryMatchingTags / prod.Tags.Count;
                                        tagScore = Math.Max(tagScore, queryTagScore);
                                    }
                                }

                                // 3. Price proximity (10% weight)
                                double priceProximityScore = 1.0;
                                if (analysis.State.MinPrice.HasValue || analysis.State.MaxPrice.HasValue)
                                {
                                    var min = analysis.State.MinPrice ?? 0m;
                                    var max = analysis.State.MaxPrice ?? 999999m;
                                    
                                    if (prod.FinalPrice >= min && prod.FinalPrice <= max)
                                    {
                                        priceProximityScore = 1.0;
                                    }
                                    else
                                    {
                                        decimal targetPrice = 0m;
                                        if (analysis.State.MinPrice.HasValue && analysis.State.MaxPrice.HasValue)
                                        {
                                            targetPrice = (analysis.State.MinPrice.Value + analysis.State.MaxPrice.Value) / 2;
                                        }
                                        else if (analysis.State.MaxPrice.HasValue)
                                        {
                                            targetPrice = analysis.State.MaxPrice.Value;
                                        }
                                        else if (analysis.State.MinPrice.HasValue)
                                        {
                                            targetPrice = analysis.State.MinPrice.Value;
                                        }

                                        var priceDiff = Math.Abs(prod.FinalPrice - targetPrice);
                                        priceProximityScore = 1.0 / (1.0 + (double)priceDiff);
                                    }
                                }

                                // 4. Name keyword match (10% weight)
                                double nameKeywordScore = 0.0;
                                if (!string.IsNullOrEmpty(analysis.SearchQuery) && !string.IsNullOrEmpty(prod.TitleEn))
                                {
                                    var queryWords = analysis.SearchQuery
                                        .Split(new[] { ' ', ',', '.', ';', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Where(w => w.Length >= 3)
                                        .Select(w => w.ToLowerInvariant())
                                        .ToList();

                                    if (queryWords.Count > 0)
                                    {
                                        int matchedWords = queryWords.Count(word => prod.TitleEn.Contains(word, StringComparison.OrdinalIgnoreCase));
                                        nameKeywordScore = (double)matchedWords / queryWords.Count;
                                    }
                                }

                                // Combined reranking score formula:
                                // Semantic similarity (40%) + Category match (20%) + Tag match (20%) + Price proximity (10%) + Name keyword match (10%)
                                double totalScore = (0.40 * hit.Score) +
                                                    (0.20 * categoryScore) +
                                                    (0.20 * tagScore) +
                                                    (0.10 * priceProximityScore) +
                                                    (0.10 * nameKeywordScore);

                                var dto = new GiftProductDto
                                {
                                    Id = productId,
                                    Title = title,
                                    Price = price,
                                    Description = description,
                                    ImageUrl = imageUrl
                                };

                                validCandidates.Add((dto, totalScore, prod.CategoryId, prod.ParentCategoryId));
                            }
                        }
                    }
                    catch
                    {
                        // Ignore individual service failures to allow other products to succeed
                    }
                }

                // Sort candidates by final ranking score descending
                var sortedCandidates = validCandidates
                    .OrderByDescending(c => c.RankingScore)
                    .ToList();

                var filteredProductsList = new List<GiftProductDto>();
                var usedCategoryKeys = new HashSet<Guid>();

                // First pass: try to pick products from unique categories (Parent category if exists, otherwise subcategory CategoryId)
                foreach (var candidate in sortedCandidates)
                {
                    var catKey = candidate.ParentCategoryId ?? candidate.CategoryId;
                    if (!usedCategoryKeys.Contains(catKey))
                    {
                        filteredProductsList.Add(candidate.Dto);
                        usedCategoryKeys.Add(catKey);
                    }

                    if (filteredProductsList.Count >= 3)
                        break;
                }

                // Second pass fallback: if we don't have 3 products, relax category diversity constraint
                if (filteredProductsList.Count < 3)
                {
                    foreach (var candidate in sortedCandidates)
                    {
                        if (!filteredProductsList.Any(p => p.Id == candidate.Dto.Id))
                        {
                            filteredProductsList.Add(candidate.Dto);
                        }

                        if (filteredProductsList.Count >= 3)
                            break;
                    }
                }

                if (filteredProductsList.Count > 0)
                {
                    // Run LLM reasoning to explain why these products fit
                    var explanationResult = await _geminiService.ExplainRecommendationsAsync(analysis.State, filteredProductsList);
                    explanationReply = explanationResult?.Reply ?? string.Empty;

                    // Merge explanations back into product DTOs
                    foreach (var prod in filteredProductsList)
                    {
                        var matchedExp = explanationResult?.Recommendations?.FirstOrDefault(r => r.Id == prod.Id);
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

            return (finalProducts, explanationReply);
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
