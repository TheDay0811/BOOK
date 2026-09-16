using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class BookController : Controller
    {
        // Mock dataset for catalog presentation
        private static readonly List<BookItemViewModel> SampleBooks = new()
        {
            new BookItemViewModel
            {
                Id = 1,
                Title = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp (Enterprise Architecture)",
                Author = "Martin Fowler & TS. Đặng Văn Nam",
                CategoryName = "Công Nghệ Thông Tin",
                PublisherName = "NXB Bách Khoa Hà Nội",
                PublishYear = 2024,
                ISBN = "978-604-938-120-1",
                CoverImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=500&auto=format&fit=crop&q=80",
                AvailableCopies = 5,
                TotalCopies = 8,
                ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 03",
                Rating = 4.9,
                ReviewCount = 38,
                Description = "Tài liệu chuyên khảo cung cấp các mẫu thiết kế (Design Patterns), kiến trúc Microservices, Domain-Driven Design (DDD) và Clean Architecture ứng dụng trong các hệ thống quy mô lớn.",
                PageCount = 560
            },
            new BookItemViewModel
            {
                Id = 2,
                Title = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng (AI & Machine Learning)",
                Author = "PGS. TS. Lương Chi Mai",
                CategoryName = "Khoa Học Dữ Liệu",
                PublisherName = "NXB Đại Học Quốc Gia",
                PublishYear = 2023,
                ISBN = "978-604-0-32115-4",
                CoverImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=500&auto=format&fit=crop&q=80",
                AvailableCopies = 2,
                TotalCopies = 6,
                ShelfLocation = "Tầng 2 - Dãy AI - Kệ 01",
                Rating = 4.8,
                ReviewCount = 42,
                Description = "Giáo trình hoàn chỉnh về học có giám sát, học không giám sát, mạng nơ-ron sâu (Deep Learning) và các mô hình xử lý ngôn ngữ tự nhiên hiện đại.",
                PageCount = 480
            },
            new BookItemViewModel
            {
                Id = 3,
                Title = "Quản Trị Chiến Lược & Chuyển Đổi Số Doanh Nghiệp",
                Author = "Michael E. Porter & GS. Vũ Minh Khương",
                CategoryName = "Kinh Tế & Quản Trị",
                PublisherName = "NXB Kinh Tế Quốc Dân",
                PublishYear = 2024,
                ISBN = "978-604-95-8820-2",
                CoverImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=500&auto=format&fit=crop&q=80",
                AvailableCopies = 0,
                TotalCopies = 4,
                ShelfLocation = "Tầng 3 - Dãy KT - Kệ 12",
                Rating = 4.7,
                ReviewCount = 29,
                Description = "Cung cấp khung lý thuyết và các nghiên cứu điển hình về tái cấu trúc chuỗi giá trị và tối ưu hóa năng lực cạnh tranh trong kỷ nguyên số.",
                PageCount = 420
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
                CoverImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=500&auto=format&fit=crop&q=80",
                AvailableCopies = 7,
                TotalCopies = 10,
                ShelfLocation = "Tầng 2 - Dãy CS - Kệ 05",
                Rating = 4.6,
                ReviewCount = 18,
                Description = "Hệ thống hóa các nguyên lý mật mã khóa đối xứng, khóa công khai, chữ ký số, giao thức bảo mật mạng và phòng chống tấn công mạng.",
                PageCount = 390
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
                CoverImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=500&auto=format&fit=crop&q=80",
                AvailableCopies = 4,
                TotalCopies = 5,
                ShelfLocation = "Tầng 2 - Dãy B4 - Kệ 01",
                Rating = 5.0,
                ReviewCount = 56,
                Description = "Hướng dẫn xây dựng Single Page Applications (SPA) với RESTful API trên ASP.NET Core 8 và giao diện Tailwind CSS kết hợp React hiện đại.",
                PageCount = 520
            },
            new BookItemViewModel
            {
                Id = 6,
                Title = "Tâm Lý Học Nhận Thức & Tư Duy Phản Biện",
                Author = "Daniel Kahneman & TS. Trần Thu Hương",
                CategoryName = "Khoa Học Xã Hội",
                PublisherName = "NXB Lao Động Xã Hội",
                PublishYear = 2022,
                ISBN = "978-604-59-9012-7",
                CoverImageUrl = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=500&auto=format&fit=crop&q=80",
                AvailableCopies = 3,
                TotalCopies = 5,
                ShelfLocation = "Tầng 3 - Dãy XH - Kệ 08",
                Rating = 4.9,
                ReviewCount = 64,
                Description = "Phân tích các bẫy nhận thức, thiên kiến vô thức và quy trình hình thành quyết định chuẩn xác trong học thuật và cuộc sống.",
                PageCount = 450
            }
        };

        // GET: /Book/Index
        public IActionResult Index(string? searchQuery, List<int>? selectedCategoryIds, string? availabilityFilter, string? sortBy, int currentPage = 1)
        {
            var query = SampleBooks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(q) || 
                                         b.Author.ToLower().Contains(q) || 
                                         b.ISBN.ToLower().Contains(q));
            }

            if (availabilityFilter == "available")
            {
                query = query.Where(b => b.IsAvailable);
            }
            else if (availabilityFilter == "out_of_stock")
            {
                query = query.Where(b => !b.IsAvailable);
            }

            // Sorting
            query = sortBy switch
            {
                "rating_desc" => query.OrderByDescending(b => b.Rating),
                "title_asc" => query.OrderBy(b => b.Title),
                "popular" => query.OrderByDescending(b => b.ReviewCount),
                _ => query.OrderByDescending(b => b.PublishYear).ThenByDescending(b => b.Id)
            };

            var viewModel = new BookListViewModel
            {
                SearchQuery = searchQuery,
                SelectedCategoryIds = selectedCategoryIds ?? new List<int>(),
                AvailabilityFilter = availabilityFilter ?? "all",
                SortBy = sortBy ?? "newest",
                CurrentPage = currentPage,
                TotalPages = 1,
                TotalItems = query.Count(),
                Categories = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "Công Nghệ Thông Tin (12)" },
                    new SelectListItem { Value = "2", Text = "Khoa Học Dữ Liệu (8)" },
                    new SelectListItem { Value = "3", Text = "Kinh Tế & Quản Trị (15)" },
                    new SelectListItem { Value = "4", Text = "An Toàn Thông Tin (6)" },
                    new SelectListItem { Value = "5", Text = "Khoa Học Xã Hội (9)" }
                },
                Books = query.ToList()
            };

            return View(viewModel);
        }

        // GET: /Book/Details/1
        public IActionResult Details(int id)
        {
            var book = SampleBooks.FirstOrDefault(b => b.Id == id) ?? SampleBooks.First();

            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                Reviews = new List<BookReviewViewModel>
                {
                    new BookReviewViewModel
                    {
                        UserName = "ThS. Lê Hoàng Nam",
                        UserAvatarUrl = "",
                        UserRole = "Giảng viên CNTT",
                        Rating = 5,
                        CreatedAt = DateTime.Now.AddDays(-3),
                        Content = "Giáo trình trình bày logic, ví dụ minh họa gắn với bài toán thực tế của các hệ thống quy mô lớn. Sinh viên năm 3 trở lên rất nên đọc."
                    },
                    new BookReviewViewModel
                    {
                        UserName = "Phạm Quỳnh Anh",
                        UserAvatarUrl = "",
                        UserRole = "Sinh viên K21",
                        Rating = 5,
                        CreatedAt = DateTime.Now.AddDays(-10),
                        Content = "Sách in ấn rất đẹp, giải thích rõ các mẫu thiết kế khó hiểu. Thư viện bảo quản sách rất mới và sạch sẽ."
                    }
                },
                RelatedBooks = SampleBooks.Where(b => b.Id != book.Id).Take(3).ToList()
            };

            return View(viewModel);
        }

        // POST: /Book/AddReview
        [HttpPost]
        public IActionResult AddReview(int bookId, int rating, string content)
        {
            TempData["Success"] = "Cảm ơn bạn đã gửi đánh giá! Nhận xét sẽ được kiểm duyệt và hiển thị.";
            return RedirectToAction(nameof(Details), new { id = bookId });
        }
    }
}
