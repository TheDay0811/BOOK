using BOOK.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace BOOK.Areas.Admin.Controllers
{
    public class SystemUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Độc giả"; // Độc giả, Thủ thư, Admin
        public bool IsActive { get; set; } = true;
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/User/Index
        public async Task<IActionResult> Index()
        {
            var taiKhoans = await _context.TaiKhoans
                .Include(tk => tk.TheThuVien)
                .ToListAsync();

            var users = taiKhoans.Select(tk => new SystemUserViewModel
            {
                Id = tk.MaTK,
                FullName = tk.TheThuVien?.HoTen ?? tk.Username,
                Username = tk.Username,
                Role = tk.Role switch
                {
                    "Admin" => "Admin",
                    "ThuThu" => "Thủ thư",
                    _ => "Độc giả"
                },
                IsActive = tk.TheThuVien == null || tk.TheThuVien.TinhTrangSuDung == "Sử dụng được"
            }).ToList();

            return View(users);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null || !new[] { "Admin", "ThuThu", "SinhVien" }.Contains(role))
                TempData["Error"] = "Dữ liệu không hợp lệ.";
            else if (tk.Username == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                TempData["Error"] = "Không thể đổi quyền của chính mình.";
            else
            {
                tk.Role = role;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã đổi quyền của {tk.Username} (có hiệu lực sau khi họ đăng nhập lại).";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCard(int id, bool active)
        {
            var the = await _context.TheThuViens.FirstOrDefaultAsync(t => t.MaTK == id);
            if (the == null) TempData["Error"] = "Tài khoản này không có thẻ thư viện.";
            else
            {
                the.TinhTrangSuDung = active ? "Sử dụng được" : "Không sử dụng";
                await _context.SaveChangesAsync();
                TempData["Success"] = active ? "Đã mở khóa thẻ." : "Đã khóa thẻ.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}