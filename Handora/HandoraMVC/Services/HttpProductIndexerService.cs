using HandoraApplication.AI.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HandoraMVC.Services
{
    public class HttpProductIndexerService : IProductIndexerService
    {
        private readonly HttpClient _httpClient;

        public HttpProductIndexerService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task IndexAllProductsAsync()
        {
            try
            {
                // Trigger the API endpoint to index products on the API side
                await _httpClient.PostAsync("api/ai/index-products", null);
            }
            catch (Exception)
            {
                // Silence exception to avoid blocking or failing MVC flows if API indexing is temporarily unavailable
            }
        }
    }
}
