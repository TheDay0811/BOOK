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
using BOOK.Services;
using MongoDB.Driver;

namespace BOOK.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ILibraryMongoLogService? _mongoLog;
        private readonly IMongoDatabase? _mongoDb;

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            ILibraryMongoLogService? mongoLog = null,
            IMongoDatabase? mongoDb = null)
        {
            _logger = logger;
            _context = context;
            _mongoLog = mongoLog;
            _mongoDb = mongoDb;
        }

        // GET: /Home/Index
        public async Task<IActionResult> Index()
        {
            var categoriesData = await _context.TheLoais
                .AsNoTracking()
                .Select(tl => new
                {
                    tl.MaTheLoai,
                    tl.TenTheLoai,
                    BookCount = tl.Sachs.Count()
                })
                .ToListAsync();

            var categories = categoriesData.Select(tl =>
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
                    BookCount = tl.BookCount,
                    IconName = icon,
                    ColorClass = color
                };
            }).ToList();

            // Truy vấn chiếu (SQL Projection) tối ưu hóa: Tính toán trực tiếp trên SQL Server với AsNoTracking
            var projectedBooks = await _context.Sachs
                .AsNoTracking()
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
                    TotalCopies = s.DauSachs.Count(),
                    BorrowCount = s.DauSachs.SelectMany(d => d.ChiTietPhieuMuons).Count(),
                    ReviewCount = _context.DanhGias.Count(r => r.MaSach == s.MaSach),
                    Rating = _context.DanhGias.Where(r => r.MaSach == s.MaSach).Select(r => (double?)r.SoSao).Average() ?? 0.0,
                    PreviewPageCount = s.TrangDocThus.Count(),
                    ShelfLocation = "Kệ " + s.MaSachCode + " - Tầng 2",
                    CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400&auto=format&fit=crop&q=80"
                })
                .ToListAsync();

            foreach (var b in projectedBooks)
            {
                b.Rating = Math.Round(b.Rating, 1);
            }

            var popularBooks = projectedBooks
                .OrderByDescending(b => b.BorrowCount)
                .ThenByDescending(b => b.TotalCopies)
                .ThenByDescending(b => b.PublishYear)
                .Take(8)
                .ToList();

            var newBooks = projectedBooks
                .OrderByDescending(b => b.PublishYear)
                .ThenByDescending(b => b.Id)
                .Take(4)
                .ToList();

            var viewModel = new HomeIndexViewModel
            {
                Categories = categories,
                NewBooks = newBooks,
                PopularBooks = popularBooks,
                TotalBooksCount = projectedBooks.Count,
                TotalCopiesCount = projectedBooks.Sum(b => b.TotalCopies)
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

        // GET: /Home/MongoStatus (Trang kiểm tra trạng thái MongoDB thời gian thực)
        public async Task<IActionResult> MongoStatus()
        {
            var auditLogs = _mongoLog != null ? await _mongoLog.GetRecentLogsAsync(20) : new();
            var chatLogs = _mongoLog != null ? await _mongoLog.GetRecentChatLogsAsync(20) : new();

            ViewBag.AuditLogs = auditLogs;
            ViewBag.ChatLogs = chatLogs;
            ViewBag.DatabaseName = _mongoDb?.DatabaseNamespace.DatabaseName ?? "LibraryDB";
            ViewBag.ConnectionString = MongoRunnerService.ActiveConnectionString;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
