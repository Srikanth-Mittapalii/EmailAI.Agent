using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class OpenRouterService : ILLMService
    {
        public string ProviderName => "OpenRouter";
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _httpClient = new HttpClient();

        public OpenRouterService(IConfiguration config)
        {
            _apiKey = config["OpenRouter:ApiKey"] ?? "";
            _model = config["OpenRouter:Model"] ?? "mistralai/mistral-7b-instruct:free";
        }

        public async Task<string> Ask(string prompt)
        {
            return await AskWithSystem(null, prompt);
        }

        public async Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            try
            {
                var url = "https://openrouter.ai/api/v1/chat/completions";

                var messages = new System.Collections.Generic.List<object>();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                messages.Add(new { role = "user", content = userPrompt });

                var bodyObj = new
                {
                    model = _model,
                    messages = messages,
                    temperature = 0.2
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("HTTP-Referer", "http://localhost:5180"); // Required by OpenRouter
                request.Headers.Add("X-Title", "Email AI Agent");
                
                var payload = JsonConvert.SerializeObject(bodyObj);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenRouter API error: ({response.StatusCode}) {responseContent}");

                dynamic? responseData = JsonConvert.DeserializeObject(responseContent);
                string? textContent = responseData?.choices?[0]?.message?.content;
                
                if (textContent != null) return textContent.Trim();

                throw new Exception("Unexpected response format from OpenRouter");
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenRouter API call failed: {ex.Message}");
            }
        }
    }
}
