using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        private bool IsMockMode()
        {
            return string.IsNullOrWhiteSpace(_options.ApiKey) || 
                   _options.ApiKey == "YOUR_GEMINI_API_KEY" || 
                   _options.ApiKey.StartsWith("YOUR_");
        }

        public async Task<GeminiAnalysisResult> AnalyzeConversationAsync(GiftRequestState currentState, string userMessage)
        {
            if (IsMockMode())
            {
                return AnalyzeConversationMock(currentState, userMessage);
            }

            var systemInstruction = @"You are a friendly AI Gift Assistant. Your goal is to help the user find the perfect gift from our catalog by asking follow-up questions and collecting their preferences.
You must gather the following preferences:
- recipientType (e.g. friend, mother, boyfriend, child, colleague)
- ageRange (e.g. kids, teens, adults, seniors, or a specific age)
- interests (e.g. art, gaming, cooking, reading)
- stylePreferences (e.g. vintage, modern, minimalist, handmade)
- colorPreferences (e.g. blue, pastel, warm colors)
- budget (e.g. under $50, $50-$100)
- occasion (e.g. birthday, wedding, graduation, Christmas)
- additionalNotes (any other user preferences)

Follow these rules:
1. Ask only 1-2 questions at a time. Do not overwhelm the user with a long list of questions.
2. Be conversational and natural. Adapt your questions based on what the user has already told you.
3. Keep track of the current preferences state. Update the fields as the user provides details.
4. Parse the budget text. If a clear budget range is mentioned, extract and output the minPrice and maxPrice (as numeric values) in the state.
5. Output your response as a JSON object matching this schema:
{
  ""state"": {
    ""recipientType"": ""string or null"",
    ""ageRange"": ""string or null"",
    ""interests"": [""string""],
    ""stylePreferences"": ""string or null"",
    ""colorPreferences"": [""string""],
    ""budget"": ""string or null"",
    ""occasion"": ""string or null"",
    ""additionalNotes"": ""string or null"",
    ""minPrice"": number or null,
    ""maxPrice"": number or null
  },
  ""reply"": ""Your next follow-up message to the user"",
  ""readyToRecommend"": true/false,
  ""searchQuery"": ""A search query string to search our product catalog if readyToRecommend is true (e.g., 'birthday gift for friend who likes painting'), otherwise null""
}";

            var contextPrompt = $@"Current Gift Preference State:
{JsonSerializer.Serialize(currentState, JsonOptions)}

User's Message:
""{userMessage}""";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = contextPrompt } } }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2
                }
            };

            var jsonText = await CallGeminiApiAsync(requestBody);
            var parsedResult = JsonSerializer.Deserialize<GeminiAnalysisResult>(jsonText, JsonOptions);
            
            return parsedResult ?? throw new Exception("Failed to parse Gemini analysis response.");
        }

        public async Task<GeminiRecommendationResult> ExplainRecommendationsAsync(GiftRequestState state, List<GiftProductDto> candidateProducts)
        {
            if (IsMockMode())
            {
                return ExplainRecommendationsMock(state, candidateProducts);
            }

            var systemInstruction = @"You are a friendly AI Gift Assistant. You have gathered the user's gift preferences and searched our product catalog.
Your task is to write a personalized response explaining why the retrieved candidate products match the user's preferences.
Follow these rules:
1. Recommend ONLY the products from the retrieved catalog. DO NOT hallucinate, guess, or invent any other products.
2. For each product, write a brief, convincing explanation (1-2 sentences) of why it fits their preferences (e.g., recipient type, occasion, interests, budget).
3. Output your response as a JSON object matching this schema:
{
  ""reply"": ""Your overall friendly introductory reply explaining the recommendations (e.g., 'Here are some great options I found for your friend who loves painting!')"",
  ""recommendations"": [
    {
      ""id"": ""product_id_value"",
      ""whyRecommended"": ""Your personalized explanation for this specific product""
    }
  ]
}";

            var contextPrompt = $@"User Gift Preferences:
{JsonSerializer.Serialize(state, JsonOptions)}

Retrieved Candidate Products Catalog:
{JsonSerializer.Serialize(candidateProducts, JsonOptions)}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = contextPrompt } } }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2
                }
            };

            var jsonText = await CallGeminiApiAsync(requestBody);
            var parsedResult = JsonSerializer.Deserialize<GeminiRecommendationResult>(jsonText, JsonOptions);

            return parsedResult ?? throw new Exception("Failed to parse Gemini recommendation response.");
        }

        #region Mock Fallback Reasoning Engine

        private GeminiAnalysisResult AnalyzeConversationMock(GiftRequestState state, string message)
        {
            message = message.ToLowerInvariant();
            
            // 1. Recipient Detection
            if (message.Contains("friend")) state.RecipientType = "Friend";
            else if (message.Contains("mother") || message.Contains("mom") || message.Contains("mama") || message.Contains("mother's")) state.RecipientType = "Mother";
            else if (message.Contains("father") || message.Contains("dad") || message.Contains("papa") || message.Contains("father's")) state.RecipientType = "Father";
            else if (message.Contains("brother")) state.RecipientType = "Brother";
            else if (message.Contains("sister")) state.RecipientType = "Sister";
            else if (message.Contains("wife") || message.Contains("spouse") || message.Contains("partner") || message.Contains("girlfriend")) state.RecipientType = "Partner";
            else if (message.Contains("husband") || message.Contains("boyfriend")) state.RecipientType = "Partner";
            else if (message.Contains("son")) state.RecipientType = "Son";
            else if (message.Contains("daughter")) state.RecipientType = "Daughter";
            else if (message.Contains("child") || message.Contains("kid") || message.Contains("baby")) state.RecipientType = "Child";
            else if (message.Contains("colleague") || message.Contains("boss") || message.Contains("coworker")) state.RecipientType = "Colleague";
            else if (message.Contains("teacher")) state.RecipientType = "Teacher";

            // 2. Occasion Detection
            if (message.Contains("birthday")) state.Occasion = "Birthday";
            else if (message.Contains("wedding") || message.Contains("marriage")) state.Occasion = "Wedding";
            else if (message.Contains("anniversary")) state.Occasion = "Anniversary";
            else if (message.Contains("christmas") || message.Contains("xmas")) state.Occasion = "Christmas";
            else if (message.Contains("graduation")) state.Occasion = "Graduation";
            else if (message.Contains("valentine")) state.Occasion = "Valentine's Day";
            else if (message.Contains("eid")) state.Occasion = "Eid";
            else if (message.Contains("mother's day")) state.Occasion = "Mother's Day";
            else if (message.Contains("father's day")) state.Occasion = "Father's Day";
            else if (message.Contains("housewarming") || message.Contains("new home")) state.Occasion = "Housewarming";
            else if (message.Contains("thank") || message.Contains("appreciation")) state.Occasion = "Thank You";
            else if (message.Contains("just because") || message.Contains("no reason") || message.Contains("surprise")) state.Occasion = "Just Because";

            // 3. Age Range Detection
            if (message.Contains("toddler") || message.Contains("baby") || message.Contains("infant")) state.AgeRange = "0-3 years";
            else if (message.Contains("young kid") || message.Contains("small child")) state.AgeRange = "4-8 years";
            else if (message.Contains("teenager") || message.Contains("teen")) state.AgeRange = "13-19 years";
            else if (message.Contains("young adult") || message.Contains("20s") || message.Contains("twenties")) state.AgeRange = "20-29 years";
            else if (message.Contains("30s") || message.Contains("thirties") || message.Contains("middle age")) state.AgeRange = "30-45 years";
            else if (message.Contains("50s") || message.Contains("senior") || message.Contains("elderly") || message.Contains("older")) state.AgeRange = "50+ years";
            else if (message.Contains("adult")) state.AgeRange = "Adults";
            else if (message.Contains("kid") || message.Contains("child")) state.AgeRange = "Kids";

            // 4. Budget Detection
            if (message.Contains("affordable") || message.Contains("cheap") || message.Contains("under 25") || message.Contains("under $25"))
            {
                state.Budget = "Under $25";
                state.MinPrice = 0;
                state.MaxPrice = 25;
            }
            else if (message.Contains("under 50") || message.Contains("less than 50") || message.Contains("under $50") || message.Contains("50 dollars") || message.Contains("cheaper"))
            {
                state.Budget = "Under $50";
                state.MinPrice = 0;
                state.MaxPrice = 50;
            }
            else if (message.Contains("50 to 100") || message.Contains("between 50 and 100") || message.Contains("50-100") || message.Contains("moderate") || message.Contains("medium"))
            {
                state.Budget = "$50 - $100";
                state.MinPrice = 50;
                state.MaxPrice = 100;
            }
            else if (message.Contains("100 to 200") || message.Contains("100-200") || message.Contains("premium"))
            {
                state.Budget = "$100 - $200";
                state.MinPrice = 100;
                state.MaxPrice = 200;
            }
            else if (message.Contains("luxury") || message.Contains("expensive") || message.Contains("high end") || message.Contains("above 100") || message.Contains("over 100") || message.Contains("no limit") || message.Contains("any budget"))
            {
                state.Budget = "Luxury ($200+)";
                state.MinPrice = 200;
                state.MaxPrice = 10000;
            }

            // 5. Interests Detection
            var interests = new[] { "art", "gaming", "music", "reading", "cooking", "sports", "gardening", "fashion", "tech", "handmade", "painting", "crafts", "jewelry", "pottery", "knitting", "photography", "travel", "fitness", "yoga", "meditation", "home decor", "candles" };
            foreach (var interest in interests)
            {
                if (message.Contains(interest) && !state.Interests.Contains(interest, StringComparer.OrdinalIgnoreCase))
                {
                    state.Interests.Add(char.ToUpper(interest[0]) + interest.Substring(1));
                }
            }

            // 6. Colors Detection
            var colors = new[] { "blue", "red", "green", "pink", "black", "white", "gold", "yellow", "purple", "pastel", "neutral", "earth tones", "warm", "cool" };
            foreach (var color in colors)
            {
                if (message.Contains(color) && !state.ColorPreferences.Contains(color, StringComparer.OrdinalIgnoreCase))
                {
                    state.ColorPreferences.Add(char.ToUpper(color[0]) + color.Substring(1));
                }
            }

            // 7. Style Detection
            if (message.Contains("vintage") || message.Contains("classic") || message.Contains("retro")) state.StylePreferences = "Vintage";
            else if (message.Contains("modern") || message.Contains("contemporary") || message.Contains("sleek")) state.StylePreferences = "Modern";
            else if (message.Contains("minimalist") || message.Contains("simple") || message.Contains("clean")) state.StylePreferences = "Minimalist";
            else if (message.Contains("handmade") || message.Contains("crafted") || message.Contains("artisan")) state.StylePreferences = "Handmade & Artisan";
            else if (message.Contains("bohemian") || message.Contains("boho")) state.StylePreferences = "Bohemian";
            else if (message.Contains("rustic") || message.Contains("farmhouse")) state.StylePreferences = "Rustic";
            else if (message.Contains("elegant") || message.Contains("luxurious") || message.Contains("fancy")) state.StylePreferences = "Elegant";

            // 8. Decide next question or if ready to recommend
            var reply = "";
            var ready = false;
            string? searchQuery = null;

            if (string.IsNullOrEmpty(state.RecipientType))
            {
                reply = "Hi there! ✨ I'd love to help you find the perfect handmade gift. Who are you shopping for? For example, a friend, your mom, a partner, or someone else?";
            }
            else if (string.IsNullOrEmpty(state.Occasion))
            {
                reply = $"A gift for your {state.RecipientType.ToLower()} — great choice! 🎁 What's the occasion? It could be a birthday, wedding, holiday, or even \"just because\"!";
            }
            else if (string.IsNullOrEmpty(state.Budget))
            {
                reply = $"Perfect! A {state.Occasion.ToLower()} gift for your {state.RecipientType.ToLower()}. 💰 What's your budget range? Something affordable (under $50), moderate ($50–$100), or more premium?";
            }
            else if (state.Interests.Count == 0)
            {
                reply = $"Almost there! What are they into? 🎨 Think hobbies or passions — like art, cooking, gaming, reading, jewelry, home decor, or anything handmade.";
            }
            else
            {
                ready = true;
                var interestsText = string.Join(", ", state.Interests.Select(i => i.ToLower()));
                var styleText = !string.IsNullOrEmpty(state.StylePreferences) ? $" {state.StylePreferences.ToLower()} style" : "";
                searchQuery = $"{state.Occasion} gift for {state.RecipientType} who likes {interestsText}{styleText} budget {state.Budget}";
                reply = $"I've got a clear picture now! 🔍 Let me search our handmade collection for the perfect {state.Occasion.ToLower()} gift...";
            }

            return new GeminiAnalysisResult
            {
                State = state,
                Reply = reply,
                ReadyToRecommend = ready,
                SearchQuery = searchQuery
            };
        }

        private GeminiRecommendationResult ExplainRecommendationsMock(GiftRequestState state, List<GiftProductDto> candidateProducts)
        {
            var explanations = new[]
            {
                "A wonderful match for someone who appreciates {0} — handcrafted with care and perfect for a {1} gift.",
                "This piece captures the essence of {0} beautifully. It's within your budget and ideal for {1}.",
                "Handpicked for your {2}'s love of {0}. The craftsmanship makes it a truly memorable {1} present.",
                "This unique find combines quality and creativity — great for anyone into {0}, especially for {1}.",
                "A standout piece that blends artisan quality with {0} vibes. Your {2} will love unwrapping this!"
            };

            var interestsStr = state.Interests.Count > 0 ? string.Join(" and ", state.Interests.Select(i => i.ToLower())) : "unique crafts";
            var occasion = state.Occasion?.ToLower() ?? "special occasion";
            var recipient = state.RecipientType?.ToLower() ?? "loved one";

            var recs = new List<RecommendationExplanation>();
            for (var i = 0; i < candidateProducts.Count; i++)
            {
                var template = explanations[i % explanations.Length];
                recs.Add(new RecommendationExplanation
                {
                    Id = candidateProducts[i].Id,
                    WhyRecommended = string.Format(template, interestsStr, occasion, recipient)
                });
            }

            return new GeminiRecommendationResult
            {
                Reply = $"Here's what I found from our handmade collection! Each piece was selected based on your {recipient}'s interests and the {occasion} occasion. Take a look:",
                Recommendations = recs
            };
        }

        #endregion

        #region Private Helpers

        private async Task<string> CallGeminiApiAsync(object requestBody)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.ChatModel}:generateContent?key={_options.ApiKey}";
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error. Status: {response.StatusCode}. Details: {errorText}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var contentProp) &&
                contentProp.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textProp))
            {
                var rawText = textProp.GetString() ?? string.Empty;
                return SanitizeJson(rawText);
            }

            throw new Exception("Unexpected response structure from Gemini API: " + responseString);
        }

        private string SanitizeJson(string rawText)
        {
            rawText = rawText.Trim();
            if (rawText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                rawText = rawText.Substring(7);
            }
            else if (rawText.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                rawText = rawText.Substring(3);
            }

            if (rawText.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                rawText = rawText.Substring(0, rawText.Length - 3);
            }

            return rawText.Trim();
        }

        #endregion
    }
}
