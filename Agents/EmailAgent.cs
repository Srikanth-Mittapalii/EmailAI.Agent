using EmailAI.Agent.Models;
using EmailAI.Agent.Services;
using System.Threading.Tasks;

namespace EmailAI.Agent.Agents
{
    public class EmailAgent
    {
        private readonly EmailIngestionService _ingestionService;

        public EmailAgent(EmailIngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }

        public async Task<Email> ProcessEmail(string subject, string body, string sender, string userId)
        {
            return await _ingestionService.ProcessAndStoreEmail(subject, body, sender, userId);
        }
    }
}
