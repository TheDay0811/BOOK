using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Borrow/Index
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var overdue = await _context.PhieuMuons
                .Include(p => p.The)
                .Where(p => p.TrangThai == "Đang mượn" && p.HanTra < today)
                .Select(p => new OverdueAlertViewModel
                {
                    BorrowId = p.MaPhieuMuon,
                    MemberName = p.The.HoTen,
                    MemberCode = p.The.SoThe,
                    DueDate = p.HanTra,
                    DaysOverdue = EF.Functions.DateDiffDay(p.HanTra, today)
                }).ToListAsync();

            var todayReturns = await _context.ChiTietPhieuMuons
                .CountAsync(c => c.NgayTraThucTe != null && c.NgayTraThucTe.Value.Date == today);

            var viewModel = new LibrarianDashboardViewModel
            {
                TodayReturnsCount = todayReturns,
                PendingRequests = new List<PendingBorrowRequestViewModel>(), // không có trong ERD case study
                RenewalRequests = new List<RenewalRequestViewModel>(),      // không có trong ERD case study
                OverdueAlerts = overdue
            };

            return View(viewModel);
        }

        // GET: /Admin/Borrow/Create
        public async Task<IActionResult> Create(string? cardNumber)
        {
            var model = new CreateBorrowTicketViewModel
            {
                CardNumber = cardNumber ?? "",
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(15)
            };

            if (!string.IsNullOrWhiteSpace(cardNumber))
            {
                var the = await _context.TheThuViens
                    .Include(t => t.TaiKhoan)
                    .Include(t => t.PhieuMuons)
                        .ThenInclude(p => p.ChiTiets)
                    .FirstOrDefaultAsync(t => t.SoThe == cardNumber.Trim());

                if (the != null)
                {
                    var activeCount = the.PhieuMuons
                        .Where(p => p.TrangThai == "Đang mượn")
                        .SelectMany(p => p.ChiTiets)
                        .Count(c => c.NgayTraThucTe == null);

                    model.CardInfo = new CardLookupResultViewModel
                    {
                        CardNumber = the.SoThe,
                        StudentName = the.HoTen,
                        StudentCode = the.TaiKhoan.Username,
                        ExpiryDate = the.NgayHetHan,
                        CardStatus = the.TinhTrangSuDung,
                        HasUnreturnedBooks = activeCount > 0,
                        ActiveBorrowedCount = activeCount,
                        BlockReason = the.TinhTrangSuDung != "Sử dụng được"
                            ? "Thẻ đang ở trạng thái Không sử dụng — phải bồi thường trước."
                            : activeCount > 0
                                ? "Thẻ còn sách chưa trả — không thể mượn tiếp."
                                : ""
                    };
                }
                else
                {
                    ViewData["CardNotFound"] = true;
                }
            }

            // Danh sách bản sao còn "Còn" để chọn mượn
            var availableCopies = await _context.DauSachs
                .Include(d => d.Sach)
                .Where(d => d.TinhTrang == "Còn")
                .Select(d => new SelectListItem
                {
                    Value = d.MaDauSach.ToString(),
                    Text = $"{d.Sach.TenSach} — Số KS {d.SoKiemSoat} ({d.Sach.MaSachCode})"
                }).ToListAsync();
            ViewData["AvailableCopies"] = availableCopies;

            return View(model);
        }

        // POST: /Admin/Borrow/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string cardNumber, DateTime dueDate, List<int> selectedCopyIds, string? notes)
        {
            var the = await _context.TheThuViens
                .Include(t => t.PhieuMuons)
                    .ThenInclude(p => p.ChiTiets)
                .FirstOrDefaultAsync(t => t.SoThe == cardNumber.Trim());

            if (the == null)
            {
                TempData["Error"] = "Không tìm thấy thẻ thư viện.";
                return RedirectToAction(nameof(Create));
            }

            var activeCount = the.PhieuMuons
                .Where(p => p.TrangThai == "Đang mượn")
                .SelectMany(p => p.ChiTiets)
                .Count(c => c.NgayTraThucTe == null);

            if (the.TinhTrangSuDung != "Sử dụng được")
            {
                TempData["Error"] = "Thẻ đang ở trạng thái Không sử dụng, không thể lập phiếu mượn.";
                return RedirectToAction(nameof(Create), new { cardNumber });
            }

            if (activeCount > 0)
            {
                TempData["Error"] = "Thẻ còn sách chưa trả, không thể mượn tiếp.";
                return RedirectToAction(nameof(Create), new { cardNumber });
            }

            if (selectedCopyIds == null || selectedCopyIds.Count == 0 || selectedCopyIds.Count > 3)
            {
                TempData["Error"] = "Phải chọn từ 1 đến 3 cuốn sách.";
                return RedirectToAction(nameof(Create), new { cardNumber });
            }

            if (dueDate > DateTime.Today.AddDays(15) || dueDate <= DateTime.Today)
            {
                TempData["Error"] = "Hạn trả không hợp lệ (tối đa 15 ngày kể từ hôm nay).";
                return RedirectToAction(nameof(Create), new { cardNumber });
            }

            var copies = await _context.DauSachs
                .Where(d => selectedCopyIds.Contains(d.MaDauSach) && d.TinhTrang == "Còn")
                .ToListAsync();

            if (copies.Count != selectedCopyIds.Count)
            {
                TempData["Error"] = "Một số bản sao đã chọn không còn khả dụng.";
                return RedirectToAction(nameof(Create), new { cardNumber });
            }

            var phieuMuon = new PhieuMuon
            {
                MaThe = the.MaThe,
                NgayMuon = DateTime.Today,
                HanTra = dueDate,
                TrangThai = "Đang mượn",
                ChiTiets = copies.Select(c => new ChiTietPhieuMuon
                {
                    MaDauSach = c.MaDauSach,
                    TinhTrangTra = "Bình thường"
                }).ToList()
            };

            foreach (var c in copies)
            {
                c.TinhTrang = "Cho mượn";
            }

            _context.PhieuMuons.Add(phieuMuon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Lập phiếu mượn #{phieuMuon.MaPhieuMuon} thành công cho thẻ {the.SoThe}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Borrow/Return
        public async Task<IActionResult> Return(int? ticketCode)
        {
            if (ticketCode == null)
            {
                return View(new ReturnBookDeskViewModel());
            }

            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == ticketCode);

            if (phieu == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu mượn.";
                return View(new ReturnBookDeskViewModel());
            }

            var model = new ReturnBookDeskViewModel
            {
                TicketCode = phieu.MaPhieuMuon.ToString(),
                CardNumber = phieu.The.SoThe,
                StudentName = phieu.The.HoTen,
                StudentCode = phieu.The.TaiKhoan.Username,
                CurrentCardStatus = phieu.The.TinhTrangSuDung,
                CardExpiryDate = phieu.The.NgayHetHan,
                BorrowedCopies = phieu.ChiTiets
                    .Where(c => c.NgayTraThucTe == null)
                    .Select(c => new ReturnBookItemViewModel
                    {
                        CopyId = c.MaDauSach,
                        BookCode = c.DauSach.Sach.MaSachCode,
                        BookTitle = c.DauSach.Sach.TenSach,
                        ControlNumber = c.DauSach.SoKiemSoat,
                        BorrowDate = phieu.NgayMuon,
                        DueDate = phieu.HanTra
                    }).ToList()
            };

            return View(model);
        }

        // POST: /Admin/Borrow/Return
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int phieuMuonId, List<int> copyIds, List<string> conditions, List<decimal> fees)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == phieuMuonId);

            if (phieu == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu mượn.";
                return RedirectToAction(nameof(Return));
            }

            bool hasDamageOrLost = false;

            for (int i = 0; i < copyIds.Count; i++)
            {
                var chiTiet = phieu.ChiTiets.FirstOrDefault(c => c.MaDauSach == copyIds[i] && c.NgayTraThucTe == null);
                if (chiTiet == null) continue;

                chiTiet.NgayTraThucTe = DateTime.Today;
                chiTiet.TinhTrangTra = conditions[i];
                chiTiet.TienBoiThuong = fees[i];

                if (conditions[i] == "Bình thường")
                {
                    chiTiet.DauSach.TinhTrang = "Còn";
                }
                else if (conditions[i] == "Hỏng")
                {
                    chiTiet.DauSach.TinhTrang = "Hỏng";
                    hasDamageOrLost = true;
                }
                else if (conditions[i] == "Mất")
                {
                    chiTiet.DauSach.TinhTrang = "Mất";
                    hasDamageOrLost = true;
                }
            }

            if (hasDamageOrLost)
            {
                phieu.The.TinhTrangSuDung = "Không sử dụng";
            }

            if (phieu.ChiTiets.All(c => c.NgayTraThucTe != null))
            {
                phieu.TrangThai = "Đã trả";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã ghi nhận trả sách cho phiếu mượn #{phieu.MaPhieuMuon}.";
            return RedirectToAction(nameof(Index));
        }
    }
}