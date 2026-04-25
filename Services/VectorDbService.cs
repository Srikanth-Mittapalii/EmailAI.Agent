using EmailAI.Agent.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class VectorDbService
    {
        private readonly IMongoCollection<Email> _collection;

        public VectorDbService(IMongoDatabase database)
        {
            _collection = database.GetCollection<Email>("Emails");
        }

        public async Task<Email> StoreEmail(Email email)
        {
            email.ProcessedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(email);
            return email;
        }

        public async Task<bool> EmailExistsByGmailId(string gmailId, string userId)
        {
            return await _collection.Find(e => e.GmailId == gmailId && e.UserId == userId).AnyAsync();
        }
 
        public async Task<Email> GetEmailById(string emailId, string userId)
        {
            return await _collection.Find(e => e.Id == emailId && e.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<List<Email>> SearchByVector(double[] embedding, string userId, int limit = 5, DateTime? startTime = null)
        {
            try
            {
                var matchFilter = new BsonDocument { { "userId", userId } };
                if (startTime.HasValue)
                {
                    matchFilter.Add("timestamp", new BsonDocument { { "$gte", startTime.Value } });
                }

                var pipelineDefinition = new BsonDocument[]
                {
                    new BsonDocument
                    {
                        {
                            "$search",
                            new BsonDocument
                            {
                                { "cosmosSearch", new BsonDocument
                                    {
                                        { "vector", new BsonArray(embedding.Select(d => (BsonValue)d)) },
                                        { "k", limit * 5 } 
                                    }
                                }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "$match", matchFilter }
                    },
                    new BsonDocument
                    {
                        { "$limit", limit }
                    }
                };

                return await _collection.Aggregate<Email>(pipelineDefinition)
                    .ToListAsync();
            }
            catch
            {
                // Fallback to simple query with user filter
                var query = _collection.Find(e => e.UserId == userId);
                if (startTime.HasValue)
                {
                    query = _collection.Find(e => e.UserId == userId && e.Timestamp >= startTime.Value);
                }
                return await query.Limit(limit).ToListAsync();
            }
        }

        public async Task<List<Email>> SearchByCategory(string category, string userId)
        {
            var filter = Builders<Email>.Filter.And(
                Builders<Email>.Filter.Eq(e => e.Category, category),
                Builders<Email>.Filter.Eq(e => e.UserId, userId)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<List<Email>> GetAllEmails(string userId)
        {
            return await _collection.Find(e => e.UserId == userId).SortByDescending(e => e.Timestamp).ToListAsync();
        }
    }
}
