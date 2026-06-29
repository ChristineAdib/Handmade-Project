using HandoraApplication.DTOs.ProductAgentDTOs;
using HandoraApplication.IServices;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace HandoraApplication.Services
{
    public class ProductAgentService : IProductAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ProductAgentService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _config = config;
        }

        public async Task<ProductAnalysisResult> AnalyzeProductImageAsync(string imageBase64, string mimeType)
        {
            var prompt = @"
You are an expert product copywriter, visual analyst, and pricing specialist for Handaura, a premium marketplace for handmade Egyptian crafts.

Carefully analyze the uploaded product image.

Rules:
- Describe ONLY what you can confidently see.
- Never invent colors, materials, decorations, or features that are not visible.
- If something is uncertain, use neutral wording.
- Make the writing sound luxurious, authentic, and emotionally engaging.
- Assume the product is handmade in Egypt unless the image clearly suggests otherwise.
- Avoid repetitive sentences and generic phrases.
- Write naturally like a premium e-commerce brand.

Return ONLY a valid JSON object with no markdown or extra text.

{
  ""titleEn"": ""Creative premium product title in English (40-60 characters)."",
  ""titleAr"": ""عنوان عربي جذاب ومميز (40-60 حرفاً)."",
  ""descriptionEn"": ""Write a premium product description (180-250 words). Start with a strong hook that captures attention. Describe the visible design, craftsmanship, shape, texture, colors, and aesthetic appeal. Explain how the item could be used or displayed. Emphasize that it is handmade in Egypt, highlighting uniqueness, attention to detail, and artisan craftsmanship. Create an emotional connection with the buyer. End with a memorable sentence explaining why this piece deserves a place in their collection."",
  ""descriptionAr"": ""اكتب وصفاً احترافياً (180-250 كلمة). ابدأ بجملة افتتاحية جذابة تلفت الانتباه. صف التصميم الظاهر، الألوان، الملمس، الشكل، التفاصيل والحرفية بدقة دون اختلاق أي معلومات. وضح كيف يمكن استخدام القطعة أو عرضها. أكد أنها صُنعت يدوياً في مصر مع إبراز قيمة العمل اليدوي وتفرد كل قطعة. استخدم لغة تسويقية راقية تخلق ارتباطاً عاطفياً مع العميل، واختم بجملة مؤثرة توضح لماذا تستحق هذه القطعة الاقتناء."",
  ""suggestedPrice"": ""Estimate a realistic selling price in Egyptian Pounds (integer only). Consider craftsmanship, visible quality, size, complexity, and the Egyptian handmade market. Price range: 150-1000 EGP."",
  ""category"": ""Choose exactly one of: Pottery, Textiles, Jewelry, Leather, Wood, Glass, Metal, Other."",
  ""tags"": [""tag1"", ""tag2"", ""tag3"", ""tag4"", ""tag5""]
}
";
            var requestBody = new
            {
                model = "meta-llama/llama-4-scout-17b-16e-instruct",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:{mimeType};base64,{imageBase64}"
                                }
                            },
                            new
                            {
                                type = "text",
                                text = prompt
                            }
                        }
                    }
                },
                temperature = 0.4,
                max_tokens = 1024
            };

            var url = "https://api.groq.com/openai/v1/chat/completions";
            var apiKey = _config["Groq:ApiKey"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Groq API error: {responseString}");

            var responseBody = JsonSerializer.Deserialize<JsonElement>(responseString);
            var rawText = responseBody
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(rawText))
                throw new Exception("Groq returned empty response");

            var cleanJson = rawText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            if (!cleanJson.StartsWith("{"))
                throw new Exception($"Unexpected Groq response: {cleanJson}");

            return JsonSerializer.Deserialize<ProductAnalysisResult>(cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }
}