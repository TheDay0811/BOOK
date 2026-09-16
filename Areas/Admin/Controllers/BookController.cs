using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookController : Controller
    {
        // GET: /Admin/Book/Index
        public IActionResult Index(string? search, string? category, string? status)
        {
            var books = new List<BookItemViewModel>
            {
                new BookItemViewModel
                {
                    Id = 1,
                    Title = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                    Author = "Martin Fowler & TS. Đặng Văn Nam",
                    CategoryName = "Công Nghệ Thông Tin",
                    PublisherName = "NXB Bách Khoa Hà Nội",
                    PublishYear = 2024,
                    ISBN = "978-604-938-120-1",
                    CoverImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=200&auto=format&fit=crop&q=80",
                    TotalCopies = 8,
                    AvailableCopies = 5,
                    ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 03"
                },
                new BookItemViewModel
                {
                    Id = 2,
                    Title = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng",
                    Author = "PGS. TS. Lương Chi Mai",
                    CategoryName = "Khoa Học Dữ Liệu",
                    PublisherName = "NXB Đại Học Quốc Gia",
                    PublishYear = 2023,
                    ISBN = "978-604-0-32115-4",
                    CoverImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=200&auto=format&fit=crop&q=80",
                    TotalCopies = 6,
                    AvailableCopies = 2,
                    ShelfLocation = "Tầng 2 - Dãy AI - Kệ 01"
                },
                new BookItemViewModel
                {
                    Id = 3,
                    Title = "Quản Trị Chiến Lược & Chuyển Đổi Số Doanh Nghiệp",
                    Author = "Michael Porter & GS. Vũ Minh Khương",
                    CategoryName = "Kinh Tế & Quản Trị",
                    PublisherName = "NXB Kinh Tế Quốc Dân",
                    PublishYear = 2024,
                    ISBN = "978-604-95-8820-2",
                    CoverImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=200&auto=format&fit=crop&q=80",
                    TotalCopies = 4,
                    AvailableCopies = 0,
                    ShelfLocation = "Tầng 3 - Dãy KT - Kệ 12"
                },
                new BookItemViewModel
                {
                    Id = 4,
                    Title = "Cơ Sở An Toàn Thông Tin & Mật Mã Ứng Dụng",
                    Author = "TS. Nguyễn Quốc Toàn",
                    CategoryName = "An Toàn Thông Tin",
                    PublisherName = "NXB Thông Tin & Truyền Thông",
                    PublishYear = 2023,
                    ISBN = "978-604-80-5541-9",
                    CoverImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=200&auto=format&fit=crop&q=80",
                    TotalCopies = 10,
                    AvailableCopies = 7,
                    ShelfLocation = "Tầng 2 - Dãy CS - Kệ 05"
                },
                new BookItemViewModel
                {
                    Id = 5,
                    Title = "Lập Trình Web Hiện Đại Với ASP.NET Core & React",
                    Author = "Dương Thành Phết & ThS. Lê Hữu Lộc",
                    CategoryName = "Công Nghệ Thông Tin",
                    PublisherName = "NXB Khoa Học & Kỹ Thuật",
                    PublishYear = 2024,
                    ISBN = "978-604-67-1901-3",
                    CoverImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=200&auto=format&fit=crop&q=80",
                    TotalCopies = 5,
                    AvailableCopies = 4,
                    ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 01"
                }
            };

            return View(books);
        }

        // GET: /Admin/Book/Details/1
        public IActionResult Details(int id = 1)
        {
            var model = new BookTitleManageViewModel
            {
                BookId = id,
                BookCode = "CS-001",
                Title = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                Author = "Martin Fowler & TS. Đặng Văn Nam",
                CategoryName = "Công Nghệ Thông Tin",
                PublisherName = "NXB Bách Khoa Hà Nội",
                PublishYear = 2024,
                CoverImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=200&auto=format&fit=crop&q=80",
                Copies = new List<BookCopyViewModel>
                {
                    new BookCopyViewModel { Id = 1, BookId = id, BookCode = "CS-001", ControlNumber = "001", Status = "Còn", Notes = "Sách nguyên vẹn mới 100%", ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 03" },
                    new BookCopyViewModel { Id = 2, BookId = id, BookCode = "CS-001", ControlNumber = "002", Status = "Còn", Notes = "Sách tốt, có dấu thư viện", ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 03" },
                    new BookCopyViewModel { Id = 3, BookId = id, BookCode = "CS-001", ControlNumber = "003", Status = "Cho mượn", Notes = "Đang cho sinh viên 22110098 mượn", ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 03" },
                    new BookCopyViewModel { Id = 4, BookId = id, BookCode = "CS-001", ControlNumber = "004", Status = "Hỏng", Notes = "Rách 20 trang phụ lục, đề xuất thanh lý", ShelfLocation = "Kho bảo trì tầng 1" }
                }
            };
            return View(model);
        }
    }
}
