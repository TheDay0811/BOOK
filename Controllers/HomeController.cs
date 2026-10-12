using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BOOK.Data;
using BOOK.Models;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: /Home/Index
        public async Task<IActionResult> Index()
        {
            var rawCategories = await _context.TheLoais
                .Include(tl => tl.Sachs)
                .ToListAsync();

            var categories = rawCategories.Select(tl =>
            {
                var name = tl.TenTheLoai;
                string icon = "book-open";
                string color = "text-navy-800 bg-blue-50";

                if (name.Contains("Công nghệ", StringComparison.OrdinalIgnoreCase))
                {
                    icon = "code-2";
                    color = "text-navy-800 bg-blue-50";
                }
                else if (name.Contains("Trí tuệ", StringComparison.OrdinalIgnoreCase) || name.Contains("Dữ liệu", StringComparison.OrdinalIgnoreCase))
                {
                    icon = "brain-circuit";
                    color = "text-emerald-700 bg-emerald-50";
                }
                else if (name.Contains("Kinh tế", StringComparison.OrdinalIgnoreCase))
                {
                    icon = "trending-up";
                    color = "text-amber-700 bg-amber-50";
                }
                else if (name.Contains("An toàn", StringComparison.OrdinalIgnoreCase) || name.Contains("Mật mã", StringComparison.OrdinalIgnoreCase))
                {
                    icon = "shield-check";
                    color = "text-purple-700 bg-purple-50";
                }
                else if (name.Contains("Ngoại ngữ", StringComparison.OrdinalIgnoreCase))
                {
                    icon = "languages";
                    color = "text-rose-700 bg-rose-50";
                }
                else if (name.Contains("Văn học", StringComparison.OrdinalIgnoreCase) || name.Contains("Xã hội", StringComparison.OrdinalIgnoreCase))
                {
                    icon = "book-heart";
                    color = "text-cyan-700 bg-cyan-50";
                }

                return new CategoryItemViewModel
                {
                    Id = tl.MaTheLoai,
                    Name = tl.TenTheLoai,
                    BookCount = tl.Sachs.Count,
                    IconName = icon,
                    ColorClass = color
                };
            }).ToList();

            // Danh sách sách kèm quan hệ
            var allSachsWithRelations = await _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                    .ThenInclude(d => d.ChiTietPhieuMuons)
                .ToListAsync();

            // Top sách nổi bật & được mượn nhiều nhất
            var popularBooks = allSachsWithRelations
                .OrderByDescending(s => s.DauSachs.SelectMany(d => d.ChiTietPhieuMuons).Count())
                .ThenByDescending(s => s.DauSachs.Count)
                .ThenByDescending(s => s.NamXuatBan)
                .Take(8)
                .Select(s => new BookItemViewModel
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
                    BorrowCount = s.DauSachs.SelectMany(d => d.ChiTietPhieuMuons).Count(),
                    ShelfLocation = $"Kệ {s.MaSachCode} - Tầng 2",
                    CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400&auto=format&fit=crop&q=80"
                }).ToList();

            // Sách mới tiếp nhận
            var newBooks = allSachsWithRelations
                .OrderByDescending(s => s.NamXuatBan)
                .ThenByDescending(s => s.MaSach)
                .Take(4)
                .Select(s => new BookItemViewModel
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
                    BorrowCount = s.DauSachs.SelectMany(d => d.ChiTietPhieuMuons).Count(),
                    ShelfLocation = $"Kệ {s.MaSachCode} - Tầng 2",
                    CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400&auto=format&fit=crop&q=80"
                }).ToList();

            var viewModel = new HomeIndexViewModel
            {
                Categories = categories,
                NewBooks = newBooks,
                PopularBooks = popularBooks,
                TotalBooksCount = await _context.Sachs.CountAsync(),
                TotalCopiesCount = await _context.DauSachs.CountAsync()
            };

            return View(viewModel);
        }

        // GET: /Home/Rules (Nội quy & Quy định mượn trả)
        public IActionResult Rules()
        {
            return View();
        }

        // GET: /Home/Privacy (Chính sách bảo mật)
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /Home/Terms (Điều khoản sử dụng)
        public IActionResult Terms()
        {
            return View();
        }

        // GET: /Home/Map (Sơ đồ thư viện)
        public IActionResult Map()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
