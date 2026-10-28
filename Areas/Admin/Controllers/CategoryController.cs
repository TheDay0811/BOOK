using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ThuThu")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index() =>
            View(await _context.TheLoais.Include(t => t.Sachs).OrderBy(t => t.TenTheLoai).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id, string name)
        {
            name = name?.Trim() ?? "";
            if (name == "") TempData["Error"] = "Tên không được rỗng.";
            else if (await _context.TheLoais.AnyAsync(t => t.TenTheLoai == name && t.MaTheLoai != id))
                TempData["Error"] = "Tên đã tồn tại.";
            else
            {
                if (id == 0) _context.TheLoais.Add(new TheLoai { TenTheLoai = name });
                else
                {
                    var t = await _context.TheLoais.FindAsync(id);
                    if (t != null) t.TenTheLoai = name;
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã lưu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.TheLoais.Include(x => x.Sachs).FirstOrDefaultAsync(x => x.MaTheLoai == id);
            if (t == null) return RedirectToAction(nameof(Index));
            if (t.Sachs.Any()) TempData["Error"] = "Thể loại đang có sách, không thể xóa.";
            else { _context.TheLoais.Remove(t); await _context.SaveChangesAsync(); TempData["Success"] = "Đã xóa."; }
            return RedirectToAction(nameof(Index));
        }
    }
}