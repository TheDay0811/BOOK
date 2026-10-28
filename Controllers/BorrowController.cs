using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /Borrow/CreateRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(BorrowRequestInputModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json");

            // 1. Kiểm tra xác thực (Chưa đăng nhập)
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Details", "Book", new { id = model.BookId });
                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        requireLogin = true,
                        loginUrl = Url.Action("Login", "Account", new { returnUrl }),
                        message = "Vui lòng đăng nhập tài khoản sinh viên để đặt mượn sách trực tuyến."
                    });
                }

                TempData["Error"] = "Vui lòng đăng nhập tài khoản sinh viên để đặt mượn sách trực tuyến.";
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // 2. Lấy thông tin độc giả từ Claims
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                studentCode = User.Identity.Name;
            }

            var the = await _context.TheThuViens
                .Include(t => t.TaiKhoan)
                .Include(t => t.PhieuMuons)
                    .ThenInclude(p => p.ChiTiets)
                .FirstOrDefaultAsync(t => t.TaiKhoan.Username == studentCode);

            if (the == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ Thẻ thư viện của bạn. Vui lòng liên hệ thủ thư." });
                }
                TempData["Error"] = "Không tìm thấy hồ sơ Thẻ thư viện của bạn. Vui lòng liên hệ thủ thư.";
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }

            // 3. Ràng buộc Thẻ: Trạng thái thẻ
            if (the.TinhTrangSuDung != "Sử dụng được" || DateTime.Today > the.NgayHetHan)
            {
                var errorMsg = "Thẻ thư viện của bạn đang bị khóa do có sách hỏng/mất hoặc hết hạn. Vui lòng liên hệ thủ thư để xử lý.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }

            // 4. Ràng buộc: Kiểm tra số sách đang mượn & chờ duyệt (tối đa 3 cuốn)
            var activeLoansCount = the.PhieuMuons
                .Where(p => p.TrangThai == "Đang mượn" || p.TrangThai == "Chờ duyệt")
                .SelectMany(p => p.ChiTiets)
                .Count(c => c.NgayTraThucTe == null);

            if (activeLoansCount >= 3)
            {
                var errorMsg = "Bạn đã mượn hoặc đang chờ duyệt 3 cuốn sách (đã đạt giới hạn tối đa 3 cuốn của Thư viện HUFLIT). Vui lòng hoàn trả các cuốn sách đang giữ trước khi mượn tiếp.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }

            var alreadyHoldingThisBook = the.PhieuMuons
                .Where(p => p.TrangThai == "Đang mượn" || p.TrangThai == "Chờ duyệt")
                .SelectMany(p => p.ChiTiets)
                .Any(c => c.NgayTraThucTe == null && c.DauSach != null && c.DauSach.MaSach == model.BookId);

            if (alreadyHoldingThisBook)
            {
                var errorMsg = "Bạn hiện đang mượn hoặc đang đăng ký mượn cuốn sách này. Theo quy chế, mỗi độc giả chỉ được mượn 1 bản sao của cùng một tựa sách.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }

            // 5. Tìm cuốn sách vật lý (DauSach) đầu tiên có tình trạng "Còn"
            var availableCopy = await _context.DauSachs
                .Include(d => d.Sach)
                .FirstOrDefaultAsync(d => d.MaSach == model.BookId && d.TinhTrang == "Còn");

            if (availableCopy == null)
            {
                var errorMsg = "Rất tiếc, tất cả bản sao của tựa sách này hiện đã được mượn hoặc đang phục hồi kho.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }

            // 6. Tính hạn trả (Quy định tối đa 15 ngày)
            var dueDate = DateTime.Today.AddDays(15);
            if (model.DueDate.HasValue && model.DueDate.Value > DateTime.Today && model.DueDate.Value <= DateTime.Today.AddDays(15))
            {
                dueDate = model.DueDate.Value;
            }

            // 7. Tạo phiếu mượn & chi tiết phiếu mượn
            var phieuMuon = new PhieuMuon
            {
                MaThe = the.MaThe,
                NgayMuon = DateTime.Today,
                HanTra = dueDate,
                TrangThai = "Chờ duyệt",
                ChiTiets = new()
                {
                    new ChiTietPhieuMuon
                    {
                        MaDauSach = availableCopy.MaDauSach,
                        TinhTrangTra = "Bình thường"
                    }
                }
            };

            // Đổi tình trạng cuốn sách sang "Cho mượn"
            availableCopy.TinhTrang = "Cho mượn";

            _context.PhieuMuons.Add(phieuMuon);

            // 8. Tạo thông báo cho Độc giả và Ban Quản Trị / Thủ thư
            _context.ThongBaos.Add(new ThongBao
            {
                TargetUser = the.TaiKhoan.Username,
                Title = "Đã gửi yêu cầu mượn sách thành công",
                Message = $"Bạn đã đăng ký mượn cuốn \"{availableCopy.Sach.TenSach}\" (Phiếu #{phieuMuon.MaPhieuMuon}, Bản số KS: {availableCopy.SoKiemSoat}). Vui lòng chờ Thủ thư duyệt và đến quầy nhận sách.",
                LinkUrl = "/Account/Profile#activeLoans",
                Type = "Info",
                DaDoc = false,
                CreatedAt = DateTime.Now
            });

            _context.ThongBaos.Add(new ThongBao
            {
                TargetUser = "AdminGroup",
                Title = "Yêu cầu mượn sách mới",
                Message = $"Độc giả {the.HoTen} (MSSV: {the.TaiKhoan.Username}) vừa đặt mượn cuốn \"{availableCopy.Sach.TenSach}\" (Phiếu #{phieuMuon.MaPhieuMuon}).",
                LinkUrl = "/Admin/Home/Index",
                Type = "Warning",
                DaDoc = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            var successMsg = $"Đặt mượn thành công cuốn sách \"{availableCopy.Sach.TenSach}\" (Số KS: [{availableCopy.SoKiemSoat}]). Hạn trả: {dueDate:dd/MM/yyyy}. Vui lòng đến Quầy Lưu Thông Tầng 1 để nhận sách!";

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    message = successMsg,
                    copyControlNumber = availableCopy.SoKiemSoat,
                    ticketId = phieuMuon.MaPhieuMuon,
                    dueDate = dueDate.ToString("dd/MM/yyyy")
                });
            }

            TempData["Success"] = successMsg;
            return RedirectToAction("Details", "Book", new { id = model.BookId });
        }
    }
}