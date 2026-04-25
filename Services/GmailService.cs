using EmailAI.Agent.Models;
using EmailAI.Agent.Agents;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EmailAI.Agent.Services
{
    public class GmailService
    {
        private readonly EmailAgent _emailAgent;
        private readonly VectorDbService _vectorDb;
        private readonly IHttpClientFactory _httpClientFactory;

        public GmailService(EmailAgent emailAgent, VectorDbService vectorDb, IHttpClientFactory httpClientFactory)
        {
            _emailAgent = emailAgent;
            _vectorDb = vectorDb;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<int> SyncLatestEmails(string userId, string accessToken, int count = 50)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // 1. Get list of messages
            var listUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults={count}";
            var listResponse = await client.GetAsync(listUrl);
            if (!listResponse.IsSuccessStatusCode) return 0;

            var listJson = await listResponse.Content.ReadAsStringAsync();
            var messageList = JsonSerializer.Deserialize<GmailMessageList>(listJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (messageList?.Messages == null) return 0;

            int syncedCount = 0;
            foreach (var msgRef in messageList.Messages)
            {
                try
                {
                    // Skip if already synced
                    if (await _vectorDb.EmailExistsByGmailId(msgRef.Id, userId))
                    {
                        continue;
                    }

                    // 2. Get message details
                    var detailsUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{msgRef.Id}";
                    var detailsResponse = await client.GetAsync(detailsUrl);
                    if (!detailsResponse.IsSuccessStatusCode) continue;

                    var detailsJson = await detailsResponse.Content.ReadAsStringAsync();
                    var gmailMsg = JsonSerializer.Deserialize<GmailMessage>(detailsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (gmailMsg == null) continue;

                    // Parse Subject and Sender from headers
                    var subject = gmailMsg.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value ?? "No Subject";
                    var from = gmailMsg.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value ?? "Unknown Sender";
                    var snippet = gmailMsg.Snippet ?? "";

                    // 3. Process with EmailAgent (Categorize & Summarize)
                    var processedEmail = await _emailAgent.ProcessEmail(subject, snippet, from, userId, msgRef.Id);
                    
                    // Convert internal Gmail timestamp (ms) to DateTime
                    if (long.TryParse(gmailMsg.InternalDate, out long ms))
                    {
                        processedEmail.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                    }

                    // 4. Store in Vector DB
                    await _vectorDb.StoreEmail(processedEmail);
                    syncedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error syncing message {msgRef.Id}: {ex.Message}");
                }
            }

            return syncedCount;
        }

        private class GmailMessageList
        {
            public List<GmailMessageRef> Messages { get; set; }
        }

        private class GmailMessageRef
        {
            public string Id { get; set; }
        }

        private class GmailMessage
        {
            public string Id { get; set; }
            public string Snippet { get; set; }
            public string InternalDate { get; set; }
            public GmailPayload Payload { get; set; }
        }

        private class GmailPayload
        {
            public List<GmailHeader> Headers { get; set; }
        }

        private class GmailHeader
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}
