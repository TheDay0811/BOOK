using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BOOK.Models.MongoEntities
{
    public class ChatHistoryLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = "gemini-1.5-flash";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
