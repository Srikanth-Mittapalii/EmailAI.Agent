using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public interface ILLMService
    {
        string ProviderName { get; }
        Task<string> Ask(string prompt);
        Task<string> AskWithSystem(string? systemPrompt, string userPrompt);
    }
}
