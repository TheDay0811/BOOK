using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.ViewModels;

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
        public async Task<IActionResult> Index(string? searchQuery, List<int>? selectedCategoryIds, string? availabilityFilter, string? sortBy, int currentPage = 1)
        {
            var query = _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLower();
                query = query.Where(s => s.TenSach.ToLower().Contains(q) || s.TacGia.ToLower().Contains(q));
            }

            if (selectedCategoryIds != null && selectedCategoryIds.Count > 0)
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
                ISBN = s.Isbn,
                CoverImageUrl = s.AnhBiaUrl,
                AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                TotalCopies = s.DauSachs.Count
            }).ToList();

            var categories = await _context.TheLoais
                .Select(tl => new SelectListItem
                {
                    Value = tl.MaTheLoai.ToString(),
                    Text = $"{tl.TenTheLoai} ({tl.Sachs.Count})"
                }).ToListAsync();

            var viewModel = new BookListViewModel
            {
                SearchQuery = searchQuery,
                SelectedCategoryIds = selectedCategoryIds ?? new List<int>(),
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

        // GET: /Book/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                return NotFound();
            }

            var book = new BookItemViewModel
            {
                Id = sach.MaSach,
                Title = sach.TenSach,
                Author = sach.TacGia,
                CategoryName = sach.TheLoai.TenTheLoai,
                PublisherName = sach.NhaXuatBan.TenNXB,
                PublishYear = sach.NamXuatBan,
                ISBN = sach.Isbn,
                CoverImageUrl = sach.AnhBiaUrl,
                AvailableCopies = sach.DauSachs.Count(d => d.TinhTrang == "Còn"),
                TotalCopies = sach.DauSachs.Count
            };

            var related = await _context.Sachs
                .Where(s => s.MaTheLoai == sach.MaTheLoai && s.MaSach != sach.MaSach)
                .Include(s => s.DauSachs)
                .Take(3)
                .Select(s => new BookItemViewModel
                {
                    Id = s.MaSach,
                    Title = s.TenSach,
                    Author = s.TacGia,
                    AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                    TotalCopies = s.DauSachs.Count
                }).ToListAsync();

            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                Reviews = new List<BookReviewViewModel>(), // chưa có bảng review trong ERD
                RelatedBooks = related
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