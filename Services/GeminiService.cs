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

            // 1. Phạt trễ hạn & Tiền phạt
            if (msg.Contains("phạt") || msg.Contains("trễ") || msg.Contains("quá hạn") || msg.Contains("tiền phạt") || msg.Contains("phí phạt"))
            {
                return "Theo Quy chế Thư viện HUFLIT: Phí phạt trễ hạn được tính tự động là 2.000 VNĐ / ngày cho mỗi cuốn sách trễ. Nếu có sách quá hạn chưa thanh toán, Thẻ thư viện của bạn sẽ bị khóa chức năng mượn mới. Bạn vui lòng mang sách và đóng phạt tại quầy Thủ thư để mở lại thẻ nhé!";
            }

            // 2. Mất sách / Hỏng sách / Bồi thường
            if (msg.Contains("mất") || msg.Contains("hỏng") || msg.Contains("rách") || msg.Contains("bồi thường"))
            {
                return "Quy định xử lý làm mất hoặc hỏng sách: Độc giả phải bồi thường theo đơn giá thực tế của sách hoặc mua sách mới đúng tựa và ấn bản để thay thế. Thẻ thư viện sẽ tạm thời bị khóa cho đến khi bạn hoàn tất thủ tục bồi thường tại Quầy Thủ thư Cơ sở Hóc Môn (Số 80/2 Bà Triệu, Tân Xuân).";
            }

            // 3. Gia hạn sách
            if (msg.Contains("gia hạn"))
            {
                return "Để gia hạn sách đang mượn: Bạn đăng nhập tài khoản sinh viên, vào trang 'Hồ Sơ & Mượn Trả', tìm phiếu mượn tương ứng và bấm nút 'Yêu cầu gia hạn'. Mỗi phiếu mượn được gia hạn thêm tối đa 07 ngày (áp dụng khi sách chưa có độc giả khác đặt mượn trước và phiếu chưa bị quá hạn).";
            }

            // 4. Đặt mượn sách online / trực tuyến
            if (msg.Contains("đặt mượn") || msg.Contains("mượn online") || msg.Contains("trực tuyến") || msg.Contains("đặt sách"))
            {
                return "Để đặt mượn sách trực tuyến: Bạn vào 'Danh mục sách', tìm cuốn sách cần đọc và bấm nút 'Đặt mượn sách'. Hệ thống sẽ tự động giữ sách cho bạn trong vòng 24 giờ để bạn đến Thư viện nhận sách vật lý tại quầy lưu thông.";
            }

            // 5. Số lượng sách & Thời hạn mượn thông thường
            if (msg.Contains("mấy cuốn") || msg.Contains("bao nhiêu cuốn") || msg.Contains("mấy cuốn sách") || (msg.Contains("mượn") && msg.Contains("tối đa")))
            {
                return "Theo Quy chế Thư viện HUFLIT cơ sở Hóc Môn: Mỗi sinh viên được mượn tối đa 3 cuốn sách trong một lượt mượn. Thời hạn mượn tiêu chuẩn là 15 ngày kể từ ngày nhận sách. Bạn cần hoàn trả hết sách của đợt mượn trước mới được tạo đợt mượn tiếp theo.";
            }

            // 6. Thời hạn mượn chung
            if (msg.Contains("thời hạn") || msg.Contains("bao lâu") || msg.Contains("15 ngày"))
            {
                return "Thời hạn mượn sách tiêu chuẩn tại Thư viện HUFLIT là 15 ngày. Bạn có thể theo dõi ngày hết hạn trên phiếu mượn hoặc tại trang 'Hồ Sơ & Mượn Trả' của mình.";
            }

            // 7. Quên mật khẩu / Đổi mật khẩu
            if (msg.Contains("quên mật khẩu") || msg.Contains("đổi mật khẩu") || msg.Contains("mật khẩu"))
            {
                return "Để đặt lại mật khẩu tài khoản thư viện HUFLIT, bạn vui lòng mang Thẻ sinh viên đến trực tiếp Quầy Thủ thư tại Thư viện cơ sở Hóc Môn hoặc gửi email từ hòm thư sinh viên về thuvien@huflit.edu.vn để được hỗ trợ cấp lại nhé.";
            }

            // 8. Địa chỉ / Cơ sở / Vị trí
            if (msg.Contains("địa chỉ") || msg.Contains("ở đâu") || msg.Contains("cơ sở") || msg.Contains("vị trí"))
            {
                return "Thư viện HUFLIT cơ sở Hóc Môn tọa lạc tại: Số 80/2 Bà Triệu, Xã Tân Xuân, Huyện Hóc Môn, TP. Hồ Chí Minh. Thời gian phục vụ: Thứ 2 đến Thứ 7, từ 07:30 đến 21:00 hàng ngày.";
            }

            // 9. Giờ mở cửa / Đóng cửa
            if (msg.Contains("giờ") || msg.Contains("mở cửa") || msg.Contains("đóng cửa"))
            {
                return "Giờ hoạt động Thư viện HUFLIT cơ sở Hóc Môn: Mở cửa từ Thứ 2 đến Thứ 7 (07:30 - 21:00). Chủ nhật và các ngày lễ nghỉ theo quy định của Nhà trường.";
            }

            // 10. Đăng ký thẻ / Làm thẻ
            if (msg.Contains("thẻ") || msg.Contains("đăng ký") || msg.Contains("làm thẻ"))
            {
                return "Để đăng ký thẻ thư viện, bạn chỉ cần bấm vào nút 'Đăng ký thẻ mới' ở góc trên cùng bên phải màn hình, điền MSSV, họ tên và mật khẩu. Hệ thống sẽ tự động cấp Thẻ thư viện LIB-{MSSV} có thời hạn 1 năm để bạn sử dụng ngay!";
            }

            // 11. Chào hỏi
            if (msg.Contains("chào") || msg.Contains("hi") || msg.Contains("hello"))
            {
                return "Xin chào bạn! Tôi là Trợ lý Thư viện ảo của Trường Đại học Ngoại ngữ - Tin học TP.HCM (HUFLIT) cơ sở Hóc Môn. Tôi có thể hỗ trợ bạn tra cứu sách, giải đáp quy định mượn trả, phí phạt và thủ tục thư viện. Bạn cần giúp gì ạ?";
            }

            return "Chào bạn! Thư viện HUFLIT cơ sở Hóc Môn hiện có sẵn trên 50.000 đầu sách thuộc các khối ngành CNTT, Ngoại ngữ, Kinh tế, Du lịch. Quy định chung: Mỗi lần mượn tối đa 3 cuốn/15 ngày, trả xong mới mượn tiếp. Bạn cần hỗ trợ thêm thông tin cụ thể nào ạ?";
        }
    }
}
