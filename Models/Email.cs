using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace EmailAI.Agent.Models
{
    public class Email
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        public string? UserId { get; set; }

        [BsonElement("gmailId")]
        public string? GmailId { get; set; }

        [BsonElement("subject")]
        public string Subject { get; set; }

        [BsonElement("body")]
        public string Body { get; set; }

        [BsonElement("sender")]
        public string Sender { get; set; }

        [BsonElement("embedding")]
        public double[] Embedding { get; set; }

        [BsonElement("category")]
        public string Category { get; set; }

        [BsonElement("summary")]
        public List<string> Summary { get; set; } = new();

        [BsonElement("action")]
        public string Action { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
