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
    [Authorize(Roles = "Admin,Librarian,ThuThu")]
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

            // 1. Danh sách yêu cầu mượn online đang chờ duyệt
            var pendingList = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .Where(p => p.TrangThai == "Chờ duyệt")
                .OrderByDescending(p => p.NgayMuon)
                .ToListAsync();

            var pendingRequests = pendingList.Select(p =>
            {
                var bookTitles = string.Join(", ", p.ChiTiets.Where(c => c.DauSach?.Sach != null).Select(c => c.DauSach.Sach.TenSach));
                var firstDetail = p.ChiTiets.FirstOrDefault();
                var book = firstDetail?.DauSach?.Sach;
                return new PendingBorrowRequestViewModel
                {
                    Id = p.MaPhieuMuon,
                    RequestCode = $"PM-{p.MaPhieuMuon:D4}",
                    MemberName = p.The?.HoTen ?? "Độc giả",
                    MemberCode = p.The?.TaiKhoan?.Username ?? p.The?.SoThe ?? "",
                    MemberPhone = !string.IsNullOrWhiteSpace(p.The?.TaiKhoan?.PhoneNumber) ? p.The.TaiKhoan.PhoneNumber : "Chưa cập nhật",
                    BookTitle = !string.IsNullOrWhiteSpace(bookTitles) ? bookTitles : (book?.TenSach ?? "Tài liệu"),
                    BookISBN = book?.MaSachCode ?? p.The?.SoThe ?? "---",
                    RequestDate = p.NgayMuon,
                    PickupDueDate = p.HanTra,
                    Status = p.TrangThai
                };
            }).ToList();

            // 2. Danh sách đang mượn để tiếp nhận trả sách tại quầy
            var activeBorrows = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .Where(p => p.TrangThai == "Đang mượn")
                .OrderByDescending(p => p.NgayMuon)
                .ToListAsync();

            var activeBorrowItems = activeBorrows.Select(p =>
            {
                var bookTitles = string.Join(", ", p.ChiTiets.Where(c => c.DauSach?.Sach != null).Select(c => c.DauSach.Sach.TenSach));
                var isOverdue = p.HanTra < today;
                var daysLate = isOverdue ? (today - p.HanTra).Days : 0;

                return new BookReturnItemViewModel
                {
                    BorrowId = p.MaPhieuMuon,
                    BorrowCode = $"PM-{p.MaPhieuMuon:D4}",
                    MemberCode = p.The?.TaiKhoan?.Username ?? p.The?.SoThe ?? "",
                    MemberName = p.The?.HoTen ?? "Độc giả",
                    BookTitle = string.IsNullOrWhiteSpace(bookTitles) ? "Tài liệu mượn" : bookTitles,
                    BookISBN = p.The?.SoThe ?? "",
                    BorrowDate = p.NgayMuon,
                    DueDate = p.HanTra,
                    DaysOverdue = daysLate,
                    ConditionStatus = isOverdue ? $"Trễ {daysLate} ngày" : "Trong hạn",
                    EstimatedFine = daysLate * 2000
                };
            }).ToList();

            // 3. Danh sách quá hạn chưa trả
            var overdue = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .Where(p => p.TrangThai == "Đang mượn" && p.HanTra < today)
                .Select(p => new OverdueAlertViewModel
                {
                    BorrowId = p.MaPhieuMuon,
                    MemberName = p.The.HoTen,
                    MemberCode = p.The.TaiKhoan.Username,
                    MemberEmail = !string.IsNullOrWhiteSpace(p.The.TaiKhoan.Email) ? p.The.TaiKhoan.Email : (p.The.TaiKhoan.Username + "@student.huflit.edu.vn"),
                    MemberPhone = !string.IsNullOrWhiteSpace(p.The.TaiKhoan.PhoneNumber) ? p.The.TaiKhoan.PhoneNumber : "Chưa cập nhật",
                    BookTitle = p.ChiTiets.Select(c => c.DauSach.Sach.TenSach).FirstOrDefault() ?? "Tài liệu",
                    DueDate = p.HanTra,
                    DaysOverdue = EF.Functions.DateDiffDay(p.HanTra, today),
                    AccumulatedFine = EF.Functions.DateDiffDay(p.HanTra, today) * 2000
                }).ToListAsync();

            // 4. Số lượng trả hôm nay
            var todayReturns = await _context.ChiTietPhieuMuons
                .CountAsync(c => c.NgayTraThucTe != null && c.NgayTraThucTe.Value.Date == today);


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
                PendingRequests = pendingRequests,
                ActiveBorrowsForReturn = activeBorrowItems,
                RenewalRequests = renewals,
                OverdueAlerts = overdue
            };

            return View(viewModel);
        }

        // POST: /Admin/Borrow/ApproveBorrowRequest
        [HttpPost]
        public async Task<IActionResult> ApproveBorrowRequest(int requestId)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == requestId);

            if (phieu == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu mượn #" + requestId });
            }

            phieu.TrangThai = "Đang mượn";
            foreach (var c in phieu.ChiTiets)
            {
                if (c.DauSach != null)
                {
                    c.DauSach.TinhTrang = "Cho mượn";
                }
            }

            if (phieu.The?.TaiKhoan != null)
            {
                var bookTitles = string.Join(", ", phieu.ChiTiets.Select(c => c.DauSach?.Sach?.TenSach ?? "Tài liệu").Distinct());
                _context.ThongBaos.Add(new ThongBao
                {
                    TargetUser = phieu.The.TaiKhoan.Username,
                    Title = "Phiếu mượn sách đã được phê duyệt!",
                    Message = $"Yêu cầu mượn sách cho phiếu #{phieu.MaPhieuMuon} ({bookTitles}) đã được Thủ thư phê duyệt. Bạn có thể đến quầy thư viện cơ sở Hóc Môn để nhận sách.",
                    LinkUrl = "/Account/Profile#activeLoans",
                    Type = "Success",
                    DaDoc = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã duyệt cấp sách cho phiếu mượn #{requestId} thành công!" });
        }

        // POST: /Admin/Borrow/RejectBorrowRequest
        [HttpPost]
        public async Task<IActionResult> RejectBorrowRequest(int requestId, string? reason)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == requestId);

            if (phieu == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu mượn #" + requestId });
            }

            phieu.TrangThai = "Đã từ chối";
            foreach (var c in phieu.ChiTiets)
            {
                if (c.DauSach != null)
                {
                    c.DauSach.TinhTrang = "Còn";
                }
            }

            if (phieu.The?.TaiKhoan != null)
            {
                var bookTitles = string.Join(", ", phieu.ChiTiets.Select(c => c.DauSach?.Sach?.TenSach ?? "Tài liệu").Distinct());
                _context.ThongBaos.Add(new ThongBao
                {
                    TargetUser = phieu.The.TaiKhoan.Username,
                    Title = "Yêu cầu mượn sách bị từ chối",
                    Message = $"Yêu cầu mượn sách cho phiếu #{phieu.MaPhieuMuon} ({bookTitles}) đã bị từ chối. Lý do: {reason ?? "Tài liệu đang kiểm kê hoặc thẻ không đủ điều kiện"}.",
                    LinkUrl = "/Account/Profile",
                    Type = "Error",
                    DaDoc = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã từ chối phiếu mượn #{requestId} thành công." });
        }

        // POST: /Admin/Borrow/ApproveRenewal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRenewal(int id)
        {
            var g = await _context.YeuCauGiaHans.Include(x => x.PhieuMuon)
                .FirstOrDefaultAsync(x => x.MaYeuCau == id && x.TrangThai == "Chờ duyệt");
            if (g == null) { TempData["Error"] = "Không tìm thấy yêu cầu gia hạn."; return RedirectToAction(nameof(Index)); }

            if (g.PhieuMuon.TrangThai != "Đang mượn") g.TrangThai = "Từ chối";
            else { g.PhieuMuon.HanTra = g.PhieuMuon.HanTra.AddDays(7); g.TrangThai = "Đã duyệt"; }
            await _context.SaveChangesAsync();
            TempData["Success"] = g.TrangThai == "Đã duyệt" ? "Đã duyệt gia hạn thêm 07 ngày." : "Phiếu đã trả, yêu cầu gia hạn bị từ chối.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Borrow/RejectRenewal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRenewal(int id)
        {
            var g = await _context.YeuCauGiaHans.FindAsync(id);
            if (g != null && g.TrangThai == "Chờ duyệt") { g.TrangThai = "Từ chối"; await _context.SaveChangesAsync(); }
            TempData["Success"] = "Đã từ chối yêu cầu gia hạn.";
            return RedirectToAction(nameof(Index));
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
                        HasUnreturnedBooks = activeCount >= 3,
                        ActiveBorrowedCount = activeCount,
                        BlockReason = the.TinhTrangSuDung != "Sử dụng được"
                            ? "Thẻ đang ở trạng thái Không sử dụng — phải bồi thường trước."
                            : activeCount >= 3
                                ? "Độc giả đã giữ tối đa 3 cuốn sách — phải trả bớt sách trước khi mượn thêm."
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

            if (activeCount + (selectedCopyIds?.Count ?? 0) > 3)
            {
                TempData["Error"] = $"Thẻ này đang giữ {activeCount} cuốn sách. Hạn mức mượn tối đa là 3 cuốn (chỉ có thể chọn thêm tối đa {Math.Max(0, 3 - activeCount)} cuốn).";
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
        public async Task<IActionResult> Return(int phieuMuonId, List<int> copyIds, List<string> conditions, List<decimal> fees, List<string>? paidStatuses)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == phieuMuonId);

            if (phieu == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu mượn.";
                return RedirectToAction(nameof(Return));
            }

            bool hasUnpaidFine = false;

            for (int i = 0; i < copyIds.Count; i++)
            {
                var chiTiet = phieu.ChiTiets.FirstOrDefault(c => c.MaDauSach == copyIds[i] && c.NgayTraThucTe == null);
                if (chiTiet == null) continue;

                chiTiet.NgayTraThucTe = DateTime.Today;
                chiTiet.TinhTrangTra = conditions[i];
                chiTiet.TienBoiThuong = fees[i];
                if (DateTime.Today > phieu.HanTra)
                    chiTiet.TienBoiThuong += (DateTime.Today - phieu.HanTra).Days * LateFeePerDay;


                bool isPaidAtCounter = paidStatuses != null && (paidStatuses.Contains(i.ToString()) || paidStatuses.Contains(copyIds[i].ToString()));

                if (chiTiet.TienBoiThuong > 0 || conditions[i] == "Hỏng" || conditions[i] == "Mất")
                {
                    chiTiet.DaBoiThuong = isPaidAtCounter;
                    if (!isPaidAtCounter)
                    {
                        hasUnpaidFine = true;
                    }
                }
                else
                {
                    chiTiet.DaBoiThuong = true;
                }

                if (conditions[i] == "Bình thường")
                {
                    chiTiet.DauSach.TinhTrang = "Còn";
                }
                else if (conditions[i] == "Hỏng")
                {
                    chiTiet.DauSach.TinhTrang = "Hỏng";
                }
                else if (conditions[i] == "Mất")
                {
                    chiTiet.DauSach.TinhTrang = "Mất";
                }
            }

            if (hasUnpaidFine)
            {
                phieu.The.TinhTrangSuDung = "Không sử dụng";
            }
            else
            {
                var otherUnpaid = await _context.ChiTietPhieuMuons
                    .Where(c => c.PhieuMuon.MaThe == phieu.MaThe && c.MaPhieuMuon != phieu.MaPhieuMuon && !c.DaBoiThuong && c.TienBoiThuong > 0)
                    .AnyAsync();
                if (!otherUnpaid && phieu.The.NgayHetHan >= DateTime.Today)
                {
                    phieu.The.TinhTrangSuDung = "Sử dụng được";
                }
            }

            if (phieu.ChiTiets.All(c => c.NgayTraThucTe != null))
            {
                phieu.TrangThai = "Đã trả";
            }

            // Gửi thông báo cho Độc giả
            if (phieu.The?.TaiKhoan != null)
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    TargetUser = phieu.The.TaiKhoan.Username,
                    Title = "Đã hoàn tất thủ tục trả sách",
                    Message = hasUnpaidFine
                        ? $"Phiếu mượn #{phieu.MaPhieuMuon} đã ghi nhận trả với tình trạng có bồi thường chưa nộp. Thẻ của bạn đang tạm khóa cho đến khi nộp phí."
                        : $"Phiếu mượn #{phieu.MaPhieuMuon} đã được trả hoàn tất vào kho thư viện cơ sở Hóc Môn. Cảm ơn bạn đã giữ gìn tài liệu!",
                    LinkUrl = "/Account/Profile#history",
                    Type = hasUnpaidFine ? "Warning" : "Success",
                    DaDoc = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã ghi nhận trả sách cho phiếu mượn #{phieu.MaPhieuMuon}." + (hasUnpaidFine ? " Thẻ độc giả bị tạm khóa do còn phí bồi thường." : " Đã thanh toán đầy đủ, thẻ hợp lệ.");
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Borrow/ConfirmCounterPayment
        [HttpPost]
        public async Task<IActionResult> ConfirmCounterPayment(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                return Json(new { success = false, message = "Vui lòng cung cấp số thẻ thư viện." });
            }

            var the = await _context.TheThuViens
                .Include(t => t.TaiKhoan)
                .Include(t => t.PhieuMuons)
                    .ThenInclude(p => p.ChiTiets)
                .FirstOrDefaultAsync(t => t.SoThe == cardNumber.Trim());

            if (the == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thẻ thư viện." });
            }

            var unpaidDetails = the.PhieuMuons
                .SelectMany(p => p.ChiTiets)
                .Where(c => !c.DaBoiThuong && c.TienBoiThuong > 0)
                .ToList();

            decimal totalPaid = 0;
            foreach (var d in unpaidDetails)
            {
                d.DaBoiThuong = true;
                totalPaid += d.TienBoiThuong;
            }

            if (the.NgayHetHan >= DateTime.Today)
            {
                the.TinhTrangSuDung = "Sử dụng được";
            }

            if (the.TaiKhoan != null)
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    TargetUser = the.TaiKhoan.Username,
                    Title = "Xác nhận nộp phí bồi thường thành công",
                    Message = $"Thủ thư đã xác nhận thu phí bồi thường số tiền {totalPaid:N0} VNĐ cho thẻ {the.SoThe}. Thẻ của bạn đã được kích hoạt lại [Sử dụng được].",
                    LinkUrl = "/Account/Profile",
                    Type = "Success",
                    DaDoc = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã xác nhận thu {totalPaid:N0} VNĐ và mở khóa thẻ {the.SoThe} thành công!" });
        }

        // GET: /Admin/Borrow/PrintTicket?ticketCode=1
        [HttpGet]
        public async Task<IActionResult> PrintTicket(int ticketCode)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                            .ThenInclude(s => s.TheLoai)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == ticketCode);

            if (phieu == null)
            {
                TempData["Error"] = $"Không tìm thấy phiếu mượn #{ticketCode}.";
                return RedirectToAction(nameof(Index));
            }

            return View(phieu);
        }

        // GET: /Admin/Borrow/Payment
        [HttpGet]
        public async Task<IActionResult> Payment(string? searchTerm)
        {
            var model = new PaymentDeskViewModel
            {
                SearchTerm = searchTerm ?? ""
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                model.HasSearched = true;
                var term = searchTerm.Trim();

                // Tra cứu theo Số thẻ, MSSV/Username, hoặc Mã phiếu
                TheThuVien? the = null;

                if (int.TryParse(term, out int ticketCode))
                {
                    var phieu = await _context.PhieuMuons
                        .Include(p => p.The)
                        .FirstOrDefaultAsync(p => p.MaPhieuMuon == ticketCode);
                    if (phieu != null)
                    {
                        the = await _context.TheThuViens
                            .Include(t => t.TaiKhoan)
                            .Include(t => t.PhieuMuons)
                                .ThenInclude(p => p.ChiTiets)
                                    .ThenInclude(c => c.DauSach)
                                        .ThenInclude(d => d.Sach)
                            .FirstOrDefaultAsync(t => t.MaThe == phieu.MaThe);
                    }
                }

                if (the == null)
                {
                    the = await _context.TheThuViens
                        .Include(t => t.TaiKhoan)
                        .Include(t => t.PhieuMuons)
                            .ThenInclude(p => p.ChiTiets)
                                .ThenInclude(c => c.DauSach)
                                    .ThenInclude(d => d.Sach)
                        .FirstOrDefaultAsync(t => t.SoThe == term || t.TaiKhoan.Username == term);
                }

                if (the != null)
                {
                    model.CardFound = true;
                    model.CardNumber = the.SoThe;
                    model.StudentName = the.HoTen;
                    model.StudentCode = the.TaiKhoan.Username;
                    model.CardStatus = the.TinhTrangSuDung;
                    model.CardExpiryDate = the.NgayHetHan;

                    var unpaidChiTiets = the.PhieuMuons
                        .SelectMany(p => p.ChiTiets.Select(c => new { Detail = c, Loan = p }))
                        .Where(x => !x.Detail.DaBoiThuong && (x.Detail.TienBoiThuong > 0 || x.Detail.TinhTrangTra == "Hỏng" || x.Detail.TinhTrangTra == "Mất"))
                        .ToList();

                    foreach (var item in unpaidChiTiets)
                    {
                        var fee = item.Detail.TienBoiThuong;
                        if (fee <= 0)
                        {
                            fee = item.Detail.TinhTrangTra == "Hỏng" ? 50000 : 100000;
                            item.Detail.TienBoiThuong = fee;
                        }

                        model.UnpaidFines.Add(new UnpaidFineItemViewModel
                        {
                            ChiTietId = item.Detail.MaChiTiet,
                            PhieuMuonId = item.Loan.MaPhieuMuon,
                            BookTitle = item.Detail.DauSach?.Sach?.TenSach ?? "Tài liệu thư viện",
                            BookCode = item.Detail.DauSach?.Sach?.MaSachCode ?? "---",
                            ControlNumber = item.Detail.DauSach?.SoKiemSoat ?? "001",
                            IncidentType = item.Detail.TinhTrangTra ?? "Hỏng",
                            IncidentDate = item.Detail.NgayTraThucTe ?? item.Loan.HanTra,
                            FineAmount = fee,
                            Description = item.Detail.TinhTrangTra == "Hỏng" 
                                ? "Bồi thường hư hỏng / rách sách (Định mức 50.000 VNĐ)" 
                                : "Bồi thường làm mất sách (Định mức 100.000 VNĐ)"
                        });
                    }
                }
            }

            return View(model);
        }

        // POST: /Admin/Borrow/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(string cardNumber, List<int> chiTietIds, string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || chiTietIds == null || !chiTietIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một khoản phạt/bồi thường để thanh toán.";
                return RedirectToAction(nameof(Payment), new { searchTerm = cardNumber });
            }

            var the = await _context.TheThuViens
                .Include(t => t.TaiKhoan)
                .Include(t => t.PhieuMuons)
                    .ThenInclude(p => p.ChiTiets)
                .FirstOrDefaultAsync(t => t.SoThe == cardNumber.Trim());

            if (the == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ thẻ thư viện.";
                return RedirectToAction(nameof(Payment));
            }

            var detailsToPay = the.PhieuMuons
                .SelectMany(p => p.ChiTiets)
                .Where(c => chiTietIds.Contains(c.MaChiTiet) && !c.DaBoiThuong)
                .ToList();

            decimal totalCollected = 0;
            foreach (var d in detailsToPay)
            {
                d.DaBoiThuong = true;
                if (d.TienBoiThuong <= 0)
                {
                    d.TienBoiThuong = d.TinhTrangTra == "Hỏng" ? 50000 : 100000;
                }
                totalCollected += d.TienBoiThuong;
            }

            // Kiểm tra xem thẻ còn khoản nợ nào khác không
            var remainingUnpaid = the.PhieuMuons
                .SelectMany(p => p.ChiTiets)
                .Any(c => !c.DaBoiThuong && (c.TienBoiThuong > 0 || c.TinhTrangTra == "Hỏng" || c.TinhTrangTra == "Mất"));

            if (!remainingUnpaid && the.NgayHetHan >= DateTime.Today)
            {
                the.TinhTrangSuDung = "Sử dụng được";
            }

            // Tạo thông báo cho sinh viên
            if (the.TaiKhoan != null)
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    TargetUser = the.TaiKhoan.Username,
                    Title = "Biên lai thanh toán bồi thường tại quầy",
                    Message = $"Bạn đã hoàn tất nộp số tiền {totalCollected:N0} VNĐ (Hình thức: {paymentMethod}) tại Quầy thư viện HUFLIT. Trạng thái Thẻ thư viện: [{the.TinhTrangSuDung}].",
                    LinkUrl = "/Account/Profile",
                    Type = "Success",
                    DaDoc = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thu thành công {totalCollected:N0} VNĐ từ độc giả {the.HoTen}. " + 
                (!remainingUnpaid ? "Thẻ đã được kích hoạt lại [Sử dụng được]!" : "Thẻ vẫn còn khoản phạt khác chưa hoàn tất.");

            return RedirectToAction(nameof(Payment), new { searchTerm = cardNumber });
        }
    }
}