namespace EmailAI.Agent.Models
{
    public class QueryRequest
    {
        public string Query { get; set; } = null!;
        public int Limit { get; set; } = 5;
        public string? Timeframe { get; set; }
    }
}
