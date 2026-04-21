using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class StaticRuleService : ILLMService
    {
        public string ProviderName => "StaticRules";

        public Task<string> Ask(string prompt)
        {
            // Simple keyword analysis fallback
            string lowerPrompt = prompt.ToLower();

            if (lowerPrompt.Contains("classify"))
            {
                if (lowerPrompt.Contains("bill") || lowerPrompt.Contains("invoice") || lowerPrompt.Contains("payment") || lowerPrompt.Contains("finance"))
                    return Task.FromResult("Finance");
                if (lowerPrompt.Contains("urgent") || lowerPrompt.Contains("emergency") || lowerPrompt.Contains("asap"))
                    return Task.FromResult("Urgent");
                if (lowerPrompt.Contains("interview") || lowerPrompt.Contains("hr") || lowerPrompt.Contains("hiring") || lowerPrompt.Contains("onboarding"))
                    return Task.FromResult("HR");
                
                return Task.FromResult("Work");
            }

            if (lowerPrompt.Contains("suggest one action"))
            {
                if (lowerPrompt.Contains("urgent") || lowerPrompt.Contains("server"))
                    return Task.FromResult("Escalate");
                if (lowerPrompt.Contains("billing") || lowerPrompt.Contains("invoice"))
                    return Task.FromResult("Reply");
                
                return Task.FromResult("Ignore");
            }

            if (lowerPrompt.Contains("summarize"))
            {
                return Task.FromResult("- Record ingested successfully\n- Categorized via Static Rules\n- Awaiting higher intelligence sync");
            }

            return Task.FromResult("Intelligence core is currently in safe mode. Automatic classification complete.");
        }

        public Task<string> AskWithSystem(string? systemPrompt, string userPrompt)
        {
            return Ask(userPrompt);
        }
    }
}
