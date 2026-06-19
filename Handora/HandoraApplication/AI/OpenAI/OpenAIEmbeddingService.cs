using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using Microsoft.Extensions.Options;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.AI.OpenAI
{
    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly OpenAIClient _client;
        private readonly OpenAIOptions _options;

        public OpenAIEmbeddingService(IOptions<OpenAIOptions> options)
        {
            _options = options.Value;
            _client = new OpenAIClient(_options.ApiKey);
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var embeddingClient = _client.GetEmbeddingClient(_options.EmbeddingModel);

            var response = await embeddingClient.GenerateEmbeddingAsync(text);

            // SDK بيرجع vector جاهز
            return response.Value.ToFloats().ToArray();
        }
    }
}
