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
            int? minRating,
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
                .AsNoTracking()
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

            var projectedQuery = query.Select(s => new BookItemViewModel
            {
                Id = s.MaSach,
                Title = s.TenSach,
                Author = s.TacGia,
                CategoryName = s.TheLoai.TenTheLoai,
                PublisherName = s.NhaXuatBan.TenNXB,
                PublishYear = s.NamXuatBan,
                ISBN = s.MaSachCode,
                AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                TotalCopies = s.DauSachs.Count(),
                BorrowCount = s.DauSachs.SelectMany(d => d.ChiTietPhieuMuons).Count(),
                ReviewCount = _context.DanhGias.Count(r => r.MaSach == s.MaSach),
                Rating = _context.DanhGias.Where(r => r.MaSach == s.MaSach).Select(r => (double?)r.SoSao).Average() ?? 0.0,
                Description = s.MoTa ?? "",
                PageCount = s.SoTrang > 0 ? s.SoTrang : 320,
                PreviewPageCount = s.TrangDocThus.Count(),
                ShelfLocation = "Kệ " + s.MaSachCode + " - Tầng 2",
                CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "/uploads/books/default-book.svg"
            });

            projectedQuery = sortBy switch
            {
                "title_asc" => projectedQuery.OrderBy(s => s.Title),
                "title_desc" => projectedQuery.OrderByDescending(s => s.Title),
                "year_asc" => projectedQuery.OrderBy(s => s.PublishYear),
                "popular" => projectedQuery.OrderByDescending(s => s.BorrowCount).ThenByDescending(s => s.PublishYear),
                "rating_desc" => projectedQuery.OrderByDescending(s => s.Rating).ThenByDescending(s => s.ReviewCount),
                _ => projectedQuery.OrderByDescending(s => s.PublishYear).ThenByDescending(s => s.Id)
            };

            var books = await projectedQuery.ToListAsync();
            foreach (var b in books)
            {
                b.Rating = Math.Round(b.Rating, 1);
            }

            if (minRating.HasValue && minRating.Value >= 1 && minRating.Value <= 5)
            {
                books = books.Where(b => b.ReviewCount > 0 && b.Rating >= minRating.Value).ToList();
            }

            var categories = await _context.TheLoais
                .AsNoTracking()
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
                MinRating = minRating,
                SortBy = sortBy ?? "newest",
                CurrentPage = currentPage,
                TotalPages = 1,
                TotalItems = books.Count,
                Categories = categories,
                Books = books
            };

            return View(viewModel);
        }

        // GET: /Book/WasmCatalog (Dữ liệu TSV siêu nhẹ cho động cơ Rust WebAssembly)
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> WasmCatalog()
        {
            var items = await _context.Sachs
                .AsNoTracking()
                .Select(s => new
                {
                    s.MaSach,
                    s.TenSach,
                    s.TacGia,
                    CategoryName = s.TheLoai.TenTheLoai,
                    s.MaSachCode,
                    s.NamXuatBan,
                    Available = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                    Total = s.DauSachs.Count(),
                    Cover = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "/uploads/books/default-book.svg"
                })
                .ToListAsync();

            var sb = new System.Text.StringBuilder(items.Count * 160);
            foreach (var b in items)
            {
                string Clean(string? val) => (val ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
                sb.Append(b.MaSach).Append('\t')
                  .Append(Clean(b.TenSach)).Append('\t')
                  .Append(Clean(b.TacGia)).Append('\t')
                  .Append(Clean(b.CategoryName)).Append('\t')
                  .Append(Clean(b.MaSachCode)).Append('\t')
                  .Append(b.NamXuatBan).Append('\t')
                  .Append(b.Available).Append('\t')
                  .Append(b.Total).Append('\t')
                  .Append(Clean(b.Cover)).Append('\t')
                  .Append("Kệ ").Append(Clean(b.MaSachCode)).Append(" - Tầng 2")
                  .Append('\n');
            }

            return Content(sb.ToString(), "text/plain; charset=utf-8");
        }

        // GET: /Book/Details/4
        public async Task<IActionResult> Details(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .Include(s => s.TrangDocThus)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                ViewBag.RequestedBookId = id;
                return View("NotFound");
            }

            var hasCustomPreview = sach.TrangDocThus != null && sach.TrangDocThus.Any();
            var previewPages = hasCustomPreview
                ? sach.TrangDocThus!
                    .OrderBy(t => t.SoTrang)
                    .ThenBy(t => t.MaTrangDocThu)
                    .Select(t => new BookPreviewPageViewModel
                    {
                        Id = t.MaTrangDocThu,
                        PageNumber = t.SoTrang,
                        Title = t.TieuDe,
                        Content = t.NoiDung
                    }).ToList()
                : BuildFallbackPreviewPages(sach);

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
                Description = !string.IsNullOrWhiteSpace(sach.MoTa)
                    ? sach.MoTa
                    : $"Tài liệu học thuật chuyên ngành {sach.TheLoai.TenTheLoai} do {sach.TacGia} biên soạn, xuất bản bởi {sach.NhaXuatBan.TenNXB} ({sach.NamXuatBan}).",
                PageCount = sach.SoTrang > 0 ? sach.SoTrang : 320,
                PreviewPageCount = previewPages.Count,
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
                    TotalCopies = s.DauSachs.Count,
                    Rating = _context.DanhGias.Where(r => r.MaSach == s.MaSach).Select(r => (double?)r.SoSao).Average() ?? 0.0,
                    ReviewCount = _context.DanhGias.Count(r => r.MaSach == s.MaSach)
                }).ToListAsync();

            // Trạng thái thẻ và quyền mượn của độc giả đang đăng nhập
            string userCardStatus = "Sử dụng được";
            bool hasUnreturnedBooks = false;
            bool hasOverdueBooks = false;
            bool alreadyHoldingThisBook = false;
            int currentBorrowedCount = 0;
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            int? currentUserAccountId = null;
            bool canModerateReviews = User.IsInRole("Admin") || User.IsInRole("Librarian") || User.IsInRole("ThuThu");

            if (isAuthenticated)
            {
                var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
                if (!string.IsNullOrEmpty(studentCode))
                {
                    var currentTk = await _context.TaiKhoans.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Username == studentCode);
                    currentUserAccountId = currentTk?.MaTK;

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

            var rawReviews = await _context.DanhGias
                .Include(r => r.TaiKhoan)
                    .ThenInclude(t => t.TheThuVien)
                .Where(r => r.MaSach == id)
                .OrderByDescending(r => r.NgayTao)
                .ToListAsync();

            var borrowerAccountIds = await _context.ChiTietPhieuMuons
                .Where(c => c.DauSach.MaSach == id && c.PhieuMuon.The != null)
                .Select(c => c.PhieuMuon.The.MaTK)
                .Distinct()
                .ToListAsync();
            var borrowerSet = borrowerAccountIds.ToHashSet();

            var reviews = rawReviews.Select(r =>
            {
                var displayName = r.TaiKhoan?.TheThuVien?.HoTen;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = !string.IsNullOrWhiteSpace(r.TaiKhoan?.FullName)
                        ? r.TaiKhoan.FullName
                        : (r.TaiKhoan?.Username ?? "Độc giả");
                }
                var roleLabel = r.TaiKhoan?.Role switch
                {
                    "Admin" => "Quản trị viên",
                    "Librarian" or "ThuThu" => "Thủ thư",
                    _ => "Sinh viên"
                };

                return new BookReviewViewModel
                {
                    Id = r.MaDanhGia,
                    UserName = displayName,
                    UserAvatarUrl = !string.IsNullOrEmpty(r.TaiKhoan?.AvatarUrl) ? r.TaiKhoan.AvatarUrl : "/uploads/avatars/default-avatar.svg",
                    Rating = r.SoSao,
                    Content = r.NoiDung,
                    CreatedAt = r.NgayTao,
                    UserRole = roleLabel,
                    IsOwner = currentUserAccountId.HasValue && r.MaTK == currentUserAccountId.Value,
                    IsVerifiedBorrower = borrowerSet.Contains(r.MaTK)
                };
            }).ToList();

            var distribution = new Dictionary<int, int>
            {
                [5] = reviews.Count(r => r.Rating == 5),
                [4] = reviews.Count(r => r.Rating == 4),
                [3] = reviews.Count(r => r.Rating == 3),
                [2] = reviews.Count(r => r.Rating == 2),
                [1] = reviews.Count(r => r.Rating == 1)
            };

            var ownReview = currentUserAccountId.HasValue
                ? rawReviews.FirstOrDefault(r => r.MaTK == currentUserAccountId.Value)
                : null;

            book.ReviewCount = reviews.Count;
            book.Rating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1);

            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                Copies = copies,
                Reviews = reviews,
                PreviewPages = previewPages,
                HasCustomPreviewPages = hasCustomPreview,
                RatingDistribution = distribution,
                CurrentUserRating = ownReview?.SoSao,
                CurrentUserReviewContent = ownReview?.NoiDung,
                CurrentUserReviewId = ownReview?.MaDanhGia,
                CanModerateReviews = canModerateReviews,
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

        // GET: /Book/Preview/4
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var sach = await _context.Sachs
                .AsNoTracking()
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .Include(s => s.TrangDocThus)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tựa sách." });
            }

            var hasCustom = sach.TrangDocThus != null && sach.TrangDocThus.Any();
            var pages = hasCustom
                ? sach.TrangDocThus!
                    .OrderBy(t => t.SoTrang)
                    .ThenBy(t => t.MaTrangDocThu)
                    .Select(t => new BookPreviewPageViewModel
                    {
                        Id = t.MaTrangDocThu,
                        PageNumber = t.SoTrang,
                        Title = t.TieuDe,
                        Content = t.NoiDung
                    }).ToList()
                : BuildFallbackPreviewPages(sach);

            return Json(new
            {
                success = true,
                bookId = sach.MaSach,
                title = sach.TenSach,
                author = sach.TacGia,
                categoryName = sach.TheLoai.TenTheLoai,
                publisherName = sach.NhaXuatBan.TenNXB,
                publishYear = sach.NamXuatBan,
                bookCode = sach.MaSachCode,
                coverImageUrl = !string.IsNullOrEmpty(sach.HinhAnh) ? sach.HinhAnh : "/uploads/books/default-book.svg",
                totalBookPages = sach.SoTrang > 0 ? sach.SoTrang : 320,
                previewCount = pages.Count,
                hasCustomPreview = hasCustom,
                isAvailable = sach.DauSachs.Any(d => d.TinhTrang == "Còn"),
                pages = pages.Select(p => new
                {
                    id = p.Id,
                    pageNumber = p.PageNumber,
                    title = p.Title,
                    content = p.Content
                })
            });
        }

        // POST: /Book/AddReview
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int bookId, int rating, string? content)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json");

            var bookExists = await _context.Sachs.AnyAsync(s => s.MaSach == bookId);
            if (!bookExists)
            {
                if (isAjax) return NotFound(new { success = false, message = "Không tìm thấy tựa sách để đánh giá." });
                TempData["Error"] = "Không tìm thấy tựa sách để đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            var cleanContent = content?.Trim() ?? "";
            if (rating < 1 || rating > 5)
            {
                const string msg = "Vui lòng chọn mức đánh giá từ 1 đến 5 sao.";
                if (isAjax) return BadRequest(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Details), new { id = bookId });
            }

            if (cleanContent.Length > 1000)
            {
                const string msg = "Nội dung nhận xét không được vượt quá 1000 ký tự.";
                if (isAjax) return BadRequest(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Details), new { id = bookId });
            }

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
            var tk = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (tk == null)
            {
                if (isAjax) return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
                return RedirectToAction("Login", "Account");
            }

            var existing = await _context.DanhGias
                .FirstOrDefaultAsync(d => d.MaSach == bookId && d.MaTK == tk.MaTK);

            bool isUpdated = existing != null;
            if (existing != null)
            {
                existing.SoSao = rating;
                existing.NoiDung = cleanContent;
                existing.NgayTao = DateTime.Now;
            }
            else
            {
                existing = new DanhGia
                {
                    MaSach = bookId,
                    MaTK = tk.MaTK,
                    SoSao = rating,
                    NoiDung = cleanContent,
                    NgayTao = DateTime.Now
                };
                _context.DanhGias.Add(existing);
            }

            await _context.SaveChangesAsync();

            var successMessage = isUpdated
                ? $"Đã cập nhật đánh giá của bạn thành {rating} sao!"
                : $"Cảm ơn bạn đã đánh giá {rating} sao cho cuốn sách này!";

            if (isAjax)
            {
                var summary = await BuildReviewSummaryPayloadAsync(bookId, tk.MaTK);
                return Json(new
                {
                    success = true,
                    message = successMessage,
                    isUpdated,
                    reviewId = existing.MaDanhGia,
                    summary
                });
            }

            TempData["Success"] = successMessage;
            return RedirectToAction(nameof(Details), "Book", new { id = bookId }, "reviews");
        }

        // POST: /Book/DeleteReview
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId, int bookId)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json");

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
            var tk = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (tk == null)
            {
                if (isAjax) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập lại." });
                return RedirectToAction("Login", "Account");
            }

            var review = await _context.DanhGias.FirstOrDefaultAsync(d => d.MaDanhGia == reviewId && d.MaSach == bookId);
            if (review == null)
            {
                const string msg = "Không tìm thấy đánh giá cần xóa.";
                if (isAjax) return NotFound(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Details), "Book", new { id = bookId }, "reviews");
            }

            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian") || User.IsInRole("ThuThu");
            if (review.MaTK != tk.MaTK && !isStaff)
            {
                const string msg = "Bạn không có quyền xóa đánh giá của người dùng khác.";
                if (isAjax) return StatusCode(403, new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Details), "Book", new { id = bookId }, "reviews");
            }

            _context.DanhGias.Remove(review);
            await _context.SaveChangesAsync();

            const string okMsg = "Đã xóa đánh giá thành công.";
            if (isAjax)
            {
                var summary = await BuildReviewSummaryPayloadAsync(bookId, tk.MaTK);
                return Json(new { success = true, message = okMsg, summary });
            }

            TempData["Success"] = okMsg;
            return RedirectToAction(nameof(Details), "Book", new { id = bookId }, "reviews");
        }

        private async Task<object> BuildReviewSummaryPayloadAsync(int bookId, int? currentUserAccountId)
        {
            var rawReviews = await _context.DanhGias
                .Include(r => r.TaiKhoan)
                    .ThenInclude(t => t.TheThuVien)
                .Where(r => r.MaSach == bookId)
                .OrderByDescending(r => r.NgayTao)
                .ToListAsync();

            var borrowerAccountIds = await _context.ChiTietPhieuMuons
                .Where(c => c.DauSach.MaSach == bookId && c.PhieuMuon.The != null)
                .Select(c => c.PhieuMuon.The.MaTK)
                .Distinct()
                .ToListAsync();
            var borrowerSet = borrowerAccountIds.ToHashSet();

            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian") || User.IsInRole("ThuThu");

            var reviews = rawReviews.Select(r =>
            {
                var displayName = r.TaiKhoan?.TheThuVien?.HoTen;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = !string.IsNullOrWhiteSpace(r.TaiKhoan?.FullName)
                        ? r.TaiKhoan.FullName
                        : (r.TaiKhoan?.Username ?? "Độc giả");
                }
                var roleLabel = r.TaiKhoan?.Role switch
                {
                    "Admin" => "Quản trị viên",
                    "Librarian" or "ThuThu" => "Thủ thư",
                    _ => "Sinh viên"
                };

                bool isOwner = currentUserAccountId.HasValue && r.MaTK == currentUserAccountId.Value;

                return new
                {
                    id = r.MaDanhGia,
                    userName = displayName,
                    userAvatarUrl = !string.IsNullOrEmpty(r.TaiKhoan?.AvatarUrl) ? r.TaiKhoan.AvatarUrl : "/uploads/avatars/default-avatar.svg",
                    rating = r.SoSao,
                    content = r.NoiDung,
                    createdAt = r.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                    userRole = roleLabel,
                    isOwner,
                    canDelete = isOwner || isStaff,
                    isVerifiedBorrower = borrowerSet.Contains(r.MaTK)
                };
            }).ToList();

            var count = reviews.Count;
            var average = count == 0 ? 0.0 : Math.Round(reviews.Average(r => r.rating), 1);
            var distribution = new Dictionary<string, int>
            {
                ["5"] = reviews.Count(r => r.rating == 5),
                ["4"] = reviews.Count(r => r.rating == 4),
                ["3"] = reviews.Count(r => r.rating == 3),
                ["2"] = reviews.Count(r => r.rating == 2),
                ["1"] = reviews.Count(r => r.rating == 1)
            };

            var own = currentUserAccountId.HasValue
                ? rawReviews.FirstOrDefault(r => r.MaTK == currentUserAccountId.Value)
                : null;

            return new
            {
                averageRating = average,
                reviewCount = count,
                distribution,
                currentUserRating = own?.SoSao,
                currentUserContent = own?.NoiDung ?? "",
                currentUserReviewId = own?.MaDanhGia,
                reviews
            };
        }

        private static List<BookPreviewPageViewModel> BuildFallbackPreviewPages(Sach sach)
        {
            var category = sach.TheLoai?.TenTheLoai ?? "Chuyên ngành";
            var publisher = sach.NhaXuatBan?.TenNXB ?? "Thư viện HUFLIT";
            var synopsis = !string.IsNullOrWhiteSpace(sach.MoTa)
                ? sach.MoTa
                : $"Tài liệu học thuật chuyên ngành {category} được lưu trữ tại Thư viện Trường Đại học Ngoại Ngữ - Tin Học TP.HCM (HUFLIT).";

            return new List<BookPreviewPageViewModel>
            {
                new()
                {
                    PageNumber = 1,
                    Title = "Thông tin xuất bản & Giới thiệu chung",
                    Content = $"TỰA SÁCH: {sach.TenSach}\nTác giả: {sach.TacGia}\nNhà xuất bản: {publisher} ({sach.NamXuatBan})\nMã tài liệu: {sach.MaSachCode}\nChuyên ngành: {category}\n\n{synopsis}"
                },
                new()
                {
                    PageNumber = 2,
                    Title = "Cấu trúc nội dung & Mục tiêu học phần",
                    Content = $"Tài liệu \"{sach.TenSach}\" cung cấp hệ thống kiến thức nền tảng và các bài tập thực hành bám sát chương trình đào tạo chuyên ngành {category}.\n\nNội dung chính gồm:\n• Phần I: Tổng quan lý thuyết và khái niệm cốt lõi\n• Phần II: Phân tích tình huống & phương pháp triển khai\n• Phần III: Câu hỏi ôn tập và bài tập vận dụng thực tế."
                },
                new()
                {
                    PageNumber = 3,
                    Title = "Trích đoạn mở đầu (Chương 1)",
                    Content = $"Chương 1 trình bày bức tranh tổng quát về chủ đề của cuốn sách \"{sach.TenSach}\", giúp sinh viên nắm vững tư duy nền tảng trước khi đi sâu vào các kỹ thuật chuyên sâu.\n\nĐể đọc trọn vẹn toàn bộ {(sach.SoTrang > 0 ? sach.SoTrang : 320)} trang của tài liệu này, bạn có thể đặt mượn trực tuyến và nhận sách tại Quầy Lưu Thông Tầng 1 - Cơ sở Hóc Môn."
                }
            };
        }
    }
}