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

        public async Task<Email> GetEmailById(string emailId, string userId)
        {
            return await _collection.Find(e => e.Id == emailId && e.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<List<Email>> SearchByVector(double[] embedding, string userId, int limit = 5)
        {
            try
            {
                // Atlas Vector Search usually requires a $vectorSearch stage or similar depending on the exact provider.
                // For simplicity and multi-tenancy, we will use a filter if supported, 
                // but here we will implement the $match to ensure the user only sees their data.
                
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
                                        { "vector", new BsonArray(embedding.Cast<BsonValue>()) },
                                        { "k", limit * 2 } // Search more to allow for user filtering
                                    }
                                }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "$match", new BsonDocument { { "userId", userId } } }
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
                return await _collection.Find(e => e.UserId == userId).Limit(limit).ToListAsync();
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
