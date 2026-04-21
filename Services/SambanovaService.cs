using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class SambanovaService : ILLMService
    {
        public string ProviderName => "Sambanova";
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _httpClient = new HttpClient();

        public SambanovaService(IConfiguration config, ILogger<SambanovaService> logger)
        {
            _apiKey = (config["Sambanova:ApiKey"] ?? "").Trim();
            _model = config["Sambanova:Model"] ?? "Meta-Llama-3.1-70B-Instruct";
            logger.LogInformation($"SambanovaService initialized. Auth Key Loaded (Length: {_apiKey.Length})");
        }

        public async Task<string> Ask(string prompt)
        {
            return await AskWithSystem(null, prompt);
        }

        public async Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            try
            {
                var url = "https://api.sambanova.ai/v1/chat/completions";

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
                    throw new Exception($"Sambanova API error: ({response.StatusCode}) {partialResponse}");
                }

                dynamic? responseData = JsonConvert.DeserializeObject(responseContent);
                string? textContent = responseData?.choices?[0]?.message?.content;
                
                if (textContent != null) return textContent.Trim();

                throw new Exception("Unexpected response format from Sambanova");
            }
            catch (Exception ex)
            {
                throw new Exception($"Sambanova API call failed: {ex.Message}");
            }
        }
    }
}
