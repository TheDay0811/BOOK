using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ThuThu")]
    public class PublisherController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PublisherController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index() =>
            View(await _context.NhaXuatBans.Include(n => n.Sachs).OrderBy(n => n.TenNXB).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id, string name)
        {
            name = name?.Trim() ?? "";
            if (name == "") TempData["Error"] = "Tên không được rỗng.";
            else if (await _context.NhaXuatBans.AnyAsync(n => n.TenNXB == name && n.MaNXB != id))
                TempData["Error"] = "Tên đã tồn tại.";
            else
            {
                if (id == 0) _context.NhaXuatBans.Add(new NhaXuatBan { TenNXB = name });
                else
                {
                    var n = await _context.NhaXuatBans.FindAsync(id);
                    if (n != null) n.TenNXB = name;
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã lưu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.NhaXuatBans.Include(x => x.Sachs).FirstOrDefaultAsync(x => x.MaNXB == id);
            if (n == null) return RedirectToAction(nameof(Index));
            if (n.Sachs.Any()) TempData["Error"] = "Nhà xuất bản đang có sách, không thể xóa.";
            else { _context.NhaXuatBans.Remove(n); await _context.SaveChangesAsync(); TempData["Success"] = "Đã xóa."; }
            return RedirectToAction(nameof(Index));
        }
    }
}