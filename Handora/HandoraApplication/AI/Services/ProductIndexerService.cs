using HandoraApplication.AI.Interfaces;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using HandoraInfrastructure.AI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Services
{
    public class ProductIndexerService : IProductIndexerService
    {
        private readonly IProductRepository _productRepository;
        private readonly IVectorStoreService _vectorStoreService;
        private readonly IEmbeddingService _embeddingService;
        private readonly QdrantOptions _qdrantOptions;

        public ProductIndexerService(
            IProductRepository productRepository,
            IVectorStoreService vectorStoreService,
            IEmbeddingService embeddingService,
            IOptions<QdrantOptions> qdrantOptions)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _vectorStoreService = vectorStoreService ?? throw new ArgumentNullException(nameof(vectorStoreService));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _qdrantOptions = qdrantOptions?.Value ?? throw new ArgumentNullException(nameof(qdrantOptions));
        }

        public async Task IndexAllProductsAsync()
        {
            var collectionName = GetProductsCollectionName();

            // 1. Get all active products from DB (eager loading Category, Images, Tags)
            var productsQuery = await _productRepository.GetAllProductsQueryAsync();
            var products = await productsQuery
                 .Include(p => p.Category)
                     .ThenInclude(c => c.Parent)
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .Where(p => p.IsActive && p.Status == ProductStatus.Active)
                .ToListAsync();

            // 1b. Also retrieve inactive/deleted product IDs to clean them up from vector store
            var inactiveOrDeletedProducts = await productsQuery
                .IgnoreQueryFilters()
                .Where(p => !p.IsActive || p.Status != ProductStatus.Active || p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var productId in inactiveOrDeletedProducts)
            {
                try
                {
                    await _vectorStoreService.DeleteAsync(collectionName, productId.ToString());
                }
                catch
                {
                    // Ignore failures for individual deletes to ensure overall indexing continues
                }
            }

            if (products.Count == 0)
            {
                return;
            }

            // 2. Automatically determine vector size from first embedding
            var sampleEmbedding = await _embeddingService.GetEmbeddingAsync("sample text");
            ulong vectorSize = (ulong)sampleEmbedding.Length;
            await _vectorStoreService.EnsureCollectionExistsAsync(collectionName, vectorSize);

            // 3. Index each product
            foreach (var product in products)
            {
                // Build a structured, descriptive text document for LLM and vector search matching
                var textBuilder = new StringBuilder();
                textBuilder.AppendLine($"Product: {product.TitleEn}");
                textBuilder.AppendLine($"Price: {product.Price}");
                if (!string.IsNullOrWhiteSpace(product.DescriptionEn))
                {
                    textBuilder.AppendLine($"Description: {product.DescriptionEn}");
                }
                if (product.Tags != null && product.Tags.Count > 0)
                {
                    var tagNames = string.Join(", ", product.Tags.Select(t => t.Name));
                    textBuilder.AppendLine($"Tags: {tagNames}");
                }

                var documentText = textBuilder.ToString();
                var embedding = await _embeddingService.GetEmbeddingAsync(documentText);

                // Main product image url resolution
                var mainImage = product.Images?.FirstOrDefault(img => img.IsMain) ?? product.Images?.FirstOrDefault();
                var imageUrl = mainImage?.ImageUrl;

                // Tags mapping
                var tagsList = product.Tags?.Select(t => t.Name).ToList() ?? new List<string>();

                var payload = new Dictionary<string, object>
                {
                    { "product_id", product.Id.ToString() },
                    { "title", product.TitleEn },
                    { "price", (double)(product.DiscountPrice ?? product.Price) }, // numeric for range filtering
                    { "description", product.DescriptionEn ?? string.Empty },
                    { "imageUrl", imageUrl ?? string.Empty },
                    { "tags", tagsList }
                };

                await _vectorStoreService.UpsertAsync(
                    collectionName: collectionName,
                    id: product.Id.ToString(),
                    embedding: embedding,
                    text: documentText,
                    metadata: payload
                );
            }
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
