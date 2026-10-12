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
    public class PublisherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublisherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Publisher/Index
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.NhaXuatBans
                .Include(n => n.Sachs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(n => n.TenNXB.ToLower().Contains(kw));
            }

            var list = await query
                .OrderBy(n => n.MaNXB)
                .Select(n => new PublisherItemViewModel
                {
                    Id = n.MaNXB,
                    Name = n.TenNXB,
                    BookCount = n.Sachs.Count
                })
                .ToListAsync();

            ViewBag.Keyword = keyword;
            return View(list);
        }

        // POST: /Admin/Publisher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vui lòng nhập tên nhà xuất bản.";
                return RedirectToAction(nameof(Index));
            }

            var trimmedName = name.Trim();
            var exists = await _context.NhaXuatBans.AnyAsync(n => n.TenNXB.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                TempData["Error"] = $"Nhà xuất bản '{trimmedName}' đã tồn tại trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var publisher = new NhaXuatBan
            {
                TenNXB = trimmedName
            };

            _context.NhaXuatBans.Add(publisher);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm nhà xuất bản '{trimmedName}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Publisher/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên nhà xuất bản không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var publisher = await _context.NhaXuatBans.FindAsync(id);
            if (publisher == null)
            {
                TempData["Error"] = "Không tìm thấy nhà xuất bản cần chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            var trimmedName = name.Trim();
            var exists = await _context.NhaXuatBans.AnyAsync(n => n.MaNXB != id && n.TenNXB.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                TempData["Error"] = $"Nhà xuất bản '{trimmedName}' đã trùng tên với một nhà xuất bản khác.";
                return RedirectToAction(nameof(Index));
            }

            publisher.TenNXB = trimmedName;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin nhà xuất bản thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Publisher/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var publisher = await _context.NhaXuatBans
                .Include(n => n.Sachs)
                .FirstOrDefaultAsync(n => n.MaNXB == id);

            if (publisher == null)
            {
                TempData["Error"] = "Không tìm thấy nhà xuất bản để xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (publisher.Sachs != null && publisher.Sachs.Any())
            {
                TempData["Error"] = $"Không thể xóa nhà xuất bản '{publisher.TenNXB}' vì đang có {publisher.Sachs.Count} tựa sách liên kết.";
                return RedirectToAction(nameof(Index));
            }

            _context.NhaXuatBans.Remove(publisher);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa nhà xuất bản '{publisher.TenNXB}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
