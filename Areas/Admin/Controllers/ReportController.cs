using BOOK.Data;
using BOOK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
    [Authorize(Roles = "Admin,ThuThu")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Report/DamagedLostBooks
        public async Task<IActionResult> DamagedLostBooks(int? year, string? condition)
        {
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
                    BookCode = d.Sach.MaSachCode,
                    BookTitle = d.Sach.TenSach,
                    CategoryName = d.Sach.TheLoai.TenTheLoai,
                    ControlNumber = d.SoKiemSoat,
                    ConditionStatus = d.TinhTrang,
                    RecordedDate = lastChiTiet?.NgayTraThucTe ?? DateTime.Today,
                    Notes = d.GhiChu,
                    StudentName = lastChiTiet?.PhieuMuon.The.HoTen ?? "(Không rõ)",
                    StudentCode = lastChiTiet?.PhieuMuon.The.TaiKhoan.Username ?? "",
                    CompensationStatus = (lastChiTiet != null && lastChiTiet.DaBoiThuong) ? "Đã bồi thường" : "Chưa bồi thường",
                    CompensationAmount = lastChiTiet?.TienBoiThuong ?? 0,
                    LiquidationStatus = "Đề xuất thanh lý"
                };
            })
            .Where(r => (year == null) || r.RecordedDate.Year == year)
            .ToList();

            var model = new DamagedLostReportViewModel
            {
                SelectedYear = year ?? DateTime.Today.Year,
                SelectedCondition = conditionFilter,
                Records = records
            };

            return View(model);
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