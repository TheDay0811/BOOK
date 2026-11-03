using System;
using System.Threading.Tasks;
using BOOK.Services;

namespace BOOK.Data
{
    public static class MongoSeeder
    {
        public static async Task SeedInitialLogsAsync(ILibraryMongoLogService logService)
        {
            var recent = await logService.GetRecentLogsAsync(5);
            if (recent.Count == 0)
            {
                // Nạp các nhật ký kiểm toán mẫu vào MongoDB
                await logService.LogAsync("SYSTEM_INIT", "System", "SuperAdmin", "Hệ thống Quản lý Thư viện HUFLIT khởi tạo thành công trên kiến trúc Hybrid SQL Server + MongoDB.");
                await logService.LogAsync("SEED_DATA", "System", "SuperAdmin", "Khởi tạo 8 đầu sách giáo trình, 2 tài khoản quản trị (admin, thuthu) và danh mục thể loại.");
                await logService.LogAsync("LOGIN", "admin", "Admin", "Quản trị viên đăng nhập vào trang quản trị hệ thống.");
                await logService.LogAsync("BORROW", "thuthu", "Librarian", "Thủ thư tạo phiếu mượn mã #1 cho sinh viên Nguyễn Văn A (LIB-24DH112970).");
                await logService.LogAsync("FINE_PAYMENT", "thuthu", "Librarian", "Xác nhận thu phí phạt trễ hạn 10.000đ cho phiếu mượn #1.");

                // Nạp lịch sử chat AI mẫu vào MongoDB
                await logService.LogChatAsync("24DH112970", "Thư viện có sách Database System Concepts không bạn?", "Chào bạn! Thư viện hiện có cuốn Database System Concepts (Ấn bản thứ 7) tại Kho lưu thông Hóc Môn, tình trạng: Còn sách.", "gemini-1.5-flash");
                await logService.LogChatAsync("24DH112970", "Quy định phạt trễ hạn sách là bao nhiêu một ngày?", "Quy định thư viện tính phí phạt trễ hạn là 2.000đ/ngày cho mỗi cuốn sách.", "gemini-1.5-flash");
            }
        }
    }
}
