using HandoraApplication.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HandoraInfrastructure.AI.Documents
{
    public class ChunkService : IChunkService
    {
        private readonly int _maxChunkSize;
        private readonly int _chunkOverlap;

        public ChunkService(int maxChunkSize = 1000, int chunkOverlap = 100)
        {
            if (maxChunkSize <= 0)
                throw new ArgumentException("Max chunk size must be greater than zero.", nameof(maxChunkSize));
            if (chunkOverlap < 0 || chunkOverlap >= maxChunkSize)
                throw new ArgumentException("Chunk overlap must be non-negative and less than max chunk size.", nameof(chunkOverlap));

            _maxChunkSize = maxChunkSize;
            _chunkOverlap = chunkOverlap;
        }

        public IReadOnlyList<string> Split(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var chunks = new List<string>();
            int index = 0;
            int textLength = text.Length;

            while (index < textLength)
            {
                int length = Math.Min(_maxChunkSize, textLength - index);
                var chunk = text.Substring(index, length).Trim();
                
                if (!string.IsNullOrEmpty(chunk))
                {
                    chunks.Add(chunk);
                }

                if (index + length >= textLength)
                {
                    break;
                }

                index += _maxChunkSize - _chunkOverlap;
            }

            return chunks;
        }
    }
}
