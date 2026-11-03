using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BOOK.Services;

namespace BOOK.Controllers
{
    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatController : Controller
    {
        private readonly ILibraryChatService _chatService;
        private readonly ILibraryMongoLogService _mongoLogService;

        public ChatController(ILibraryChatService chatService, ILibraryMongoLogService mongoLogService)
        {
            _chatService = chatService;
            _mongoLogService = mongoLogService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { reply = "Bạn vui lòng nhập nội dung câu hỏi nhé!", model = "none" });
            }

            var result = await _chatService.GetLibrarianResponseAsync(request.Message);

            // Tự động lưu hội thoại vào MongoDB NoSQL với đúng tên model thực tế
            var username = User.Identity?.IsAuthenticated == true ? User.Identity.Name! : "Khách độc giả";
            await _mongoLogService.LogChatAsync(username, request.Message, result.Reply, result.ModelUsed);

            return Json(new { reply = result.Reply, model = result.ModelUsed });
        }
    }
}
