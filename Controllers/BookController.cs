using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using BOOK.Models.Entities;
namespace BOOK.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Book/Index
        public async Task<IActionResult> Index(
            string? searchQuery,
            string? keyword,
            int? categoryId,
            List<int>? selectedCategoryIds,
            string? availabilityFilter,
            string? sortBy,
            int currentPage = 1)
        {
            // Đồng bộ keyword từ form tìm kiếm khác nếu có
            if (!string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = keyword;
            }

            // Hỗ trợ categoryId đơn lẻ từ liên kết danh mục trang chủ
            selectedCategoryIds ??= new List<int>();
            if (categoryId.HasValue && !selectedCategoryIds.Contains(categoryId.Value))
            {
                selectedCategoryIds.Add(categoryId.Value);
            }

            var query = _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLower();
                query = query.Where(s => s.TenSach.ToLower().Contains(q) ||
                                         s.TacGia.ToLower().Contains(q) ||
                                         s.MaSachCode.ToLower().Contains(q) ||
                                         s.TheLoai.TenTheLoai.ToLower().Contains(q));
            }

            if (selectedCategoryIds.Count > 0)
            {
                query = query.Where(s => selectedCategoryIds.Contains(s.MaTheLoai));
            }

            if (availabilityFilter == "available")
            {
                query = query.Where(s => s.DauSachs.Any(d => d.TinhTrang == "Còn"));
            }
            else if (availabilityFilter == "out_of_stock")
            {
                query = query.Where(s => !s.DauSachs.Any(d => d.TinhTrang == "Còn"));
            }

            query = sortBy switch
            {
                "title_asc" => query.OrderBy(s => s.TenSach),
                "title_desc" => query.OrderByDescending(s => s.TenSach),
                "year_asc" => query.OrderBy(s => s.NamXuatBan),
                _ => query.OrderByDescending(s => s.NamXuatBan).ThenByDescending(s => s.MaSach)
            };

            var sachList = await query.ToListAsync();

            var books = sachList.Select(s => new BookItemViewModel
            {
                Id = s.MaSach,
                Title = s.TenSach,
                Author = s.TacGia,
                CategoryName = s.TheLoai.TenTheLoai,
                PublisherName = s.NhaXuatBan.TenNXB,
                PublishYear = s.NamXuatBan,
                ISBN = s.MaSachCode,
                AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                TotalCopies = s.DauSachs.Count,
                ShelfLocation = $"Kệ {s.MaSachCode} - Tầng 2",
                CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "/uploads/books/default-book.svg"
            }).ToList();

            var categories = await _context.TheLoais
                .Select(tl => new SelectListItem
                {
                    Value = tl.MaTheLoai.ToString(),
                    Text = $"{tl.TenTheLoai} ({tl.Sachs.Count})",
                    Selected = selectedCategoryIds.Contains(tl.MaTheLoai)
                }).ToListAsync();

            var viewModel = new BookListViewModel
            {
                SearchQuery = searchQuery,
                SelectedCategoryIds = selectedCategoryIds,
                AvailabilityFilter = availabilityFilter ?? "all",
                SortBy = sortBy ?? "newest",
                CurrentPage = currentPage,
                TotalPages = 1,
                TotalItems = books.Count,
                Categories = categories,
                Books = books
            };

            return View(viewModel);
        }

        // GET: /Book/Details/4
        public async Task<IActionResult> Details(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                ViewBag.RequestedBookId = id;
                return View("NotFound");
            }

            var book = new BookItemViewModel
            {
                Id = sach.MaSach,
                Title = sach.TenSach,
                Author = sach.TacGia,
                CategoryName = sach.TheLoai.TenTheLoai,
                PublisherName = sach.NhaXuatBan.TenNXB,
                PublishYear = sach.NamXuatBan,
                ISBN = sach.MaSachCode,
                AvailableCopies = sach.DauSachs.Count(d => d.TinhTrang == "Còn"),
                TotalCopies = sach.DauSachs.Count,
                ShelfLocation = $"Kệ {sach.MaSachCode} - Quầy Tầng 2",
                CoverImageUrl = !string.IsNullOrEmpty(sach.HinhAnh) ? sach.HinhAnh : "/uploads/books/default-book.svg"
            };

            var copies = sach.DauSachs.Select(d => new BookCopyViewModel
            {
                Id = d.MaDauSach,
                BookId = sach.MaSach,
                BookCode = sach.MaSachCode,
                ControlNumber = d.SoKiemSoat,
                Status = d.TinhTrang,
                Notes = string.IsNullOrWhiteSpace(d.GhiChu) ? "Nguyên vẹn trong kho" : d.GhiChu,
                ShelfLocation = $"Kệ {sach.MaSachCode}"
            }).OrderBy(c => c.ControlNumber).ToList();

            var related = await _context.Sachs
                .Where(s => s.MaTheLoai == sach.MaTheLoai && s.MaSach != sach.MaSach)
                .Include(s => s.DauSachs)
                .Take(3)
                .Select(s => new BookItemViewModel
                {
                    Id = s.MaSach,
                    Title = s.TenSach,
                    Author = s.TacGia,
                    ISBN = s.MaSachCode,
                    CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "/uploads/books/default-book.svg",
                    AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                    TotalCopies = s.DauSachs.Count
                }).ToListAsync();

            // Trạng thái thẻ và quyền mượn của độc giả đang đăng nhập
            string userCardStatus = "Sử dụng được";
            bool hasUnreturnedBooks = false;
            bool hasOverdueBooks = false;
            bool alreadyHoldingThisBook = false;
            int currentBorrowedCount = 0;
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (isAuthenticated)
            {
                var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
                if (!string.IsNullOrEmpty(studentCode))
                {
                    var the = await _context.TheThuViens
                        .Include(t => t.PhieuMuons)
                            .ThenInclude(p => p.ChiTiets)
                                .ThenInclude(c => c.DauSach)
                        .FirstOrDefaultAsync(t => t.TaiKhoan.Username == studentCode);

                    if (the != null)
                    {
                        userCardStatus = (DateTime.Today > the.NgayHetHan) ? "Không sử dụng" : the.TinhTrangSuDung;
                        var activeLoans = the.PhieuMuons
                            .Where(p => p.TrangThai == "Đang mượn" || p.TrangThai == "Chờ duyệt")
                            .ToList();

                        var activeCopies = activeLoans
                            .SelectMany(p => p.ChiTiets)
                            .Where(c => c.NgayTraThucTe == null)
                            .ToList();

                        currentBorrowedCount = activeCopies.Count;
                        hasUnreturnedBooks = currentBorrowedCount >= 3;
                        hasOverdueBooks = activeLoans.Any(p => p.TrangThai == "Đang mượn" && p.HanTra < DateTime.Today);
                        alreadyHoldingThisBook = activeCopies.Any(c => c.DauSach != null && c.DauSach.MaSach == id);
                    }
                }
            }

            var reviews = await _context.DanhGias
                .Where(r => r.MaSach == id)
                .OrderByDescending(r => r.NgayTao)
                .Select(r => new BookReviewViewModel
                {
                    UserName = r.TaiKhoan.TheThuVien != null ? r.TaiKhoan.TheThuVien.HoTen : r.TaiKhoan.Username,
                    Rating = r.SoSao,
                    Content = r.NoiDung,
                    CreatedAt = r.NgayTao
                }).ToListAsync();
            book.ReviewCount = reviews.Count;
            book.Rating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1);

            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                Copies = copies,
                Reviews = reviews,
                RelatedBooks = related,
                UserCardStatus = userCardStatus,
                HasUnreturnedBooks = hasUnreturnedBooks,
                HasOverdueBooks = hasOverdueBooks,
                AlreadyHoldingThisBook = alreadyHoldingThisBook,
                CurrentBorrowedCount = currentBorrowedCount,
                IsUserAuthenticated = isAuthenticated
            };

            return View(viewModel);
        }

        // POST: /Book/AddReview
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int bookId, int rating, string content)
        {
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(content) || content.Trim().Length < 10)
            {
                TempData["Error"] = "Đánh giá 1–5 sao và nội dung tối thiểu 10 ký tự.";
                return RedirectToAction(nameof(Details), new { id = bookId });
            }
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tk = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (tk == null) return RedirectToAction("Login", "Account");

            _context.DanhGias.Add(new DanhGia { MaSach = bookId, MaTK = tk.MaTK, SoSao = rating, NoiDung = content.Trim() });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã gửi đánh giá!";
            return RedirectToAction(nameof(Details), new { id = bookId });
        }
    }
}