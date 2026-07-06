using HandoraApplication.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Embeddings
{
    public class OnnxEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly Tokenizers.DotNet.Tokenizer? _tokenizer;
        private readonly bool _isAvailable;
        private readonly ILogger<OnnxEmbeddingService>? _logger;

        public OnnxEmbeddingService(ILogger<OnnxEmbeddingService>? logger = null)
        {
            _logger = logger;
            try
            {
                var modelPath = Path.Combine(AppContext.BaseDirectory, "AI", "Embeddings", "Models", "model.onnx");
                var tokenizerPath = Path.Combine(AppContext.BaseDirectory, "AI", "Embeddings", "Models", "tokenizer.json");

                _session = new InferenceSession(modelPath);
                _tokenizer = new Tokenizers.DotNet.Tokenizer(tokenizerPath);
                _isAvailable = true;
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                _logger?.LogWarning(ex, "ONNX Runtime is not available on this host. AI embedding features will be disabled. Products and other endpoints will continue to function normally.");
            }
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (!_isAvailable || _session == null || _tokenizer == null)
            {
                // Return empty vector — embedding features unavailable on this host
                return Array.Empty<float>();
            }

            // 1. Tokenize — the tokenizer returns a fixed-length array (128) padded with 0s
            var encoding = _tokenizer.Encode(text);

            var inputIds = encoding
                .Select(x => (long)x)
                .ToArray();

            // 2. Detect actual token length (non-padding).
            //    The tokenizer pads with 0 (PAD token). Find the last non-zero token.
            int actualLength = 0;
            for (int i = 0; i < inputIds.Length; i++)
            {
                if (inputIds[i] != 0)
                {
                    actualLength = i + 1;
                }
            }
            if (actualLength == 0) actualLength = 1; // safety fallback

            // 3. Build attention mask: 1 for real tokens, 0 for padding
            var attentionMask = new long[inputIds.Length];
            for (int i = 0; i < inputIds.Length; i++)
            {
                attentionMask[i] = i < actualLength ? 1L : 0L;
            }

            var tokenTypeIds = Enumerable
                .Repeat(0L, inputIds.Length)
                .ToArray();

            // 4. Create tensors
            var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
            var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length });
            var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, tokenTypeIds.Length });

            // 5. Feed model
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
            };

            using var results = _session.Run(inputs);

            // 6. Extract embeddings
            var output = results.First().AsTensor<float>();

            // 7. Mean pooling over ONLY the actual (non-padding) tokens, then L2-normalize
            var vector = MeanPooling(output, actualLength);

            return vector;
        }

        private float[] MeanPooling(Tensor<float> tokenEmbeddings, int actualLength)
        {
            var dim = tokenEmbeddings.Dimensions[2];
            var result = new float[dim];

            // Sum only the actual (non-padding) token embeddings
            for (int i = 0; i < actualLength; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    result[j] += tokenEmbeddings[0, i, j];
                }
            }

            // Divide by actual token count (not the full padded length)
            for (int j = 0; j < dim; j++)
            {
                result[j] /= actualLength;
            }

            // L2 normalize the vector for optimal cosine similarity
            double norm = 0.0;
            for (int j = 0; j < dim; j++)
            {
                norm += result[j] * result[j];
            }
            norm = Math.Sqrt(norm);
            if (norm > 0)
            {
                for (int j = 0; j < dim; j++)
                {
                    result[j] = (float)(result[j] / norm);
                }
            }

            return result;
        }

        public void Dispose()
        {
            _session?.Dispose();
            (_tokenizer as IDisposable)?.Dispose();
        }
    }
}
