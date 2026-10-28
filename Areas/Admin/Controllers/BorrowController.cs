using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ThuThu")]
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const decimal LateFeePerDay = 2000m; // đ/ngày/cuốn
        public BorrowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Borrow/Index
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var overdue = await _context.PhieuMuons
            .Where(p => p.TrangThai == "Đang mượn" && p.HanTra < today)
            .Select(p => new OverdueAlertViewModel
            {
            BorrowId = p.MaPhieuMuon,
            MemberName = p.The.HoTen,
            MemberCode = p.The.SoThe,
            BookTitle = p.ChiTiets.Where(c => c.NgayTraThucTe == null)
                     .Select(c => c.DauSach.Sach.TenSach).FirstOrDefault() ?? "",
            DueDate = p.HanTra,
            DaysOverdue = EF.Functions.DateDiffDay(p.HanTra, today),
            AccumulatedFine = EF.Functions.DateDiffDay(p.HanTra, today)
                          * p.ChiTiets.Count(c => c.NgayTraThucTe == null) * LateFeePerDay
            }).ToListAsync();
            var todayReturns = await _context.ChiTietPhieuMuons
                .CountAsync(c => c.NgayTraThucTe != null && c.NgayTraThucTe.Value.Date == today);


            var pending = await _context.YeuCauMuons
             .Where(y => y.TrangThai == "Chờ duyệt")
             .OrderBy(y => y.NgayYeuCau)
              .Select(y => new PendingBorrowRequestViewModel
          {
        Id = y.MaYeuCau,
        RequestCode = "YC" + y.MaYeuCau,
        MemberName = y.The.HoTen,
        MemberCode = y.The.SoThe,
        BookTitle = y.Sach.TenSach,
        BookISBN = y.Sach.MaSachCode,
        RequestDate = y.NgayYeuCau,
        PickupDueDate = y.HanTraDeXuat,
        Status = y.TrangThai
            }).ToListAsync();

            var renewals = await _context.YeuCauGiaHans
                .Where(g => g.TrangThai == "Chờ duyệt")
                .Select(g => new RenewalRequestViewModel
                {
                    RequestId = g.MaYeuCau,
                    BorrowId = g.MaPhieuMuon,
                    MemberName = g.PhieuMuon.The.HoTen,
                    MemberCode = g.PhieuMuon.The.SoThe,
                    BookTitle = g.PhieuMuon.ChiTiets.Select(c => c.DauSach.Sach.TenSach).FirstOrDefault() ?? "",
                    CurrentDueDate = g.PhieuMuon.HanTra,
                    RequestedNewDueDate = g.PhieuMuon.HanTra.AddDays(7),
                    TimesRenewed = _context.YeuCauGiaHans.Count(x => x.MaPhieuMuon == g.MaPhieuMuon && x.TrangThai == "Đã duyệt"),
                    Reason = g.LyDo
                }).ToListAsync();
            var viewModel = new LibrarianDashboardViewModel
            {
                TodayReturnsCount = todayReturns,
                PendingRequests = pending,
                RenewalRequests = renewals,
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
                if (DateTime.Today > phieu.HanTra)
                    chiTiet.TienBoiThuong += (DateTime.Today - phieu.HanTra).Days * LateFeePerDay;


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
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var yc = await _context.YeuCauMuons
                .Include(y => y.The).ThenInclude(t => t.PhieuMuons)
                .FirstOrDefaultAsync(y => y.MaYeuCau == id && y.TrangThai == "Chờ duyệt");
            if (yc == null) { TempData["Error"] = "Không tìm thấy yêu cầu."; return RedirectToAction(nameof(Index)); }

            if (yc.The.TinhTrangSuDung != "Sử dụng được" || yc.The.PhieuMuons.Any(p => p.TrangThai == "Đang mượn"))
            { TempData["Error"] = "Thẻ không đủ điều kiện mượn (bị khóa hoặc đang mượn sách)."; return RedirectToAction(nameof(Index)); }

            var copy = await _context.DauSachs
                .Where(d => d.MaSach == yc.MaSach && d.TinhTrang == "Còn")
                .OrderBy(d => d.SoKiemSoat).FirstOrDefaultAsync();
            if (copy == null) { TempData["Error"] = "Sách đã hết bản sao."; return RedirectToAction(nameof(Index)); }

            var due = yc.HanTraDeXuat.Date;
            if (due <= DateTime.Today || due > DateTime.Today.AddDays(15)) due = DateTime.Today.AddDays(15);

            copy.TinhTrang = "Cho mượn";
            _context.PhieuMuons.Add(new PhieuMuon
            {
                MaThe = yc.MaThe,
                NgayMuon = DateTime.Today,
                HanTra = due,
                TrangThai = "Đang mượn",
                ChiTiets = new List<ChiTietPhieuMuon> { new ChiTietPhieuMuon { MaDauSach = copy.MaDauSach } }
            });
            yc.TrangThai = "Đã duyệt";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã duyệt yêu cầu YC{id}, gán bản số {copy.SoKiemSoat}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var yc = await _context.YeuCauMuons.FindAsync(id);
            if (yc != null && yc.TrangThai == "Chờ duyệt") { yc.TrangThai = "Từ chối"; await _context.SaveChangesAsync(); }
            TempData["Success"] = $"Đã từ chối yêu cầu YC{id}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRenewal(int id)
        {
            var g = await _context.YeuCauGiaHans.Include(x => x.PhieuMuon)
                .FirstOrDefaultAsync(x => x.MaYeuCau == id && x.TrangThai == "Chờ duyệt");
            if (g == null) { TempData["Error"] = "Không tìm thấy yêu cầu."; return RedirectToAction(nameof(Index)); }

            if (g.PhieuMuon.TrangThai != "Đang mượn") g.TrangThai = "Từ chối";
            else { g.PhieuMuon.HanTra = g.PhieuMuon.HanTra.AddDays(7); g.TrangThai = "Đã duyệt"; }
            await _context.SaveChangesAsync();
            TempData["Success"] = g.TrangThai == "Đã duyệt" ? "Đã duyệt gia hạn +7 ngày." : "Phiếu đã trả, yêu cầu bị từ chối.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRenewal(int id)
        {
            var g = await _context.YeuCauGiaHans.FindAsync(id);
            if (g != null && g.TrangThai == "Chờ duyệt") { g.TrangThai = "Từ chối"; await _context.SaveChangesAsync(); }
            TempData["Success"] = "Đã từ chối yêu cầu gia hạn.";
            return RedirectToAction(nameof(Index));
        }
    }
}