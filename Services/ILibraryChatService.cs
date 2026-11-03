using System.Threading.Tasks;

namespace BOOK.Services
{
    public class ChatResult
    {
        public string Reply { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = "qwen2.5:7b-instruct";
    }

    public interface ILibraryChatService
    {
        Task<ChatResult> GetLibrarianResponseAsync(string userMessage);
    }
}
