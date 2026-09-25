using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
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
    }
}