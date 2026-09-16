using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class MemberController : Controller
    {
        // GET: /Member
        public IActionResult Index()
        {
            var viewModel = new MemberPortalViewModel
            {
                FullName = "Trần Văn Hoàng",
                MemberCode = "22110098",
                Email = "hoang.tv@student.edu.vn",
                PhoneNumber = "0912 333 444",
                Faculty = "Khoa Công Nghệ Thông Tin",
                ActiveLoans = new List<MemberActiveBorrowViewModel>
                {
                    new MemberActiveBorrowViewModel
                    {
                        BorrowId = 8801,
                        BookTitle = "Cấu Trúc Dữ Liệu & Giải Thuật Ứng Dụng",
                        BookISBN = "978-604-0-12345-6",
                        CoverImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=400&auto=format&fit=crop&q=80",
                        BorrowDate = DateTime.Today.AddDays(-16),
                        DueDate = DateTime.Today.AddDays(-2),
                        RenewalCount = 1
                    },
                    new MemberActiveBorrowViewModel
                    {
                        BorrowId = 8820,
                        BookTitle = "Lập Trình Web Hiện Đại Với ASP.NET Core",
                        BookISBN = "978-604-67-1901-3",
                        CoverImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=400&auto=format&fit=crop&q=80",
                        BorrowDate = DateTime.Today.AddDays(-5),
                        DueDate = DateTime.Today.AddDays(9),
                        RenewalCount = 0
                    }
                },
                BorrowHistory = new List<MemberBorrowHistoryViewModel>
                {
                    new MemberBorrowHistoryViewModel
                    {
                        Id = 1,
                        BookTitle = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                        BorrowDate = DateTime.Today.AddMonths(-2),
                        ReturnDate = DateTime.Today.AddMonths(-2).AddDays(14),
                        Status = "Đã hoàn tất (Đúng hạn)",
                        FinePaid = 0
                    },
                    new MemberBorrowHistoryViewModel
                    {
                        Id = 2,
                        BookTitle = "Tâm Lý Học Nhận Thức & Tư Duy Phản Biện",
                        BorrowDate = DateTime.Today.AddMonths(-3),
                        ReturnDate = DateTime.Today.AddMonths(-3).AddDays(18),
                        Status = "Đã hoàn tất (Trễ 4 ngày)",
                        FinePaid = 8000
                    }
                },
                LateFeeInfo = new MemberLateFeeViewModel
                {
                    TotalUnpaidFine = 4000
                }
            };

            return View(viewModel);
        }
    }
}
