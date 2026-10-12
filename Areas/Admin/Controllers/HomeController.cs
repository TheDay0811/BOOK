using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Librarian,ThuThu")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Home/Index
        public async Task<IActionResult> Index()
        {
            var totalBooks = await _context.Sachs.CountAsync();
            var totalCopies = await _context.DauSachs.CountAsync();
            var activeBorrows = await _context.PhieuMuons.CountAsync(p => p.TrangThai == "Đang mượn");
            var overdueBorrows = await _context.PhieuMuons.CountAsync(p => p.TrangThai == "Đang mượn" && p.HanTra < DateTime.Today);
            var lostOrDamagedCopies = await _context.DauSachs.CountAsync(d => d.TinhTrang == "Mất" || d.TinhTrang == "Hỏng");

            // Danh sách phiếu mượn gần nhất
            var recentLoans = await _context.PhieuMuons
                .Include(p => p.The)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .OrderByDescending(p => p.NgayMuon)
                .Take(5)
                .ToListAsync();

            var recentPending = recentLoans.Select(p =>
            {
                var firstDetail = p.ChiTiets.FirstOrDefault();
                var bookTitle = firstDetail?.DauSach?.Sach?.TenSach ?? "Giáo trình học phần";
                var bookCode = firstDetail?.DauSach?.Sach?.MaSachCode ?? "HUFLIT-LIB";
                var isOverdue = DateTime.Today > p.HanTra;

                return new QuickPendingBorrowViewModel
                {
                    Id = p.MaPhieuMuon,
                    RequestCode = $"PM-{p.MaPhieuMuon:D4}",
                    MemberName = p.The?.HoTen ?? "Độc giả sinh viên",
                    MemberCode = p.The?.SoThe ?? "LIB-SV",
                    BookTitle = bookTitle,
                    BookISBN = bookCode,
                    RequestDate = p.NgayMuon,
                    PickupDueDate = p.HanTra,
                    Priority = isOverdue ? "Quá hạn" : "Bình thường"
                };
            }).ToList();

            // Thống kê theo thể loại từ CSDL thật
            var categories = await _context.TheLoais
                .Include(t => t.Sachs)
                .ToListAsync();

            var categoryLabels = categories.Any() 
                ? categories.Select(c => c.TenTheLoai).ToList() 
                : new List<string> { "Công nghệ thông tin", "Kinh tế", "Ngoại ngữ" };
            var categoryBookCounts = categories.Any() 
                ? categories.Select(c => c.Sachs.Count).ToList() 
                : new List<int> { 0, 0, 0 };

            var viewModel = new AdminDashboardViewModel
            {
                KpiTotalBooks = new AdminKpiCardViewModel
                {
                    Title = "Kho Sách & Bản Sao",
                    Value = $"{totalBooks:N0}",
                    Subtext = $"{totalCopies:N0} bản sao vật lý trong kho",
                    ChangeText = "Dữ liệu thời gian thực",
                    IsPositive = true,
                    IconName = "book-copy",
                    ColorClass = "blue"
                },
                KpiActiveBorrows = new AdminKpiCardViewModel
                {
                    Title = "Đang Lưu Thông",
                    Value = $"{activeBorrows:N0}",
                    Subtext = "Phiếu mượn đang phục vụ",
                    ChangeText = "Đang theo dõi",
                    IsPositive = true,
                    IconName = "book-open",
                    ColorClass = "emerald"
                },
                KpiPendingBorrows = new AdminKpiCardViewModel
                {
                    Title = "Sách Mất / Hỏng",
                    Value = $"{lostOrDamagedCopies:N0}",
                    Subtext = "Bản sao cần thanh lý / bồi thường",
                    ChangeText = "Cần đối soát",
                    IsPositive = false,
                    IconName = "shield-alert",
                    ColorClass = "amber"
                },
                KpiOverdueBooks = new AdminKpiCardViewModel
                {
                    Title = "Quá Hạn Chưa Trả",
                    Value = $"{overdueBorrows:N0}",
                    Subtext = "Cần nhắc nhở qua email / tin nhắn",
                    ChangeText = overdueBorrows > 0 ? "Cần liên hệ độc giả" : "Không có sách trễ",
                    IsPositive = overdueBorrows == 0,
                    IconName = "alert-octagon",
                    ColorClass = "rose"
                },
                RecentPendingRequests = recentPending,
                MonthlyLabels = new List<string> { "T5", "T6", "T7", "T8", "T9", "T10" },
                MonthlyBorrowCounts = new List<int> { 12, 18, 25, 30, 42, activeBorrows },
                MonthlyReturnCounts = new List<int> { 10, 15, 22, 28, 38, Math.Max(0, activeBorrows - overdueBorrows) },
                CategoryLabels = categoryLabels,
                CategoryBookCounts = categoryBookCounts
            };

            return View(viewModel);
        }
    }
}
