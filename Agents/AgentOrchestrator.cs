using EmailAI.Agent.Models;
using EmailAI.Agent.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailAI.Agent.Agents
{
    public class AgentOrchestrator
    {
        private readonly VectorDbService _vectorDbService;
        private readonly EmbeddingService _embeddingService;
        private readonly IntelligenceOrchestrator _intelligence;

        public AgentOrchestrator(
            VectorDbService vectorDbService,
            EmbeddingService embeddingService,
            IntelligenceOrchestrator intelligence)
        {
            _vectorDbService = vectorDbService;
            _embeddingService = embeddingService;
            _intelligence = intelligence;
        }

        public async Task<AgentResponse> ProcessQuery(string query, string userId, int limit = 5)
        {
            var executionStartTime = DateTime.UtcNow;
            DateTime? filterStartTime = null;

            try
            {
                // Step 1: Extract intent/timeframe using LLM
                var intentPrompt = $@"Analyze the following user search query and extract search parameters.
Identify if the user is asking for emails within a specific timeframe (e.g., 'last 10 days', 'past month', 'yesterday').
Current Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

Query: ""{query}""

Return ONLY JSON:
{{
  ""searchTerm"": ""string"",
  ""daysAgo"": number | null
}}";
                var intentJson = await _intelligence.Ask(intentPrompt);
                
                string searchTerm = query;
                try 
                {
                    var json = intentJson.Substring(intentJson.IndexOf('{'), intentJson.LastIndexOf('}') - intentJson.IndexOf('{') + 1);
                    var intent = System.Text.Json.JsonSerializer.Deserialize<SearchIntent>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (intent?.DaysAgo > 0)
                    {
                        filterStartTime = DateTime.UtcNow.AddDays(-intent.DaysAgo.Value);
                    }
                    if (!string.IsNullOrEmpty(intent?.SearchTerm))
                    {
                        searchTerm = intent.SearchTerm;
                    }
                }
                catch { /* Fallback to raw query */ }

                // Step 2: Generate embedding for query
                var embedding = await _embeddingService.GenerateEmbedding(searchTerm);

                // Step 3: Search vector DB (Filtered by user and timeframe)
                var similarEmails = await _vectorDbService.SearchByVector(embedding, userId, limit, filterStartTime);

                // Step 4: Build context
                var context = similarEmails.Any() 
                    ? string.Join("\n", similarEmails.Select(e => $"- [{e.Timestamp:yyyy-MM-dd}] {e.Subject}: {e.Body.Substring(0, Math.Min(200, e.Body.Length))}..."))
                    : "No relevant emails found in the specified timeframe.";

                // Step 4: Ask Gemini to answer
                var answerPrompt = $@"Answer this question based ONLY on the provided context.
If the answer is not found in context, say 'No relevant emails found.'

Context:
{context}

Question: {query}

Answer:";

                var answer = await _intelligence.Ask(answerPrompt);

                var executionTime = (DateTime.UtcNow - executionStartTime).TotalMilliseconds;

                return new AgentResponse
                {
                    Success = true,
                    Response = answer,
                    Emails = similarEmails,
                    Metrics = new ExecutionMetrics
                    {
                        TotalTimeMs = (int)executionTime,
                        TokensUsed = EstimateTokens(answer),
                        CacheHits = 0
                    }
                };
            }
            catch (Exception ex)
            {
                return new AgentResponse
                {
                    Success = false,
                    Response = $"Error: {ex.Message}",
                    Emails = new()
                };
            }
        }

        private int EstimateTokens(string text)
        {
            // Rough estimation: 1 token ≈ 4 characters
            return text.Length / 4;
        }
    }
}

public class SearchIntent
{
    public string SearchTerm { get; set; }
    public int? DaysAgo { get; set; }
}
