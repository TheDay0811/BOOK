using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BorrowController : Controller
    {
        // GET: /Admin/Borrow/Index
        public IActionResult Index()
        {
            var viewModel = new LibrarianDashboardViewModel
            {
                TodayReturnsCount = 18,
                PendingRequests = new List<PendingBorrowRequestViewModel>
                {
                    new PendingBorrowRequestViewModel
                    {
                        Id = 1,
                        RequestCode = "RQ-2026-0412",
                        MemberName = "Lê Thị Mai Linh",
                        MemberCode = "23110255",
                        MemberPhone = "0982 123 456",
                        BookTitle = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                        BookISBN = "978-604-938-120-1",
                        RequestDate = DateTime.Now.AddHours(-2),
                        PickupDueDate = DateTime.Today.AddDays(1),
                        Status = "Chờ duyệt"
                    },
                    new PendingBorrowRequestViewModel
                    {
                        Id = 2,
                        RequestCode = "RQ-2026-0413",
                        MemberName = "Vũ Đình Trọng",
                        MemberCode = "21120042",
                        MemberPhone = "0903 456 789",
                        BookTitle = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng",
                        BookISBN = "978-604-0-32115-4",
                        RequestDate = DateTime.Now.AddHours(-4),
                        PickupDueDate = DateTime.Today.AddDays(1),
                        Status = "Chờ duyệt"
                    },
                    new PendingBorrowRequestViewModel
                    {
                        Id = 3,
                        RequestCode = "RQ-2026-0414",
                        MemberName = "Đặng Phương Thảo",
                        MemberCode = "22130112",
                        MemberPhone = "0977 888 999",
                        BookTitle = "Cơ Sở An Toàn Thông Tin & Mật Mã",
                        BookISBN = "978-604-80-5541-9",
                        RequestDate = DateTime.Now.AddHours(-5),
                        PickupDueDate = DateTime.Today.AddDays(1),
                        Status = "Chờ duyệt"
                    }
                },
                RenewalRequests = new List<RenewalRequestViewModel>
                {
                    new RenewalRequestViewModel
                    {
                        RequestId = 101,
                        BorrowId = 55,
                        MemberName = "Phan Quốc Huy",
                        MemberCode = "20110034",
                        BookTitle = "Lập Trình Web Hiện Đại Với ASP.NET Core",
                        CurrentDueDate = DateTime.Today.AddDays(1),
                        RequestedNewDueDate = DateTime.Today.AddDays(15),
                        TimesRenewed = 1,
                        MaxAllowedRenewals = 2,
                        Reason = "Cần thêm thời gian làm đồ án môn học Kiến trúc Phần mềm"
                    },
                    new RenewalRequestViewModel
                    {
                        RequestId = 102,
                        BorrowId = 62,
                        MemberName = "Nguyễn Minh Châu",
                        MemberCode = "22140089",
                        BookTitle = "Tâm Lý Học Nhận Thức & Tư Duy Phản Biện",
                        CurrentDueDate = DateTime.Today,
                        RequestedNewDueDate = DateTime.Today.AddDays(14),
                        TimesRenewed = 0,
                        MaxAllowedRenewals = 2,
                        Reason = "Phục vụ đề tài tiểu luận kết thúc học phần"
                    }
                },
                OverdueAlerts = new List<OverdueAlertViewModel>
                {
                    new OverdueAlertViewModel
                    {
                        BorrowId = 8801,
                        MemberName = "Trần Văn Hoàng",
                        MemberCode = "22110098",
                        MemberEmail = "hoang.tv@student.edu.vn",
                        MemberPhone = "0912 333 444",
                        BookTitle = "Cấu Trúc Dữ Liệu & Giải Thuật Ứng Dụng",
                        DueDate = DateTime.Today.AddDays(-2),
                        DaysOverdue = 2,
                        AccumulatedFine = 4000,
                        LastNotifiedDate = DateTime.Today.AddDays(-1)
                    },
                    new OverdueAlertViewModel
                    {
                        BorrowId = 8790,
                        MemberName = "Hoàng Kim Oanh",
                        MemberCode = "21150021",
                        MemberEmail = "oanh.hk@student.edu.vn",
                        MemberPhone = "0934 555 666",
                        BookTitle = "Quản Trị Chiến Lược & Chuyển Đổi Số",
                        DueDate = DateTime.Today.AddDays(-7),
                        DaysOverdue = 7,
                        AccumulatedFine = 14000,
                        LastNotifiedDate = DateTime.Today.AddDays(-3)
                    }
                }
            };

            return View(viewModel);
        }

        // GET: /Admin/Borrow/Create
        public IActionResult Create()
        {
            var model = new CreateBorrowTicketViewModel
            {
                CardNumber = "LIB-2026-1024",
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(15),
                CardInfo = new CardLookupResultViewModel
                {
                    CardNumber = "LIB-2026-1024",
                    StudentName = "Trần Văn Hoàng",
                    StudentCode = "22110098",
                    ExpiryDate = DateTime.Today.AddYears(1),
                    CardStatus = "Sử dụng được",
                    HasUnreturnedBooks = false,
                    ActiveBorrowedCount = 0
                },
                SelectedItems = new List<SelectedBorrowItemViewModel>
                {
                    new SelectedBorrowItemViewModel
                    {
                        CopyId = 1,
                        BookCode = "CS-001",
                        BookTitle = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                        ControlNumber = "001",
                        ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 03"
                    },
                    new SelectedBorrowItemViewModel
                    {
                        CopyId = 2,
                        BookCode = "AI-002",
                        BookTitle = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng",
                        ControlNumber = "001",
                        ShelfLocation = "Tầng 2 - Dãy AI - Kệ 01"
                    }
                }
            };
            return View(model);
        }

        // GET: /Admin/Borrow/Return
        public IActionResult Return(string? ticketCode = "PM-2026-0881")
        {
            var model = new ReturnBookDeskViewModel
            {
                TicketCode = ticketCode ?? "PM-2026-0881",
                CardNumber = "LIB-2026-1024",
                StudentName = "Trần Văn Hoàng",
                StudentCode = "22110098",
                CurrentCardStatus = "Sử dụng được",
                CardExpiryDate = DateTime.Today.AddMonths(8),
                BorrowedCopies = new List<ReturnBookItemViewModel>
                {
                    new ReturnBookItemViewModel
                    {
                        CopyId = 1,
                        BookCode = "CS-001",
                        BookTitle = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                        ControlNumber = "001",
                        BorrowDate = DateTime.Today.AddDays(-14),
                        DueDate = DateTime.Today.AddDays(1),
                        ReturnCondition = "Bình thường"
                    },
                    new ReturnBookItemViewModel
                    {
                        CopyId = 2,
                        BookCode = "AI-002",
                        BookTitle = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng",
                        ControlNumber = "003",
                        BorrowDate = DateTime.Today.AddDays(-14),
                        DueDate = DateTime.Today.AddDays(1),
                        ReturnCondition = "Bình thường"
                    }
                }
            };
            return View(model);
        }
    }
}
