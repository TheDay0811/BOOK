using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Wishlist" });
            }

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity.Name;
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favorites = await _context.YeuThichs
                .Where(y => y.MaTK == user.MaTK)
                .Include(y => y.Sach)
                    .ThenInclude(s => s.TheLoai)
                .Include(y => y.Sach)
                    .ThenInclude(s => s.NhaXuatBan)
                .Include(y => y.Sach)
                    .ThenInclude(s => s.DauSachs)
                .OrderByDescending(y => y.NgayThem)
                .ToListAsync();

            var viewModel = new WishlistIndexViewModel
            {
                Items = favorites.Select(y => new WishlistItemViewModel
                {
                    MaYeuThich = y.MaYeuThich,
                    BookId = y.MaSach,
                    Title = y.Sach.TenSach,
                    Author = y.Sach.TacGia,
                    CategoryName = y.Sach.TheLoai?.TenTheLoai ?? "Chưa phân loại",
                    PublisherName = y.Sach.NhaXuatBan?.TenNXB ?? "",
                    PublishYear = y.Sach.NamXuatBan,
                    CoverImageUrl = !string.IsNullOrEmpty(y.Sach.HinhAnh) ? y.Sach.HinhAnh : "/images/default-book.png",
                    AvailableCopies = y.Sach.DauSachs.Count(d => d.TrangThai == "Sẵn sàng" || d.TrangThai == "Có sẵn"),
                    TotalCopies = y.Sach.DauSachs.Count,
                    AddedDate = y.NgayThem
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: /Wishlist/Toggle (AJAX)
        [HttpPost]
        public async Task<IActionResult> Toggle(int bookId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để lưu sách vào danh sách yêu thích!" });
            }

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity.Name;
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (user == null)
            {
                return Json(new { success = false, requireLogin = true, message = "Tài khoản không tồn tại, vui lòng đăng nhập lại." });
            }

            var book = await _context.Sachs.FindAsync(bookId);
            if (book == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin cuốn sách này." });
            }

            var existing = await _context.YeuThichs
                .FirstOrDefaultAsync(y => y.MaTK == user.MaTK && y.MaSach == bookId);

            if (existing != null)
            {
                _context.YeuThichs.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorited = false, message = $"Đã bỏ lưu \"{book.TenSach}\" khỏi danh sách yêu thích." });
            }
            else
            {
                var newFav = new YeuThich
                {
                    MaTK = user.MaTK,
                    MaSach = bookId,
                    NgayThem = DateTime.Now
                };
                _context.YeuThichs.Add(newFav);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorited = true, message = $"Đã lưu \"{book.TenSach}\" vào danh sách yêu thích thành công!" });
            }
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int bookId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity.Name;
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            var existing = await _context.YeuThichs
                .FirstOrDefaultAsync(y => y.MaTK == user.MaTK && y.MaSach == bookId);

            if (existing != null)
            {
                _context.YeuThichs.Remove(existing);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sách khỏi danh sách yêu thích.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Wishlist/CheckStatus?bookId=...
        [HttpGet]
        public async Task<IActionResult> CheckStatus(int bookId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Json(new { isFavorited = false });
            }

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity.Name;
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Username == username);
            if (user == null) return Json(new { isFavorited = false });

            bool isFavorited = await _context.YeuThichs.AnyAsync(y => y.MaTK == user.MaTK && y.MaSach == bookId);
            return Json(new { isFavorited });
        }
    }
}
