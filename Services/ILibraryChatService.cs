using System.Threading.Tasks;

namespace BOOK.Services
{
    public interface ILibraryChatService
    {
        Task<string> GetLibrarianResponseAsync(string userMessage);
    }
}
