using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace BOOK.Areas.Admin.Controllers
{
    public class SystemUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Độc giả"; // Độc giả, Thủ thư, Quản lý, Admin
        public bool IsActive { get; set; } = true;
    }

    [Area("Admin")]
    public class UserController : Controller
    {
        // GET: /Admin/User/Index
        public IActionResult Index()
        {
            var users = new List<SystemUserViewModel>
            {
                new SystemUserViewModel
                {
                    Id = 1,
                    FullName = "TS. Lê Quốc Hùng",
                    Username = "admin_hunglq",
                    Email = "hung.lq@university.edu.vn",
                    PhoneNumber = "0908 111 222",
                    Role = "Admin",
                    IsActive = true
                },
                new SystemUserViewModel
                {
                    Id = 2,
                    FullName = "Nguyễn Thị Thu Hà",
                    Username = "thuthu_hantt",
                    Email = "ha.ntt@university.edu.vn",
                    PhoneNumber = "0918 333 444",
                    Role = "Thủ thư",
                    IsActive = true
                },
                new SystemUserViewModel
                {
                    Id = 3,
                    FullName = "ThS. Đỗ Văn Nam",
                    Username = "quanly_namdv",
                    Email = "nam.dv@university.edu.vn",
                    PhoneNumber = "0932 555 666",
                    Role = "Quản lý",
                    IsActive = true
                },
                new SystemUserViewModel
                {
                    Id = 4,
                    FullName = "Trần Văn Hoàng",
                    Username = "22110098",
                    Email = "hoang.tv@student.edu.vn",
                    PhoneNumber = "0912 333 444",
                    Role = "Độc giả",
                    IsActive = true
                },
                new SystemUserViewModel
                {
                    Id = 5,
                    FullName = "Lê Thị Mai Linh",
                    Username = "23110255",
                    Email = "linh.ltm@student.edu.vn",
                    PhoneNumber = "0982 123 456",
                    Role = "Độc giả",
                    IsActive = false // Bị khóa do trễ hạn nhiều lần
                }
            };

            return View(users);
        }
    }
}
