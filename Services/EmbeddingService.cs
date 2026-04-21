using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartComponents.LocalEmbeddings;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class EmbeddingService
    {
        private readonly string _apiKey;
        private readonly string _embeddingUrl;
        private readonly LocalEmbedder _localEmbedder;
        private static readonly HttpClient _httpClient = new HttpClient();

        public EmbeddingService(IConfiguration config, LocalEmbedder localEmbedder)
        {
            _apiKey = config["HuggingFace:ApiKey"] ?? "";
            _embeddingUrl = config["HuggingFace:EmbeddingUrl"] ?? 
                "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2";
            _localEmbedder = localEmbedder;
            
            // Set default headers for HuggingFace
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization") && !string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<double[]> GenerateEmbedding(string text)
        {
            try
            {
                // Attempt 1: Try HuggingFace remote API
                var payload = new { inputs = text };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // CRITICAL: We set a short timeout for the remote call so the user doesn't wait forever if HF is slow
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.PostAsync(_embeddingUrl, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    // HF returns double[] or double[][] depending on input
                    try 
                    {
                        var vector = JsonConvert.DeserializeObject<double[]>(resultJson);
                        if (vector != null) return vector;
                    }
                    catch 
                    {
                        var nestedVector = JsonConvert.DeserializeObject<double[][]>(resultJson);
                        if (nestedVector != null && nestedVector.Length > 0) return nestedVector[0];
                    }
                }
                
                // If we reach here, HF failed (status code or parsing)
                return GenerateLocalFallback(text);
            }
            catch (Exception)
            {
                // Fallback to local on any network exception or timeout
                return GenerateLocalFallback(text);
            }
        }

        private double[] GenerateLocalFallback(string text)
        {
            // Microsoft LocalEmbeddings returns a float array (384 dimensions for the default model)
            var embedding = _localEmbedder.Embed(text);
            
            // Convert float[] to double[] to match our MongoDB schema
            double[] result = new double[embedding.Values.Length];
            for (int i = 0; i < embedding.Values.Length; i++)
            {
                result[i] = (double)embedding.Values.Span[i];
            }
            
            return result;
        }
    }
}
