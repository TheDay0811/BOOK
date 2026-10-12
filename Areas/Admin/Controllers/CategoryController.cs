using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Librarian,ThuThu")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Category/Index
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.TheLoais
                .Include(t => t.Sachs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(t => t.TenTheLoai.ToLower().Contains(kw));
            }

            var list = await query
                .OrderBy(t => t.MaTheLoai)
                .Select(t => new CategoryItemViewModel
                {
                    Id = t.MaTheLoai,
                    Name = t.TenTheLoai,
                    BookCount = t.Sachs.Count
                })
                .ToListAsync();

            ViewBag.Keyword = keyword;
            return View(list);
        }

        // POST: /Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vui lòng nhập tên thể loại sách.";
                return RedirectToAction(nameof(Index));
            }

            var trimmedName = name.Trim();
            var exists = await _context.TheLoais.AnyAsync(t => t.TenTheLoai.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                TempData["Error"] = $"Thể loại '{trimmedName}' đã tồn tại trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var category = new TheLoai
            {
                TenTheLoai = trimmedName
            };

            _context.TheLoais.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm thể loại '{trimmedName}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên thể loại không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.TheLoais.FindAsync(id);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy thể loại cần chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            var trimmedName = name.Trim();
            var exists = await _context.TheLoais.AnyAsync(t => t.MaTheLoai != id && t.TenTheLoai.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                TempData["Error"] = $"Thể loại '{trimmedName}' đã trùng tên với một thể loại khác.";
                return RedirectToAction(nameof(Index));
            }

            category.TenTheLoai = trimmedName;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin thể loại thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Category/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.TheLoais
                .Include(t => t.Sachs)
                .FirstOrDefaultAsync(t => t.MaTheLoai == id);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy thể loại để xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (category.Sachs != null && category.Sachs.Any())
            {
                TempData["Error"] = $"Không thể xóa thể loại '{category.TenTheLoai}' vì đang có {category.Sachs.Count} tựa sách thuộc thể loại này.";
                return RedirectToAction(nameof(Index));
            }

            _context.TheLoais.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa thể loại '{category.TenTheLoai}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
