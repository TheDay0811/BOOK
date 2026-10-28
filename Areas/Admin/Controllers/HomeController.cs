using BOOK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ThuThu")]
    public class HomeController : Controller
    {
        // GET: /Admin/Home/Index
        public IActionResult Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                KpiTotalBooks = new AdminKpiCardViewModel
                {
                    Title = "Kho Sách & Bản Sao",
                    Value = "12.450",
                    Subtext = "48.600 bản sao vật lý",
                    ChangeText = "+8.2%",
                    IsPositive = true,
                    IconName = "book-copy",
                    ColorClass = "blue"
                },
                KpiActiveBorrows = new AdminKpiCardViewModel
                {
                    Title = "Đang Lưu Thông",
                    Value = "1.840",
                    Subtext = "Sách đang được mượn về",
                    ChangeText = "+12.5%",
                    IsPositive = true,
                    IconName = "book-open",
                    ColorClass = "emerald"
                },
                KpiPendingBorrows = new AdminKpiCardViewModel
                {
                    Title = "Chờ Phê Duyệt",
                    Value = "24",
                    Subtext = "Yêu cầu đặt mượn online",
                    ChangeText = "Cần xử lý trong ngày",
                    IsPositive = false,
                    IconName = "clock-arrow-up",
                    ColorClass = "amber"
                },
                KpiOverdueBooks = new AdminKpiCardViewModel
                {
                    Title = "Quá Hạn Chưa Trả",
                    Value = "18",
                    Subtext = "Đang tích lũy tiền phạt",
                    ChangeText = "Cần gửi email nhắc",
                    IsPositive = false,
                    IconName = "alert-octagon",
                    ColorClass = "rose"
                },
                RecentPendingRequests = new List<QuickPendingBorrowViewModel>
                {
                    new QuickPendingBorrowViewModel
                    {
                        Id = 1,
                        RequestCode = "RQ-2026-0412",
                        MemberName = "Lê Thị Mai Linh",
                        MemberCode = "23110255",
                        BookTitle = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                        BookISBN = "978-604-938-120-1",
                        RequestDate = DateTime.Now.AddMinutes(-25),
                        PickupDueDate = DateTime.Today.AddDays(1),
                        Priority = "Gấp"
                    },
                    new QuickPendingBorrowViewModel
                    {
                        Id = 2,
                        RequestCode = "RQ-2026-0413",
                        MemberName = "Vũ Đình Trọng",
                        MemberCode = "21120042",
                        BookTitle = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng",
                        BookISBN = "978-604-0-32115-4",
                        RequestDate = DateTime.Now.AddMinutes(-50),
                        PickupDueDate = DateTime.Today.AddDays(1),
                        Priority = "Bình thường"
                    },
                    new QuickPendingBorrowViewModel
                    {
                        Id = 3,
                        RequestCode = "RQ-2026-0414",
                        MemberName = "Đặng Phương Thảo",
                        MemberCode = "22130112",
                        BookTitle = "Cơ Sở An Toàn Thông Tin & Mật Mã",
                        BookISBN = "978-604-80-5541-9",
                        RequestDate = DateTime.Now.AddHours(-2),
                        PickupDueDate = DateTime.Today.AddDays(1),
                        Priority = "Bình thường"
                    },
                    new QuickPendingBorrowViewModel
                    {
                        Id = 4,
                        RequestCode = "RQ-2026-0415",
                        MemberName = "Trần Minh Quang",
                        MemberCode = "20110884",
                        BookTitle = "Lập Trình Web Hiện Đại Với ASP.NET Core",
                        BookISBN = "978-604-67-1901-3",
                        RequestDate = DateTime.Now.AddHours(-3),
                        PickupDueDate = DateTime.Today.AddDays(2),
                        Priority = "Gấp"
                    },
                    new QuickPendingBorrowViewModel
                    {
                        Id = 5,
                        RequestCode = "RQ-2026-0416",
                        MemberName = "Nguyễn Thu Hà",
                        MemberCode = "23140510",
                        BookTitle = "Tâm Lý Học Nhận Thức & Tư Duy Phản Biện",
                        BookISBN = "978-604-59-9012-7",
                        RequestDate = DateTime.Now.AddHours(-4),
                        PickupDueDate = DateTime.Today.AddDays(2),
                        Priority = "Bình thường"
                    }
                },
                MonthlyLabels = new List<string> { "T10/25", "T11/25", "T12/25", "T1/26", "T2/26", "T3/26", "T4/26", "T5/26", "T6/26", "T7/26", "T8/26", "T9/26" },
                MonthlyBorrowCounts = new List<int> { 980, 1120, 1450, 890, 1340, 1680, 1820, 1950, 1400, 1100, 1550, 1840 },
                MonthlyReturnCounts = new List<int> { 920, 1080, 1390, 850, 1280, 1600, 1750, 1880, 1350, 1050, 1490, 1780 },
                CategoryLabels = new List<string> { "Công Nghệ Thông Tin", "Kinh Tế & Quản Trị", "AI & Khoa Học Dữ Liệu", "An Toàn Thông Tin", "Khoa Học Xã Hội" },
                CategoryBookCounts = new List<int> { 4350, 3100, 2250, 1500, 1250 }
            };

            return View(viewModel);
        }
    }
}
