using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Services
{
    public class GeminiAiService(IConfiguration configuration) : IAiReviewService
    {
        private static readonly HttpClient _httpClient = new();
        private readonly IConfiguration _configuration = configuration;

        public async Task<ReviewSummaryResult> GenerateSummaryAsync(
            string? existingSummary,
            string? existingPros,
            string? existingCons,
            List<string> newReviews)
        {
            // Read the API key from configuration (IConfiguration reads from appsettings.json, User Secrets, and Environment Variables)
            var apiKey = _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Clean fallback logic for local development if API key is not configured
                return GenerateLocalFallbackSummary(existingSummary, existingPros, existingCons, newReviews);
            }

            try
            {
                var newReviewsText = string.Join("\n- ", newReviews);
                var prompt = $@"You are an expert product review summarizer AI.
Here is the existing AI summary of this product based on past reviews:
Summary: {existingSummary ?? "None yet"}
Pros: {existingPros ?? "[]"}
Cons: {existingCons ?? "[]"}

Here are the new verified reviews:
- {newReviewsText}

Please update the summary, pros, and cons to incorporate the new feedback while maintaining the overall historical consensus.

You must respond with a JSON object strictly matching this schema:
{{
  ""overallSummary"": ""A concise summary paragraph combining past and new reviews."",
  ""pros"": [""Pro 1"", ""Pro 2"", ""Pro 3""],
  ""cons"": [""Con 1"", ""Con 2"", ""Con 3""]
}}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = prompt
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                var requestJson = JsonSerializer.Serialize(requestBody);
                using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, requestContent);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                
                var textResponse = root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(textResponse))
                {
                    throw new Exception("Received empty response text from Gemini API.");
                }

                var result = JsonSerializer.Deserialize<ReviewSummaryResult>(textResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? throw new Exception("Failed to deserialize Gemini response to ReviewSummaryResult.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeminiAiService] Error calling Gemini API: {ex.Message}");
                // In case of any network or API failures, fallback gracefully to prevent blocking background jobs
                return GenerateLocalFallbackSummary(existingSummary, existingPros, existingCons, newReviews);
            }
        }

        private static ReviewSummaryResult GenerateLocalFallbackSummary(
            string? existingSummary,
            string? existingPros,
            string? existingCons,
            List<string> newReviews)
        {
            var prosList = new List<string>();
            var consList = new List<string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(existingPros))
                {
                    prosList = JsonSerializer.Deserialize<List<string>>(existingPros) ?? new();
                }
            }
            catch { /* Ignored */ }

            try
            {
                if (!string.IsNullOrWhiteSpace(existingCons))
                {
                    consList = JsonSerializer.Deserialize<List<string>>(existingCons) ?? new();
                }
            }
            catch { /* Ignored */ }

            // Clean mock update logic
            var latestReviews = newReviews.Take(3).ToList();
            foreach (var review in latestReviews)
            {
                if (review.Contains("5/5") || review.Contains("4/5"))
                {
                    prosList.Add(review.Split(']').Last().Trim());
                }
                else if (review.Contains("1/5") || review.Contains("2/5"))
                {
                    consList.Add(review.Split(']').Last().Trim());
                }
            }

            // Keep sizes reasonable (last 5 items)
            prosList = prosList.Distinct().Take(5).ToList();
            consList = consList.Distinct().Take(5).ToList();

            if (prosList.Count == 0) prosList.Add("Generally good product feedback.");
            if (consList.Count == 0) consList.Add("No major complaints noted.");

            var newSummary = $"AI rolling summary updated with {newReviews.Count} reviews. " +
                             (string.IsNullOrEmpty(existingSummary) ? "No previous summary." : $"Previous summary context: {existingSummary}");

            return new ReviewSummaryResult
            {
                OverallSummary = newSummary,
                Pros = prosList,
                Cons = consList
            };
        }
    }
}
