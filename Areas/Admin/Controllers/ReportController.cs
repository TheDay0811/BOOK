using BOOK.Data;
using BOOK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BOOK.Areas.Admin.Controllers
{
    public class BorrowStatsViewModel
    {
        public int Year { get; set; }
        public List<(int Month, int Borrowed, int Returned)> Months { get; set; } = new();
        public List<(string Title, int Count)> TopBooks { get; set; } = new();
        public int OverdueNow { get; set; }
        public decimal UnpaidFines { get; set; }
    }
    [Area("Admin")]
    [Authorize(Roles = "Admin,Librarian,ThuThu")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Report/Index
        public async Task<IActionResult> Index(string tab = "overview", int? year = null, string? condition = null)
        {
            var totalBorrows = await _context.PhieuMuons.CountAsync();
            var activeBorrows = await _context.PhieuMuons.CountAsync(p => p.TrangThai == "Đang mượn");
            var overdueBorrows = await _context.PhieuMuons.CountAsync(p => p.TrangThai == "Đang mượn" && p.HanTra < DateTime.Today);
            var compensations = await _context.ChiTietPhieuMuons
                .Where(c => c.DaBoiThuong)
                .SumAsync(c => c.TienBoiThuong);

            // GroupBy ChiTietPhieuMuon để tìm Top 5 sách mượn nhiều nhất
            var topBooks = await _context.ChiTietPhieuMuons
                .Include(c => c.DauSach)
                    .ThenInclude(d => d.Sach)
                        .ThenInclude(s => s.TheLoai)
                .Where(c => c.DauSach != null && c.DauSach.Sach != null)
                .GroupBy(c => c.DauSach.Sach)
                .Select(g => new TopBorrowedBookViewModel
                {
                    BookId = g.Key.MaSach,
                    Title = g.Key.TenSach,
                    BookCode = g.Key.MaSachCode,
                    CategoryName = g.Key.TheLoai.TenTheLoai,
                    BorrowCount = g.Count(),
                    TotalCopies = g.Key.DauSachs.Count
                })
                .OrderByDescending(b => b.BorrowCount)
                .Take(5)
                .ToListAsync();

            var analyticsModel = new BorrowAnalyticsReportViewModel
            {
                TotalBorrows = totalBorrows,
                ActiveBorrows = activeBorrows,
                OverdueBorrows = overdueBorrows,
                TotalCompensations = compensations,
                TopBooks = topBooks
            };

            // Dữ liệu Sách hỏng / mất & Thanh lý
            var conditionFilter = condition ?? "Tất cả";
            var query = _context.DauSachs
                .Include(d => d.Sach).ThenInclude(s => s.TheLoai)
                .Include(d => d.ChiTietPhieuMuons)
                    .ThenInclude(c => c.PhieuMuon)
                        .ThenInclude(p => p.The)
                            .ThenInclude(t => t.TaiKhoan)
                .Where(d => d.TinhTrang == "Hỏng" || d.TinhTrang == "Mất")
                .AsQueryable();

            if (conditionFilter == "Chỉ mất")
            {
                query = query.Where(d => d.TinhTrang == "Mất");
            }
            else if (conditionFilter == "Chỉ hỏng")
            {
                query = query.Where(d => d.TinhTrang == "Hỏng");
            }

            var list = await query.ToListAsync();

            var records = list.Select(d =>
            {
                var lastChiTiet = d.ChiTietPhieuMuons
                    .Where(c => c.TinhTrangTra == d.TinhTrang)
                    .OrderByDescending(c => c.NgayTraThucTe)
                    .FirstOrDefault();

                return new DamagedLostBookRecordViewModel
                {
                    Id = d.MaDauSach,
                    ChiTietId = lastChiTiet?.MaChiTiet ?? 0,
                    CardId = lastChiTiet?.PhieuMuon?.The?.MaThe ?? 0,
                    BookCode = d.Sach.MaSachCode,
                    BookTitle = d.Sach.TenSach,
                    CategoryName = d.Sach.TheLoai.TenTheLoai,
                    ControlNumber = d.SoKiemSoat,
                    ConditionStatus = d.TinhTrang,
                    RecordedDate = lastChiTiet?.NgayTraThucTe ?? DateTime.Today,
                    Notes = d.GhiChu,
                    StudentName = lastChiTiet?.PhieuMuon.The.HoTen ?? "(Chưa có người mượn)",
                    StudentCode = lastChiTiet?.PhieuMuon.The.TaiKhoan.Username ?? "",
                    CompensationStatus = (lastChiTiet != null && lastChiTiet.DaBoiThuong) ? "Đã bồi thường" : "Chưa bồi thường",
                    CompensationAmount = lastChiTiet?.TienBoiThuong ?? (d.TinhTrang == "Hỏng" ? 50000 : 100000),
                    LiquidationStatus = "Đề xuất thanh lý"
                };
            })
            .Where(r => (year == null) || r.RecordedDate.Year == year)
            .ToList();

            var damagedLostModel = new DamagedLostReportViewModel
            {
                SelectedYear = year ?? DateTime.Today.Year,
                SelectedCondition = conditionFilter,
                Records = records
            };

            var viewModel = new UnifiedReportViewModel
            {
                Analytics = analyticsModel,
                DamagedLost = damagedLostModel,
                ActiveTab = tab
            };

            return View(viewModel);
        }

        // GET: /Admin/Report/DamagedLostBooks -> Chuyển hướng sang Tab Sách Hỏng ở Index
        public IActionResult DamagedLostBooks(int? year, string? condition)
        {
            return RedirectToAction(nameof(Index), new { tab = "damaged", year, condition });
        }

        // POST: /Admin/Report/ConfirmCompensation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCompensation(int chiTietId)
        {
            var chiTiet = await _context.ChiTietPhieuMuons
                .Include(c => c.PhieuMuon)
                    .ThenInclude(p => p.The)
                .FirstOrDefaultAsync(c => c.MaChiTiet == chiTietId);

            if (chiTiet == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin lượt mượn hỏng/mất này.";
                return RedirectToAction(nameof(Index), new { tab = "damaged" });
            }

            chiTiet.DaBoiThuong = true;
            if (chiTiet.TienBoiThuong == 0)
            {
                chiTiet.TienBoiThuong = chiTiet.TinhTrangTra == "Hỏng" ? 50000 : 100000;
            }

            var card = chiTiet.PhieuMuon.The;
            var hasOtherUncompensated = await _context.ChiTietPhieuMuons
                .AnyAsync(c => c.MaChiTiet != chiTietId &&
                               c.PhieuMuon.MaThe == card.MaThe &&
                               (c.TinhTrangTra == "Mất" || c.TinhTrangTra == "Hỏng") &&
                               !c.DaBoiThuong);

            if (!hasOtherUncompensated && card.NgayHetHan >= DateTime.Today)
            {
                card.TinhTrangSuDung = "Sử dụng được";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã ghi nhận bồi thường {chiTiet.TienBoiThuong:N0} đ từ độc giả {card.HoTen} ({card.SoThe}). Thẻ thư viện: '{card.TinhTrangSuDung}'!";
            return RedirectToAction(nameof(Index), new { tab = "damaged" });
        }

        // GET: /Admin/Report/Settings (Cấu hình hệ thống quy chế - Chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        public IActionResult Settings()
        {
            return View();
        }
        // GET: /Admin/Report/BorrowStats
        public async Task<IActionResult> BorrowStats(int? year)
        {
            var y = year ?? DateTime.Today.Year;

            var borrowed = await _context.ChiTietPhieuMuons.Where(c => c.PhieuMuon.NgayMuon.Year == y)
                .GroupBy(c => c.PhieuMuon.NgayMuon.Month).Select(g => new { M = g.Key, N = g.Count() }).ToListAsync();
            var returned = await _context.ChiTietPhieuMuons.Where(c => c.NgayTraThucTe != null && c.NgayTraThucTe.Value.Year == y)
                .GroupBy(c => c.NgayTraThucTe!.Value.Month).Select(g => new { M = g.Key, N = g.Count() }).ToListAsync();
            var top = await _context.ChiTietPhieuMuons.Where(c => c.PhieuMuon.NgayMuon.Year == y)
                .GroupBy(c => c.DauSach.Sach.TenSach).Select(g => new { T = g.Key, N = g.Count() })
                .OrderByDescending(x => x.N).Take(10).ToListAsync();

            var model = new BorrowStatsViewModel
            {
                Year = y,
                Months = Enumerable.Range(1, 12).Select(m => (m,
                    borrowed.FirstOrDefault(x => x.M == m)?.N ?? 0,
                    returned.FirstOrDefault(x => x.M == m)?.N ?? 0)).ToList(),
                TopBooks = top.Select(x => (x.T, x.N)).ToList(),
                OverdueNow = await _context.PhieuMuons.CountAsync(p => p.TrangThai == "Đang mượn" && p.HanTra < DateTime.Today),
                UnpaidFines = await _context.ChiTietPhieuMuons.Where(c => !c.DaBoiThuong).SumAsync(c => (decimal?)c.TienBoiThuong) ?? 0
            };
            return View(model);
        }
    }
}