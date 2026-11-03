using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BOOK.Models.MongoEntities;
using MongoDB.Driver;

namespace BOOK.Services
{
    public interface ILibraryMongoLogService
    {
        Task LogAsync(string action, string username, string role, string details, string ipAddress = "127.0.0.1");
        Task LogChatAsync(string username, string prompt, string response, string model = "gemini-1.5-flash");
        Task<List<SystemAuditLog>> GetRecentLogsAsync(int count = 50);
        Task<List<ChatHistoryLog>> GetRecentChatLogsAsync(int count = 50);
    }

    public class LibraryMongoLogService : ILibraryMongoLogService
    {
        private readonly IMongoCollection<SystemAuditLog> _logs;
        private readonly IMongoCollection<ChatHistoryLog> _chats;

        public LibraryMongoLogService(IMongoDatabase database)
        {
            _logs = database.GetCollection<SystemAuditLog>("system_audit_logs");
            _chats = database.GetCollection<ChatHistoryLog>("chat_history_logs");
        }

        public async Task LogAsync(string action, string username, string role, string details, string ipAddress = "127.0.0.1")
        {
            try
            {
                var entry = new SystemAuditLog
                {
                    Action = action,
                    Username = username,
                    UserRole = role,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };
                await _logs.InsertOneAsync(entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MongoDB Log Error]: {ex.Message}");
            }
        }

        public async Task LogChatAsync(string username, string prompt, string response, string model = "gemini-1.5-flash")
        {
            try
            {
                var entry = new ChatHistoryLog
                {
                    Username = username,
                    UserPrompt = prompt,
                    BotResponse = response,
                    ModelUsed = model,
                    Timestamp = DateTime.UtcNow
                };
                await _chats.InsertOneAsync(entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MongoDB Chat Log Error]: {ex.Message}");
            }
        }

        public async Task<List<SystemAuditLog>> GetRecentLogsAsync(int count = 50)
        {
            try
            {
                return await _logs.Find(_ => true)
                    .SortByDescending(l => l.Timestamp)
                    .Limit(count)
                    .ToListAsync();
            }
            catch
            {
                return new List<SystemAuditLog>();
            }
        }

        public async Task<List<ChatHistoryLog>> GetRecentChatLogsAsync(int count = 50)
        {
            try
            {
                return await _chats.Find(_ => true)
                    .SortByDescending(c => c.Timestamp)
                    .Limit(count)
                    .ToListAsync();
            }
            catch
            {
                return new List<ChatHistoryLog>();
            }
        }
    }
}
