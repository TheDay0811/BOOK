using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;

namespace BOOK.Areas.Admin.Controllers
{
    public class SystemUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = "Độc giả"; // Độc giả, Thủ thư, Admin
        public bool IsActive { get; set; } = true;
        public bool IsCurrentAdmin { get; set; } = false;
    }

    public class EditUserInputModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Độc giả";
        public string? CardStatus { get; set; }
        public DateTime? CardExpiryDate { get; set; }
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

            var currentUsername = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";

            var users = taiKhoans.Select(tk => new SystemUserViewModel
            {
                Id = tk.MaTK,
                FullName = !string.IsNullOrWhiteSpace(tk.FullName) ? tk.FullName : (tk.TheThuVien?.HoTen ?? tk.Username),
                Username = tk.Username,
                Email = !string.IsNullOrWhiteSpace(tk.Email) ? tk.Email : (tk.Username.Contains("@") ? tk.Username : $"{tk.Username.ToLower()}@student.huflit.edu.vn"),
                PhoneNumber = !string.IsNullOrWhiteSpace(tk.PhoneNumber) ? tk.PhoneNumber : "---",
                AvatarUrl = !string.IsNullOrWhiteSpace(tk.AvatarUrl) ? tk.AvatarUrl : "/uploads/avatars/default-avatar.svg",
                Role = tk.Role switch
                {
                    "Admin" => "Admin",
                    "ThuThu" or "Librarian" => "Thủ thư",
                    _ => "Độc giả"
                },
                IsActive = tk.TheThuVien == null || tk.TheThuVien.TinhTrangSuDung == "Sử dụng được",
                IsCurrentAdmin = tk.Username.Equals("admin@huflit.edu.vn", System.StringComparison.OrdinalIgnoreCase) ||
                                 tk.Username.Equals("admin", System.StringComparison.OrdinalIgnoreCase) ||
                                 tk.Username.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase)
            }).ToList();

            return View(users);
        }

        // GET: /Admin/User/GetUser/5 (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var tk = await _context.TaiKhoans
                .Include(t => t.TheThuVien)
                    .ThenInclude(th => th!.PhieuMuons)
                        .ThenInclude(p => p.ChiTiets)
                .FirstOrDefaultAsync(t => t.MaTK == id);

            if (tk == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            var currentUsername = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";
            bool isCurrentAdmin = tk.Username.Equals("admin@huflit.edu.vn", System.StringComparison.OrdinalIgnoreCase) ||
                                 tk.Username.Equals("admin", System.StringComparison.OrdinalIgnoreCase) ||
                                 tk.Username.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase);

            int activeCount = 0;
            if (tk.TheThuVien != null)
            {
                activeCount = tk.TheThuVien.PhieuMuons
                    .Where(p => p.TrangThai == "Đang mượn")
                    .SelectMany(p => p.ChiTiets)
                    .Count(c => c.NgayTraThucTe == null);
            }

            return Json(new
            {
                success = true,
                id = tk.MaTK,
                username = tk.Username,
                fullName = !string.IsNullOrWhiteSpace(tk.FullName) ? tk.FullName : (tk.TheThuVien?.HoTen ?? tk.Username),
                email = !string.IsNullOrWhiteSpace(tk.Email) ? tk.Email : (tk.Username.Contains("@") ? tk.Username : $"{tk.Username.ToLower()}@student.huflit.edu.vn"),
                phoneNumber = !string.IsNullOrWhiteSpace(tk.PhoneNumber) ? tk.PhoneNumber : "",
                role = tk.Role switch
                {
                    "Admin" => "Admin",
                    "ThuThu" or "Librarian" => "Thủ thư",
                    _ => "Độc giả"
                },
                isCurrentAdmin = isCurrentAdmin,
                hasCard = tk.TheThuVien != null,
                cardNumber = tk.TheThuVien?.SoThe ?? "",
                cardStatus = tk.TheThuVien?.TinhTrangSuDung ?? "Sử dụng được",
                cardExpiryDate = tk.TheThuVien?.NgayHetHan.ToString("yyyy-MM-dd") ?? "",
                activeBorrowedCount = activeCount,
                avatarUrl = !string.IsNullOrWhiteSpace(tk.AvatarUrl) ? tk.AvatarUrl : "/uploads/avatars/default-avatar.svg"
            });
        }

        // POST: /Admin/User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserInputModel model)
        {
            var tk = await _context.TaiKhoans
                .Include(t => t.TheThuVien)
                .FirstOrDefaultAsync(t => t.MaTK == model.Id);

            if (tk == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";
            bool isCurrentAdmin = tk.Username.Equals("admin@huflit.edu.vn", System.StringComparison.OrdinalIgnoreCase) ||
                                 tk.Username.Equals("admin", System.StringComparison.OrdinalIgnoreCase) ||
                                 tk.Username.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase);

            tk.FullName = string.IsNullOrWhiteSpace(model.FullName) ? tk.FullName : model.FullName.Trim();
            tk.Email = string.IsNullOrWhiteSpace(model.Email) ? tk.Email : model.Email.Trim();
            tk.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? tk.PhoneNumber : model.PhoneNumber.Trim();

            // Nếu không phải là tài khoản Admin hiện tại, cho phép sửa role và thông tin thẻ
            if (!isCurrentAdmin)
            {
                if (!string.IsNullOrWhiteSpace(model.Role))
                {
                    tk.Role = model.Role switch
                    {
                        "Admin" => "Admin",
                        "Thủ thư" or "Librarian" => "Librarian",
                        _ => "SinhVien"
                    };
                }

                if (tk.TheThuVien != null)
                {
                    tk.TheThuVien.HoTen = tk.FullName ?? tk.TheThuVien.HoTen;
                    if (!string.IsNullOrWhiteSpace(model.CardStatus))
                    {
                        tk.TheThuVien.TinhTrangSuDung = model.CardStatus;
                    }
                    if (model.CardExpiryDate.HasValue)
                    {
                        tk.TheThuVien.NgayHetHan = model.CardExpiryDate.Value;
                    }
                }
            }
            else
            {
                tk.Role = "Admin";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật thông tin người dùng \"{tk.Username}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/User/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string newRole)
        {
            var user = await _context.TaiKhoans.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng được yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";
            if (user.Username.Equals("admin@huflit.edu.vn", System.StringComparison.OrdinalIgnoreCase) ||
                user.Username.Equals("admin", System.StringComparison.OrdinalIgnoreCase) ||
                user.Username.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Không thể thay đổi vai trò hoặc giáng quyền tài khoản Quản trị viên đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            user.Role = newRole switch
            {
                "Admin" => "Admin",
                "Thủ thư" or "Librarian" => "Librarian",
                _ => "SinhVien"
            };

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật vai trò của tài khoản '{user.Username}' thành '{newRole}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/User/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.TaiKhoans
                .Include(t => t.TheThuVien)
                .FirstOrDefaultAsync(t => t.MaTK == id);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản người dùng.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";
            if (user.Username.Equals("admin@huflit.edu.vn", System.StringComparison.OrdinalIgnoreCase) ||
                user.Username.Equals("admin", System.StringComparison.OrdinalIgnoreCase) ||
                user.Username.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Không thể khóa tài khoản Quản trị viên tối cao đang vận hành!";
                return RedirectToAction(nameof(Index));
            }

            if (user.TheThuVien != null)
            {
                user.TheThuVien.TinhTrangSuDung = user.TheThuVien.TinhTrangSuDung == "Sử dụng được" ? "Không sử dụng" : "Sử dụng được";
                await _context.SaveChangesAsync();
                var statusText = user.TheThuVien.TinhTrangSuDung == "Sử dụng được" ? "Mở khóa" : "Khóa";
                TempData["Success"] = $"Đã {statusText} tài khoản '{user.Username}' thành công!";
            }
            else
            {
                TempData["Warning"] = $"Tài khoản '{user.Username}' không liên kết thẻ độc giả nên luôn hoạt động.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/User/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.TaiKhoans.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            user.PasswordHash = user.Role == "Admin" ? "Admin@123456" : (user.Role == "Librarian" ? "Librarian@123456" : "123456");
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã đặt lại mật khẩu mặc định cho tài khoản '{user.Username}' ({user.PasswordHash}) thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}