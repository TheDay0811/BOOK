using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public string Role { get; set; } = "Độc giả"; // Độc giả, Thủ thư, Admin
        public bool IsActive { get; set; } = true;
    }

    [Area("Admin")]
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
    }
}