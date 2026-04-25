using EmailAI.Agent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class EmailIngestionService
    {
        private readonly EmbeddingService _embeddingService;
        private readonly IntelligenceOrchestrator _intelligence;
        private readonly VectorDbService _vectorDbService;

        public EmailIngestionService(
            EmbeddingService embeddingService,
            IntelligenceOrchestrator intelligence,
            VectorDbService vectorDbService)
        {
            _embeddingService = embeddingService;
            _intelligence = intelligence;
            _vectorDbService = vectorDbService;
        }

        public async Task<Email> ProcessAndStoreEmail(string subject, string body, string sender, string userId, string? gmailId = null)
        {
            // Step 1: Generate embedding
            var embedding = await _embeddingService.GenerateEmbedding($"{subject} {body}");

            // Step 2: Classify with Gemini
            var classificationPrompt = $@"Classify this email into EXACTLY ONE category: Urgent, Finance, HR, or General
Respond ONLY with the category name (one word).

Subject: {subject}
Body: {body}";

            var category = await _intelligence.Ask(classificationPrompt);
            category = category.Trim();

            // Step 3: Summarize with Gemini
            var summaryPrompt = $@"Summarize this email in exactly 3 bullet points. Each point max 12 words.
Respond ONLY with bullet points (no numbering).

{body}";

            var summaryText = await _intelligence.Ask(summaryPrompt);
            var summary = summaryText.Split('\n')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(3)
                .ToList();

            // Step 4: Action suggestion with Gemini
            var actionPrompt = $@"Suggest ONE action for this email: Reply, Escalate, or Ignore
Respond ONLY with the action name (one word).

{body}";

            var action = await _intelligence.Ask(actionPrompt);
            action = action.Trim();

            // Step 5: Store in MongoDB
            var email = new Email
            {
                UserId = userId,
                GmailId = gmailId,
                Subject = subject,
                Body = body,
                Sender = sender,
                Embedding = embedding,
                Category = category,
                Summary = summary,
                Action = action,
                Timestamp = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            return await _vectorDbService.StoreEmail(email);
        }
    }
}
