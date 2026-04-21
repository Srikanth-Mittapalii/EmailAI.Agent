using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class HuggingFaceLLMService : ILLMService
    {
        public string ProviderName => "HuggingFace";
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        public HuggingFaceLLMService(IConfiguration config)
        {
            _apiKey = (config["HuggingFace:ApiKey"] ?? "").Trim();
        }

        public async Task<string> Ask(string prompt)
        {
            return await AskWithSystem(null, prompt);
        }

        public async Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            try
            {
                // Use HuggingFace's OpenAI-compatible Router endpoint
                var url = "https://router.huggingface.co/hf-inference/v1/chat/completions";

                var messages = new System.Collections.Generic.List<object>();
                if (!string.IsNullOrEmpty(systemPrompt))
                    messages.Add(new { role = "system", content = systemPrompt });
                messages.Add(new { role = "user", content = userPrompt });

                var bodyObj = new
                {
                    model = "mistralai/Mistral-7B-Instruct-v0.3",
                    messages = messages,
                    max_tokens = 512,
                    temperature = 0.2
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
                var payload = JsonConvert.SerializeObject(bodyObj);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var partial = responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent;
                    throw new Exception($"HuggingFace API error: ({response.StatusCode}) {partial}");
                }

                dynamic? responseData = JsonConvert.DeserializeObject(responseContent);
                string? textContent = responseData?.choices?[0]?.message?.content;

                if (textContent != null) return textContent.Trim();

                throw new Exception("Unexpected response format from HuggingFace");
            }
            catch (Exception ex)
            {
                throw new Exception($"HuggingFace API call failed: {ex.Message}");
            }
        }
    }
}
