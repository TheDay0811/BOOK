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

        public ChatController(ILibraryChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { reply = "Bạn vui lòng nhập nội dung câu hỏi nhé!" });
            }

            var reply = await _chatService.GetLibrarianResponseAsync(request.Message);
            return Json(new { reply });
        }
    }
}
