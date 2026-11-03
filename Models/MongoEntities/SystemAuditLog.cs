using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BOOK.Models.MongoEntities
{
    public class SystemAuditLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Action { get; set; } = string.Empty; // LOGIN, BORROW, RETURN, FINE_PAY, AI_CHAT, CARD_LOCK
        public string Username { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "127.0.0.1";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
