using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class IntelligenceOrchestrator
    {
        private readonly IEnumerable<ILLMService> _llmServices;
        private readonly ILogger<IntelligenceOrchestrator> _logger;

        public IntelligenceOrchestrator(
            IEnumerable<ILLMService> llmServices, 
            ILogger<IntelligenceOrchestrator> logger)
        {
            _llmServices = llmServices;
            _logger = logger;
        }

        public async Task<string> Ask(string prompt)
        {
            return await ExecuteWithFallback(s => s.Ask(prompt));
        }

        public async Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            return await ExecuteWithFallback(s => s.AskWithSystem(systemPrompt, userPrompt));
        }

        private async Task<string> ExecuteWithFallback(Func<ILLMService, Task<string>> action)
        {
            var errors = new List<string>();

            // High-Speed Priority: Groq -> Sambanova -> DeepSeek -> Gemini -> Others
            var priorityOrder = new List<string> { "Groq", "Sambanova", "DeepSeek", "Gemini", "HuggingFace", "OpenRouter" };
            var orderedServices = _llmServices
                .OrderBy(s => {
                    int index = priorityOrder.IndexOf(s.ProviderName);
                    return index == -1 ? int.MaxValue : index;
                })
                .ToList();

            foreach (var service in orderedServices)
            {
                try
                {
                    _logger.LogInformation($"Attempting intelligence request via {service.ProviderName}...");
                    var result = await action(service);
                    _logger.LogInformation($"Request successful via {service.ProviderName}.");
                    return result;
                }
                catch (Exception ex)
                {
                    // Debug Mode: Log partial response if available in the exception message
                    var errorMsg = $"\n[!] INTELLIGENCE ALERT: {service.ProviderName} failed.\nReason: {ex.Message}\n";
                    _logger.LogError(errorMsg);
                    errors.Add($"{service.ProviderName}: {ex.Message}");
                    continue; // Try next provider
                }
            }

            throw new Exception($"All intelligence providers failed. Diagnostics: {string.Join(" | ", errors)}");
        }
    }
}
