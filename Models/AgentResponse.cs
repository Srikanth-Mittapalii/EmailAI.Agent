using System.Collections.Generic;

namespace EmailAI.Agent.Models
{
    public class ExecutionMetrics
    {
        public int TotalTimeMs { get; set; }
        public int TokensUsed { get; set; }
        public int CacheHits { get; set; }
    }

    public class AgentResponse
    {
        public bool Success { get; set; }
        public string Response { get; set; }
        public List<Email> Emails { get; set; }
        public ExecutionMetrics Metrics { get; set; }
    }
}
