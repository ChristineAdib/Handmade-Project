using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Services
{
    public class RagService : IRagService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStoreService _vectorStoreService;
        private readonly IChunkService _chunkService;

        public RagService(
            IEmbeddingService embeddingService,
            IVectorStoreService vectorStoreService,
            IChunkService chunkService)
        {
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorStoreService = vectorStoreService ?? throw new ArgumentNullException(nameof(vectorStoreService));
            _chunkService = chunkService ?? throw new ArgumentNullException(nameof(chunkService));
        }

        public async Task IndexAsync(RagDocumentDto document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(document.Collection)) throw new ArgumentException("Collection name is required.", nameof(document.Collection));
            if (string.IsNullOrWhiteSpace(document.Id)) throw new ArgumentException("Document ID is required.", nameof(document.Id));
            if (string.IsNullOrWhiteSpace(document.Text)) return;

            // 1. Split text into chunks
            var chunks = _chunkService.Split(document.Text);
            if (chunks.Count == 0) return;

            // 2. Ensure collection exists (automatically detects embedding size)
            var firstEmbedding = await _embeddingService.GetEmbeddingAsync(chunks[0]);
            ulong vectorSize = (ulong)firstEmbedding.Length;
            await _vectorStoreService.EnsureCollectionExistsAsync(document.Collection, vectorSize);

            // 3. Delete existing chunks for this document to prevent duplicates/orphans
            var deleteFilter = new Dictionary<string, object>
            {
                { "document_id", document.Id }
            };
            try
            {
                await _vectorStoreService.DeleteByFilterAsync(document.Collection, deleteFilter);
            }
            catch (Exception)
            {
                // Safe to ignore if collection is new or empty
            }

            // 4. Index chunks with embeddings and metadata
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkText = chunks[i];
                var embedding = i == 0 ? firstEmbedding : await _embeddingService.GetEmbeddingAsync(chunkText);
                var chunkId = $"{document.Id}_chunk_{i}";

                var payload = new Dictionary<string, object>
                {
                    { "document_id", document.Id },
                    { "chunk_index", i },
                    { "total_chunks", chunks.Count }
                };

                // Merge optional user metadata
                if (document.Metadata != null)
                {
                    foreach (var kvp in document.Metadata)
                    {
                        if (!payload.ContainsKey(kvp.Key))
                        {
                            payload[kvp.Key] = kvp.Value;
                        }
                    }
                }

                await _vectorStoreService.UpsertAsync(
                    collectionName: document.Collection,
                    id: chunkId,
                    embedding: embedding,
                    text: chunkText,
                    metadata: payload);
            }
        }

        public async Task<IReadOnlyList<RagSearchResultDto>> SearchAsync(RagSearchRequestDto request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Collection)) throw new ArgumentException("Collection name is required.", nameof(request.Collection));
            if (string.IsNullOrWhiteSpace(request.Query)) return Array.Empty<RagSearchResultDto>();

            // 1. Generate query vector
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(request.Query);

            // 2. Query Qdrant via similarity search
            return await _vectorStoreService.SearchAsync(
                collectionName: request.Collection,
                embedding: queryEmbedding,
                topK: request.TopK,
                filter: request.Filter);
        }

        public async Task DeleteAsync(string collection, string id)
        {
            if (string.IsNullOrWhiteSpace(collection)) throw new ArgumentException("Collection name is required.", nameof(collection));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document ID is required.", nameof(id));

            var deleteFilter = new Dictionary<string, object>
            {
                { "document_id", id }
            };

            await _vectorStoreService.DeleteByFilterAsync(collection, deleteFilter);
        }
    }
}
