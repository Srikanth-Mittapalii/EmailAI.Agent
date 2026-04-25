using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EmailAI.Agent.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("passwordHash")]
        public string? PasswordHash { get; set; }

        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("googleId")]
        public string? GoogleId { get; set; }

        [BsonElement("profilePicture")]
        public string? ProfilePicture { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
