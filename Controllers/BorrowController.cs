using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;

namespace BOOK.Controllers
{
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BorrowController(ApplicationDbContext context) { _context = context; }

        [HttpPost]
        [Authorize(Roles = "SinhVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(int bookId, DateTime dueDate)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var the = await _context.TheThuViens
                .Include(t => t.PhieuMuons)
                .FirstOrDefaultAsync(t => t.TaiKhoan.Username == username);

            string? error = null;
            if (the == null) error = "Không tìm thấy thẻ thư viện.";
            else if (the.TinhTrangSuDung != "Sử dụng được" || the.NgayHetHan < DateTime.Today)
                error = "Thẻ không sử dụng được (bị khóa hoặc hết hạn).";
            else if (the.PhieuMuons.Any(p => p.TrangThai == "Đang mượn"))
                error = "Bạn đang mượn sách, trả xong mới được mượn tiếp.";
            else if (await _context.YeuCauMuons.AnyAsync(y => y.MaThe == the.MaThe && y.TrangThai == "Chờ duyệt"))
                error = "Bạn đã có một yêu cầu mượn đang chờ duyệt.";
            else if (dueDate.Date <= DateTime.Today || dueDate.Date > DateTime.Today.AddDays(15))
                error = "Hạn trả tối đa 15 ngày kể từ hôm nay.";
            else if (!await _context.DauSachs.AnyAsync(d => d.MaSach == bookId && d.TinhTrang == "Còn"))
                error = "Sách hiện không còn bản nào.";

            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", "Book", new { id = bookId });
            }

            _context.YeuCauMuons.Add(new YeuCauMuon { MaThe = the!.MaThe, MaSach = bookId, HanTraDeXuat = dueDate.Date });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi yêu cầu mượn. Thủ thư sẽ duyệt sớm.";
            return RedirectToAction("Details", "Book", new { id = bookId });
        }
    }
}