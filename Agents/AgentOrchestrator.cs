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
            var startTime = DateTime.UtcNow;

            try
            {
                // Step 1: Generate embedding for query
                var embedding = await _embeddingService.GenerateEmbedding(query);

                // Step 2: Search vector DB (Filtered by user)
                var similarEmails = await _vectorDbService.SearchByVector(embedding, userId, limit);

                // Step 3: Build context
                var context = string.Join("\n", similarEmails.Select(e =>
                    $"- {e.Subject}: {e.Body.Substring(0, Math.Min(200, e.Body.Length))}..."));

                // Step 4: Ask Gemini to answer
                var answerPrompt = $@"Answer this question based ONLY on the provided context.
If the answer is not found in context, say 'No relevant emails found.'

Context:
{context}

Question: {query}

Answer:";

                var answer = await _intelligence.Ask(answerPrompt);

                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

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
