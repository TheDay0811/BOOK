using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BOOK.Services
{
    public class GeminiService : ILibraryChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetLibrarianResponseAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return "Chào bạn! Tôi là Thủ thư ảo HUFLIT cơ sở Hóc Môn. Bạn cần hỗ trợ tra cứu sách hay quy định mượn trả gì ạ?";
            }

            var apiKey = _configuration["GeminiSettings:ApiKey"] ?? _configuration["GeminiApiKey"];
            var model = _configuration["GeminiSettings:Model"] ?? "gemini-1.5-flash";

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                    var systemPrompt = "Bạn là Trợ lý Thư viện ảo của Trường Đại học Ngoại ngữ - Tin học TP.HCM (HUFLIT) cơ sở Hóc Môn (Địa chỉ: Số 80/2 Bà Triệu, Xã Tân Xuân, Huyện Hóc Môn, TP. Hồ Chí Minh. Hotline: (028) 38 632 052, Email: thuvien@huflit.edu.vn). Hãy hỗ trợ độc giả giải đáp các thắc mắc về nội quy mượn trả (mượn tối đa 3 cuốn trong 15 ngày, thẻ bị khóa nếu làm hỏng/mất sách và phải bồi thường để mở lại), tìm kiếm sách và vị trí quầy thư viện bằng tiếng Việt lịch sự, thân thiện, súc tích.";

                    var requestPayload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role = "user",
                                parts = new[] { new { text = userMessage } }
                            }
                        },
                        systemInstruction = new
                        {
                            parts = new[] { new { text = systemPrompt } }
                        },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 800
                        }
                    };

                    var jsonString = JsonSerializer.Serialize(requestPayload);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(endpoint, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseBody);
                        var candidates = doc.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            var text = candidates[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                return text.Trim();
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Gemini API returned status code {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gọi Gemini API, chuyển sang trả lời theo tri thức nội quy HUFLIT.");
                }
            }

            // =========================================================================
            // CHẾ ĐỘ DỰ PHÒNG THÔNG MINH THEO CHUẨN NỘI QUY THƯ VIỆN HUFLIT HÓC MÔN
            // =========================================================================
            return GenerateSmartHuflitResponse(userMessage);
        }

        private static string GenerateSmartHuflitResponse(string userMessage)
        {
            var msg = userMessage.ToLower().Trim();

            if (msg.Contains("mượn") && (msg.Contains("mấy") || msg.Contains("bao nhiêu") || msg.Contains("tối đa")))
            {
                return "Theo Quy chế Thư viện HUFLIT cơ sở Hóc Môn: Mỗi sinh viên được mượn tối đa 3 cuốn sách trong một lượt mượn. Thời hạn mượn tối đa là 15 ngày kể từ ngày duyệt phiếu. Bạn phải trả hết sách đang mượn mới được mượn tiếp nhé!";
            }

            if (msg.Contains("hạn") || msg.Contains("bao lâu") || msg.Contains("ngày") || msg.Contains("15 ngày"))
            {
                return "Thời hạn mượn sách chuẩn tại Thư viện HUFLIT là tối đa 15 ngày. Nếu cần thêm thời gian nghiên cứu, bạn có thể vào trang 'Hồ Sơ & Mượn Trả' để gia hạn thêm 07 ngày (áp dụng khi sách chưa có độc giả khác đặt trước).";
            }

            if (msg.Contains("mất") || msg.Contains("hỏng") || msg.Contains("rách") || msg.Contains("bồi thường"))
            {
                return "Nếu làm mất hoặc hỏng sách, Thẻ thư viện của bạn sẽ tự động chuyển sang trạng thái 'Không sử dụng' (bị khóa tạm thời). Bạn vui lòng mang sách đến quầy Thủ thư tại Cơ sở Hóc Môn (Số 80/2 Bà Triệu, Tân Xuân) để ghi nhận bồi thường. Sau khi hoàn tất bồi thường, thẻ sẽ được khôi phục 'Sử dụng được' ngay lập tức!";
            }

            if (msg.Contains("quên mật khẩu") || msg.Contains("đổi mật khẩu") || msg.Contains("mật khẩu"))
            {
                return "Để đặt lại mật khẩu tài khoản thư viện HUFLIT, bạn vui lòng mang Thẻ sinh viên đến trực tiếp Quầy Thủ thư tại Thư viện cơ sở Hóc Môn hoặc gửi email từ hòm thư sinh viên về thuvien@huflit.edu.vn để được hỗ trợ cấp lại nhé.";
            }

            if (msg.Contains("địa chỉ") || msg.Contains("ở đâu") || msg.Contains("cơ sở") || msg.Contains("vị trí"))
            {
                return "Thư viện HUFLIT cơ sở Hóc Môn tọa lạc tại: Số 80/2 Bà Triệu, Xã Tân Xuân, Huyện Hóc Môn, TP. Hồ Chí Minh. Thời gian phục vụ: Thứ 2 đến Thứ 7, từ 07:30 đến 21:00 hàng ngày.";
            }

            if (msg.Contains("giờ") || msg.Contains("mở cửa") || msg.Contains("đóng cửa"))
            {
                return "Giờ hoạt động Thư viện HUFLIT cơ sở Hóc Môn: Mở cửa từ Thứ 2 đến Thứ 7 (07:30 - 21:00). Chủ nhật và các ngày lễ nghỉ theo quy định của Nhà trường.";
            }

            if (msg.Contains("thẻ") || msg.Contains("đăng ký") || msg.Contains("làm thẻ"))
            {
                return "Để đăng ký thẻ thư viện, bạn chỉ cần bấm vào nút 'Đăng ký thẻ mới' ở góc trên cùng bên phải màn hình, điền MSSV, họ tên và mật khẩu. Hệ thống sẽ tự động cấp Thẻ thư viện LIB-{MSSV} có thời hạn 1 năm để bạn sử dụng ngay!";
            }

            if (msg.Contains("chào") || msg.Contains("hi") || msg.Contains("hello"))
            {
                return "Xin chào bạn! Tôi là Trợ lý Thư viện ảo của Trường Đại học Ngoại ngữ - Tin học TP.HCM (HUFLIT) cơ sở Hóc Môn. Tôi có thể hỗ trợ bạn tra cứu sách, hướng dẫn quy định mượn trả và thủ tục thư viện. Bạn cần giúp gì ạ?";
            }

            return "Chào bạn! Thư viện HUFLIT cơ sở Hóc Môn hiện có sẵn trên 50.000 đầu sách thuộc các khối ngành CNTT, Ngoại ngữ, Kinh tế, Du lịch. Quy định chung: Mỗi lần mượn tối đa 3 cuốn/15 ngày, trả xong mới mượn tiếp. Bạn cần hỗ trợ thêm thông tin cụ thể nào ạ?";
        }
    }
}
