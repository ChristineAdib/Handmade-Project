using HandoraApplication.AI.Interfaces;
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
        private readonly InferenceSession _session;
        private readonly Tokenizers.DotNet.Tokenizer _tokenizer;
          
        public OnnxEmbeddingService()
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "AI", "Embeddings", "Models", "model.onnx");
            var tokenizerPath = Path.Combine(AppContext.BaseDirectory, "AI", "Embeddings", "Models", "tokenizer.json");

            _session = new InferenceSession(modelPath);
            _tokenizer = new Tokenizers.DotNet.Tokenizer(tokenizerPath);
        }
       
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            // 1. Tokenize
            var encoding = _tokenizer.Encode(text);

            var inputIds = encoding
                .Select(x => (long)x)
                .ToArray();

            var attentionMask = Enumerable
                .Repeat(1L, inputIds.Length)
                .ToArray();

            var tokenTypeIds = Enumerable
                .Repeat(0L, inputIds.Length)
                .ToArray();

            // 2. Create tensors
            var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
            var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length });
            var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, tokenTypeIds.Length });

            // 3. Feed model
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
            };

            using var results = _session.Run(inputs);

            // 4. Extract embeddings
            var output = results.First().AsTensor<float>();

            // 5. Mean pooling (important for sentence embeddings)
            var vector = MeanPooling(output);

            return vector;
        }

        private float[] MeanPooling(Tensor<float> tokenEmbeddings)
        {
            var length = tokenEmbeddings.Dimensions[1];
            var dim = tokenEmbeddings.Dimensions[2];

            var result = new float[dim];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    result[j] += tokenEmbeddings[0, i, j];
                }
            }

            for (int j = 0; j < dim; j++)
            {
                result[j] /= length;
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
