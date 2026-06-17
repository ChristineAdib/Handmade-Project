using HandoraApplication.IServices.AI;
using HandoraApplication.Settings;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services.AIServices
{
    public class RagService : IRagService
    {
        private readonly IVectorDbService _vectorDb;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RagSettings _settings;
        private readonly Kernel _kernel;
        private readonly ITextEmbeddingGenerationService _embedding;

        public RagService(
            IVectorDbService vectorDb,
            IUnitOfWork unitOfWork,
            IOptions<RagSettings> settings)
        {
            _vectorDb = vectorDb;
            _unitOfWork = unitOfWork;
            _settings = settings.Value;

            _kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(_settings.ChatModel, _settings.OpenAiApiKey)
                .AddOpenAITextEmbeddingGeneration(_settings.EmbeddingModel, _settings.OpenAiApiKey)
                .Build();

            _embedding = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        }

        public async Task IndexProductAsync(string productId, string name,
            string description, string category, decimal price, string sellerName)
        {
            await _vectorDb.EnsureCollectionExistsAsync(_settings.CollectionName, 1536);

            var text = $"{name}. {description}. الفئة: {category}. " +
                       $"السعر: {price} جنيه. البائع: {sellerName}.";

            var embeddings = await _embedding.GenerateEmbeddingsAsync(new[] { text });
            var vector = embeddings[0].ToArray();

            var payload = new Dictionary<string, string>
            {
                ["product_id"] = productId,
                ["name"] = name,
                ["category"] = category,
                ["price"] = price.ToString(),
                ["seller_name"] = sellerName
            };

            await _vectorDb.UpsertVectorAsync(
                _settings.CollectionName,
                Guid.Parse(productId),
                vector,
                payload);
        }

        public async Task IndexAllProductsAsync()
        {
            // اعدّل السطر ده حسب الـ repository بتاعك
            var products = await _unitOfWork.Repository<Product>().GetAllAsync();

            foreach (var product in products)
            {
                await IndexProductAsync(
                    product.Id.ToString(),
                    product.Name,
                    product.Description ?? "",
                    product.Category?.Name ?? "",
                    product.Price,
                    product.Seller?.UserName ?? "");
            }
        }

        public async Task<string> SearchProductsAsync(string userQuestion)
        {
            // 1. حوّل السؤال لـ vector
            var embeddings = await _embedding.GenerateEmbeddingsAsync(new[] { userQuestion });
            var queryVector = embeddings[0].ToArray();

            // 2. ابحث عن أقرب 5 منتجات
            var results = await _vectorDb.SearchAsync(_settings.CollectionName, queryVector, topK: 5);

            // 3. لو مفيش نتايج
            if (!results.Any())
                return "للأسف مش لاقيين منتجات مناسبة دلوقتي. جرب تبحث بكلمات تانية!";

            // 4. ابني الـ context
            var context = string.Join("\n", results.Select((r, i) =>
                $"{i + 1}. {r.Payload["name"]} — {r.Payload["price"]} جنيه" +
                $" — البائع: {r.Payload["seller_name"]}" +
                $" — الفئة: {r.Payload["category"]}"));

            // 5. ابعت للـ LLM
            var prompt = $"""
                أنت مساعد Handora، منصة للمنتجات الحرفية اليدوية المصرية.
                
                ── قواعد مهمة ──
                • استخدم فقط المنتجات الموجودة في السياق أدناه
                • لو المستخدم بيدور على هدية أو ديكور، اقترح المناسب وقول ليه
                • لو بيدور على seller يعمله custom، ركّز على اسم البائع وفئته
                • اذكر السعر دايماً
                • رد بالعربي بشكل ودي ومختصر
                
                ── المنتجات المتاحة ──
                {context}
                
                ── سؤال العميل ──
                {userQuestion}
                """;

            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString();
        }
    }
}
