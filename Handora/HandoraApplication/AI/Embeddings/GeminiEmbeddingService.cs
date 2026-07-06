using HandoraApplication.AI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Embeddings
{
    /// <summary>
    /// Embedding service that uses Gemini's text-embedding-004 API.
    /// Unlike OnnxEmbeddingService, this requires no local model files
    /// and works on any hosting environment with internet access.
    /// </summary>
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiEmbeddingService> _logger;
        private const string EmbeddingModel = "gemini-embedding-001";
        private const int OutputDimensionality = 384; // Match existing Qdrant collection vector size

        public GeminiEmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiEmbeddingService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("[GeminiEmbedding] Empty text provided, returning zero vector.");
                return new float[OutputDimensionality];
            }

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{EmbeddingModel}:embedContent?key={_apiKey}";

                var requestBody = new
                {
                    model = $"models/{EmbeddingModel}",
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = text }
                        }
                    },
                    outputDimensionality = OutputDimensionality
                };

                var json = JsonSerializer.Serialize(requestBody);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[GeminiEmbedding] API returned {StatusCode}: {Body}", response.StatusCode, responseJson);
                    return new float[OutputDimensionality];
                }

                using var doc = JsonDocument.Parse(responseJson);
                var valuesElement = doc.RootElement
                    .GetProperty("embedding")
                    .GetProperty("values");

                var values = new float[valuesElement.GetArrayLength()];
                int i = 0;
                foreach (var val in valuesElement.EnumerateArray())
                {
                    values[i++] = val.GetSingle();
                }

                // L2 normalize
                double norm = 0.0;
                for (int j = 0; j < values.Length; j++)
                {
                    norm += values[j] * values[j];
                }
                norm = Math.Sqrt(norm);
                if (norm > 0)
                {
                    for (int j = 0; j < values.Length; j++)
                    {
                        values[j] = (float)(values[j] / norm);
                    }
                }

                return values;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GeminiEmbedding] Failed to generate embedding for text (length={Length})", text.Length);
                return new float[OutputDimensionality];
            }
        }
    }
}
