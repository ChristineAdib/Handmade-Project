using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GeminiService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private bool IsMockMode()
        {
            return string.IsNullOrWhiteSpace(_options.ApiKey) || 
                   _options.ApiKey == "YOUR_GEMINI_API_KEY" || 
                   _options.ApiKey.StartsWith("YOUR_");
        }

        public async Task<GeminiAnalysisResult> AnalyzeConversationAsync(GiftRequestState currentState, string userMessage, int questionsAsked = 0)
        {
            if (IsMockMode())
            {
                return AnalyzeConversationMock(currentState, userMessage, questionsAsked);
            }

            var systemInstruction = @"You are a friendly AI Gift Assistant. Your goal is to help the user find the perfect gift from our catalog by asking follow-up questions and collecting their preferences.
You must gather the following preferences:
- recipientType (e.g. friend, mother, boyfriend, child, colleague)
- ageRange (e.g. kids, teens, adults, seniors, or a specific age)
- interests (e.g. art, gaming, cooking, reading)
- stylePreferences (e.g. vintage, modern, minimalist, handmade)
- colorPreferences (e.g. blue, pastel, warm colors)
- budget (e.g. under 250 EGP, 250-500 EGP)
- occasion (e.g. birthday, wedding, graduation, Christmas)
- additionalNotes (any other user preferences)

Follow these rules:
1. Ask only 1-2 questions at a time. Do not overwhelm the user with a long list of questions.
2. Be conversational and natural. Adapt your questions based on what the user has already told you.
3. Keep track of the current preferences state. Update the fields as the user provides details.
4. Parse the budget text. If a specific price (e.g. ""price 99"", ""99 EGP"") or budget range/limit is mentioned, extract and output the minPrice and maxPrice (as numeric values) in the state. If the user specifies an exact target price (e.g. 99), set both minPrice and maxPrice to that value (99).
5. You must ask a maximum of 5 questions before generating recommendations. If you have already asked 5 questions (i.e. 'Number of questions asked by the assistant so far' is 5 or more), or if you already have enough information before reaching 5 questions, you MUST set readyToRecommend to true, stop asking any more questions, and generate a search query string to search our product catalog based on the preferences collected so far.
6. You only recommend in-stock products from our catalog, and never invent or suggest unavailable or out-of-stock items.
7. Output your response as a JSON object matching this schema:
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

Number of questions asked by the assistant so far: {questionsAsked}

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
1. Recommend ONLY the products from the retrieved catalog, which are verified to be in-stock. DO NOT hallucinate, guess, or invent any other products, and never suggest unavailable or out-of-stock items.
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

        private GeminiAnalysisResult AnalyzeConversationMock(GiftRequestState state, string message, int questionsAsked)
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

            // 4. Budget & Price Detection
            var numbers = System.Text.RegularExpressions.Regex.Matches(message, @"\b\d+(?:\.\d+)?\b")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => decimal.Parse(m.Value))
                .ToList();

            if (numbers.Count > 0)
            {
                if (numbers.Count == 1)
                {
                    var val = numbers[0];
                    if (message.Contains("under") || message.Contains("less than") || message.Contains("below") || message.Contains("max") || message.Contains("up to"))
                    {
                        state.MinPrice = 0;
                        state.MaxPrice = val;
                        state.Budget = $"Under ${val}";
                    }
                    else if (message.Contains("above") || message.Contains("more than") || message.Contains("at least") || message.Contains("min") || message.Contains("over"))
                    {
                        state.MinPrice = val;
                        state.MaxPrice = 999999;
                        state.Budget = $"Above ${val}";
                    }
                    else
                    {
                        // Exact target price match (e.g. "price 99")
                        state.MinPrice = val;
                        state.MaxPrice = val;
                        state.Budget = $"Around ${val}";
                    }
                }
                else if (numbers.Count >= 2)
                {
                    var sorted = numbers.OrderBy(n => n).ToList();
                    state.MinPrice = sorted[0];
                    state.MaxPrice = sorted[1];
                    state.Budget = $"{sorted[0]} - {sorted[1]} EGP";
                }
            }
            else
            {
                // Fallback to standard descriptive terms if no exact number is present
                if (message.Contains("affordable") || message.Contains("cheap"))
                {
                    state.Budget = "Under 250 EGP";
                    state.MinPrice = 0;
                    state.MaxPrice = 250;
                }
                else if (message.Contains("moderate") || message.Contains("medium"))
                {
                    state.Budget = "250 - 500 EGP";
                    state.MinPrice = 250;
                    state.MaxPrice = 500;
                }
                else if (message.Contains("premium"))
                {
                    state.Budget = "500 - 1000 EGP";
                    state.MinPrice = 500;
                    state.MaxPrice = 1000;
                }
                else if (message.Contains("luxury") || message.Contains("expensive") || message.Contains("high end") || message.Contains("no limit") || message.Contains("any budget"))
                {
                    state.Budget = "Luxury (1000+ EGP)";
                    state.MinPrice = 1000;
                    state.MaxPrice = 10000;
                }
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

            if (questionsAsked >= 5 || (!string.IsNullOrEmpty(state.RecipientType) && !string.IsNullOrEmpty(state.Occasion) && !string.IsNullOrEmpty(state.Budget) && state.Interests.Count > 0))
            {
                ready = true;
                var interestsText = state.Interests.Count > 0 ? string.Join(", ", state.Interests.Select(i => i.ToLower())) : "handmade items";
                var styleText = !string.IsNullOrEmpty(state.StylePreferences) ? $" {state.StylePreferences.ToLower()} style" : "";
                var recipientText = !string.IsNullOrEmpty(state.RecipientType) ? $" for {state.RecipientType}" : "";
                var occasionText = !string.IsNullOrEmpty(state.Occasion) ? $" {state.Occasion}" : "special occasion";
                var budgetText = !string.IsNullOrEmpty(state.Budget) ? $" budget {state.Budget}" : "";

                searchQuery = $"{occasionText} gift{recipientText} who likes {interestsText}{styleText}{budgetText}";
                reply = $"I've got a clear picture now! 🔍 Let me search our handmade collection for the perfect gift...";
            }
            else if (string.IsNullOrEmpty(state.RecipientType))
            {
                reply = "Hi there! ✨ I'd love to help you find the perfect handmade gift. Who are you shopping for? For example, a friend, your mom, a partner, or someone else?";
            }
            else if (string.IsNullOrEmpty(state.Occasion))
            {
                reply = $"A gift for your {state.RecipientType.ToLower()} — great choice! 🎁 What's the occasion? It could be a birthday, wedding, holiday, or even \"just because\"!";
            }
            else if (string.IsNullOrEmpty(state.Budget))
            {
                reply = $"Perfect! A {state.Occasion.ToLower()} gift for your {state.RecipientType.ToLower()}. 💰 What's your budget range? Something affordable (under 250 EGP), moderate (250–500 EGP), or more premium?";
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

        public async Task<string> AnalyzeCrochetDollPhotoAsync(string base64Image, string mimeType, CancellationToken ct = default)
        {
            if (IsMockMode())
            {
                return @"{
                  ""personIdentity"": {
                    ""gender"": ""Female"",
                    ""estimatedAgeRange"": ""20s"",
                    ""faceShape"": ""Oval"",
                    ""skinTone"": ""Fair"",
                    ""facialFeatures"": ""Brown eyes, natural lips"",
                    ""expression"": ""Smile"",
                    ""glasses"": ""No"",
                    ""glassesDetails"": ""None"",
                    ""facialHair"": ""No"",
                    ""facialHairDetails"": ""None"",
                    ""frecklesMolesDimples"": ""No""
                  },
                  ""hairOrHeadCoverage"": {
                    ""hairVisible"": ""Yes"",
                    ""hairStyle"": ""Straight"",
                    ""hairLength"": ""Long"",
                    ""hairColor"": ""Chestnut Brown"",
                    ""headCovered"": ""No"",
                    ""coverType"": ""None"",
                    ""hijabOrScarfStyle"": ""None"",
                    ""hijabOrScarfColors"": ""None"",
                    ""hairlineVisible"": ""Yes"",
                    ""anyHairShowing"": ""Yes"",
                    ""modestyLevel"": ""Medium""
                  },
                  ""clothing"": {
                    ""topType"": ""Sweater"",
                    ""topColor"": ""Beige"",
                    ""patternTexturePrint"": ""No"",
                    ""outerwear"": ""No"",
                    ""bottomType"": ""Skirt"",
                    ""bottomColor"": ""Brown"",
                    ""fullOutfitStyle"": ""Casual""
                  },
                  ""accessories"": {
                    ""headAccessories"": ""None"",
                    ""jewelry"": ""None"",
                    ""bagOrPurse"": ""No"",
                    ""shoes"": ""Flats"",
                    ""otherAccessories"": ""None""
                  },
                  ""otherVisualDetails"": {
                    ""dominantColors"": ""Beige, Brown"",
                    ""background"": ""Indoor"",
                    ""lighting"": ""Natural""
                  }
                }";
            }

            var promptText = @"Analyze the uploaded photo of a person. You must extract and describe their appearance details with extreme accuracy before generating a crochet doll.
Do NOT guess. Do NOT invent. Do NOT change religious, cultural, or personal attributes. Preserve every visual detail exactly as seen in the photo.

Output your response as a JSON object matching this schema:
{
  ""personIdentity"": {
    ""gender"": ""Male"" or ""Female"",
    ""estimatedAgeRange"": ""Teen"" or ""20s"" or ""30s"" or ""40s"" or ""50s+"",
    ""faceShape"": ""Oval"" or ""Round"" or ""Square"" or ""Heart"" or ""Long"",
    ""skinTone"": ""Very Fair"" or ""Fair"" or ""Light"" or ""Medium"" or ""Tan"" or ""Deep"",
    ""facialFeatures"": ""Detailed description of eyes, eyebrows, nose, lips, cheeks, chin"",
    ""expression"": ""Neutral"" or ""Smile"" or ""Big Smile"" or ""Serious"",
    ""glasses"": ""Yes"" or ""No"",
    ""glassesDetails"": ""Type and frame color if glasses are worn"",
    ""facialHair"": ""Yes"" or ""No"",
    ""facialHairDetails"": ""Type, style, and color if facial hair is present"",
    ""frecklesMolesDimples"": ""Yes/No + Type + Location if present""
  },
  ""hairOrHeadCoverage"": {
    ""hairVisible"": ""Yes"" or ""No"",
    ""hairStyle"": ""Style description if hair is visible"",
    ""hairLength"": ""Length description if hair is visible"",
    ""hairColor"": ""Color description if hair is visible"",
    ""headCovered"": ""Yes"" or ""No"",
    ""coverType"": ""Hijab"" or ""Scarf"" or ""Cap"" or ""Other"" or ""None"",
    ""hijabOrScarfStyle"": ""Wrapped"" or ""Turban"" or ""Shawl"" or ""Other"" or ""None"",
    ""hijabOrScarfColors"": ""Colors if head is covered"",
    ""hairlineVisible"": ""Yes"" or ""No"",
    ""anyHairShowing"": ""Yes"" or ""No"",
    ""modestyLevel"": ""High"" or ""Medium"" or ""Low""
  },
  ""clothing"": {
    ""topType"": ""Dress"" or ""Shirt"" or ""Hoodie"" or ""Sweater"" or ""Blazer"" or ""Abaya"" or ""Other"",
    ""topColor"": ""Colors"",
    ""patternTexturePrint"": ""Yes/No + Description"",
    ""outerwear"": ""Yes/No + Type + Color"",
    ""bottomType"": ""Pants"" or ""Skirt"" or ""Jeans"" or ""Other"",
    ""bottomColor"": ""Color"",
    ""fullOutfitStyle"": ""Casual"" or ""Formal"" or ""Sporty"" or ""Modest"" or ""Streetwear"" or ""Vintage"" or ""Other""
  },
  ""accessories"": {
    ""headAccessories"": ""Glasses / Hat / Cap / Hijab Pin / Headband / Other / None"",
    ""jewelry"": ""Earrings / Necklace / Rings / Bracelet / Watch / None"",
    ""bagOrPurse"": ""Yes/No + Type + Color"",
    ""shoes"": ""Sneakers / Boots / Heels / Flats / Other + Color"",
    ""otherAccessories"": ""Phone / Book / Flowers / None""
  },
  ""otherVisualDetails"": {
    ""dominantColors"": ""Dominant colors in the photo"",
    ""background"": ""Indoor / Outdoor / Plain / Location"",
    ""lighting"": ""Natural / Warm / Cool / Studio""
  }
}

Rules to follow:
1. If the person is wearing a hijab/scarf -> The coverType must be 'Hijab' or 'Scarf', and the hairVisible must be 'No'.
2. If glasses are worn -> glasses must be 'Yes'.
3. If any detail is unclear -> Set as ""Unknown"" and do NOT guess.
4. Only return the JSON object.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = promptText },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2
                }
            };

            return await CallGeminiApiAsync(requestBody);
        }

        #region Private Helpers

        private async Task<string> CallGeminiApiAsync(object requestBody)
        {
            var modelsToTry = new List<string> { _options.ChatModel };
            if (_options.ChatModel != "gemini-1.5-flash")
            {
                modelsToTry.Add("gemini-1.5-flash");
            }
            if (_options.ChatModel != "gemini-2.5-flash" && !modelsToTry.Contains("gemini-2.5-flash"))
            {
                modelsToTry.Add("gemini-2.5-flash");
            }

            Exception lastException = null!;

            foreach (var model in modelsToTry)
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_options.ApiKey}";
                const int maxRetries = 3;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
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

                        var statusCode = (int)response.StatusCode;
                        var errorText = await response.Content.ReadAsStringAsync();
                        
                        _logger.LogWarning("Gemini API call to model {Model} failed on attempt {Attempt}/{MaxRetries}. Status: {Status}. Details: {Details}",
                            model, attempt, maxRetries, response.StatusCode, errorText);

                        lastException = new Exception($"Gemini API error. Status: {response.StatusCode}. Details: {errorText}");

                        // Retry only on transient errors (503 ServiceUnavailable, 429 TooManyRequests, or >=500 server errors)
                        if (attempt < maxRetries && (statusCode == 503 || statusCode == 429 || statusCode >= 500))
                        {
                            var delayMs = 1000 * (int)Math.Pow(2, attempt - 1);
                            await Task.Delay(delayMs);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Transient exception in Gemini API call to model {Model} on attempt {Attempt}/{MaxRetries}.", model, attempt, maxRetries);
                        lastException = ex;
                        if (attempt < maxRetries)
                        {
                            var delayMs = 1000 * (int)Math.Pow(2, attempt - 1);
                            await Task.Delay(delayMs);
                            continue;
                        }
                    }
                    break; 
                }
                
                _logger.LogWarning("Model {Model} failed all attempts or returned a non-transient error. Trying next fallback model if available.", model);
            }

            throw lastException ?? new Exception("Gemini API call failed with unknown error.");
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
