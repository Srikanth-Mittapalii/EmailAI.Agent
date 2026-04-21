using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class GeminiService : ILLMService
    {
        public string ProviderName => "Gemini";
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GeminiService(IConfiguration config)
        {
            _apiKey = config["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey missing");
            _model = config["Gemini:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<string> Ask(string prompt)
        {
            return await AskWithSystem(null, prompt);
        }

        public async Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                object bodyObj;
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    bodyObj = new
                    {
                        systemInstruction = new
                        {
                            parts = new[] { new { text = systemPrompt } }
                        },
                        contents = new[]
                        {
                            new { 
                                role = "user", 
                                parts = new[] { new { text = userPrompt } }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.2,
                            maxOutputTokens = 1024
                        }
                    };
                }
                else
                {
                    bodyObj = new
                    {
                        contents = new[]
                        {
                            new { 
                                role = "user", 
                                parts = new[] { new { text = userPrompt } }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.2,
                            maxOutputTokens = 1024
                        }
                    };
                }

                var payload = JsonConvert.SerializeObject(bodyObj);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Gemini API error: ({response.StatusCode}) {responseContent}");

                dynamic? responseData = JsonConvert.DeserializeObject(responseContent);
                string? textContent = responseData?.candidates?[0]?.content?.parts?[0]?.text;
                
                if (textContent != null)
                {
                    // Clean markdown code blocks if present
                    if (textContent.StartsWith("```json"))
                    {
                        textContent = textContent.Substring(7);
                        if (textContent.EndsWith("```"))
                        {
                            textContent = textContent.Substring(0, textContent.Length - 3);
                        }
                    }
                    else if (textContent.StartsWith("```"))
                    {
                        textContent = textContent.Substring(3);
                        if (textContent.EndsWith("```"))
                        {
                            textContent = textContent.Substring(0, textContent.Length - 3);
                        }
                    }
                    
                    return textContent.Trim();
                }

                throw new Exception("Unexpected response format from Gemini");
            }
            catch (Exception ex)
            {
                throw new Exception($"Gemini API call failed: {ex.Message}");
            }
        }
    }
}
