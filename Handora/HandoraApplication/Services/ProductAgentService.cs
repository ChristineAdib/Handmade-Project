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
            var prompt = @"You are an expert at analyzing handmade Egyptian craft products for Handaura platform.
Analyze this product image and return ONLY a JSON object, no markdown, no extra text:
{
  ""titleEn"": ""title in English max 60 chars"",
  ""titleAr"": ""العنوان بالعربي"",
  ""descriptionEn"": ""description in English 100-150 words"",
  ""descriptionAr"": ""الوصف بالعربي 100-150 كلمة"",
  ""suggestedPrice"": 0,
  ""category"": ""one of: Pottery Textiles Jewelry Leather Wood Glass Metal Other"",
  ""tags"": [""tag1"", ""tag2"", ""tag3""]
}";

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