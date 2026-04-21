using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class GroqService : ILLMService
    {
        public string ProviderName => "Groq";
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GroqService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GroqService(IConfiguration config, ILogger<GroqService> logger)
        {
            _apiKey = (config["Groq:ApiKey"] ?? "").Trim();
            _model = config["Groq:Model"] ?? "llama-3.1-70b-versatile";
            _logger = logger;
            _logger.LogInformation($"GroqService initialized. Auth Key Loaded (Length: {_apiKey.Length})");
        }

        public async Task<string> Ask(string prompt)
        {
            return await AskWithSystem(null, prompt);
        }

        public async Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            try
            {
                var url = "https://api.groq.com/openai/v1/chat/completions";

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

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
                
                var payload = JsonConvert.SerializeObject(bodyObj);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var partialResponse = responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent;
                    throw new Exception($"Groq API error: ({response.StatusCode}) {partialResponse}");
                }

                dynamic? responseData = JsonConvert.DeserializeObject(responseContent);
                string? textContent = responseData?.choices?[0]?.message?.content;
                
                if (textContent != null) return textContent.Trim();

                throw new Exception("Unexpected response format from Groq");
            }
            catch (Exception ex)
            {
                throw new Exception($"Groq API call failed: {ex.Message}");
            }
        }
    }
}
