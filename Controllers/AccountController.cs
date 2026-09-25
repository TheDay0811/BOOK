using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;
using BOOK.ViewModels;

namespace BOOK.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Profile));
            }

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = model.UsernameOrEmail.Trim().ToLower();

            var taiKhoan = await _context.TaiKhoans
                .Include(tk => tk.TheThuVien)
                .FirstOrDefaultAsync(tk => tk.Username.ToLower() == username && tk.PasswordHash == model.Password);

            if (taiKhoan == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                return View(model);
            }

            string role = taiKhoan.Role; // "Admin" / "ThuThu" / "SinhVien"
            string fullName = taiKhoan.TheThuVien?.HoTen ?? taiKhoan.Username;
            string studentCode = taiKhoan.Username;
            string cardNumber = taiKhoan.TheThuVien?.SoThe ?? "";
            string cardStatus = taiKhoan.TheThuVien?.TinhTrangSuDung ?? "";

            // Tạo Claims Identity xác thực Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.NameIdentifier, studentCode),
                new Claim(ClaimTypes.Role, role),
                new Claim("CardNumber", cardNumber),
                new Claim("CardStatus", cardStatus)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 14 : 1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Điều hướng phân quyền
            if (role == "Admin")
            {
                TempData["Success"] = "Đăng nhập thành công với quyền Quản trị viên (Admin)!";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            else if (role == "ThuThu")
            {
                TempData["Success"] = "Đăng nhập thành công với quyền Thủ thư trực ca!";
                return RedirectToAction("Index", "Borrow", new { area = "Admin" });
            }
            else
            {
                TempData["Success"] = $"Đăng nhập thành công! Chào mừng độc giả {fullName} ({studentCode}) quay trở lại thư viện.";
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction(nameof(Profile));
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterViewModel
            {
                CardIssueDate = DateTime.Today,
                CardExpiryDate = DateTime.Today.AddYears(1),
                CardStatus = "Sử dụng được",
                CardNumber = "LIB-Chưa cấp"
            };
            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CardIssueDate = DateTime.Today;
                model.CardExpiryDate = DateTime.Today.AddYears(1);
                model.CardStatus = "Sử dụng được";
                model.CardNumber = !string.IsNullOrWhiteSpace(model.StudentCode)
                    ? $"LIB-{model.StudentCode.Trim().ToUpper()}"
                    : "LIB-Chưa cấp";
                return View(model);
            }

            var usernameExists = await _context.TaiKhoans
                .AnyAsync(tk => tk.Username.ToLower() == model.StudentCode.Trim().ToLower());

            if (usernameExists)
            {
                ModelState.AddModelError("StudentCode", "Mã số sinh viên này đã đăng ký tài khoản.");
                model.CardIssueDate = DateTime.Today;
                model.CardExpiryDate = DateTime.Today.AddYears(1);
                model.CardStatus = "Sử dụng được";
                model.CardNumber = $"LIB-{model.StudentCode.Trim().ToUpper()}";
                return View(model);
            }

            var newTaiKhoan = new TaiKhoan
            {
                Username = model.StudentCode.Trim(),
                PasswordHash = model.Password, // case study không yêu cầu hash, giữ đơn giản
                Role = "SinhVien"
            };

            var assignedCardNumber = $"LIB-{model.StudentCode.Trim().ToUpper()}";
            var newThe = new TheThuVien
            {
                SoThe = assignedCardNumber,
                HoTen = model.FullName,
                NgayHetHan = DateTime.Today.AddYears(1),
                TinhTrangSuDung = "Sử dụng được",
                TaiKhoan = newTaiKhoan
            };

            _context.TheThuViens.Add(newThe);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đăng ký tài khoản thành công cho độc giả {model.FullName}! Hệ thống đã tự động cấp Thẻ thư viện số [{assignedCardNumber}] có hạn sử dụng đến ngày {newThe.NgayHetHan:dd/MM/yyyy}. Bạn có thể đăng nhập ngay bây giờ.";

            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                return RedirectToAction(nameof(Login));
            }

            var the = await _context.TheThuViens
                .Include(t => t.TaiKhoan)
                .Include(t => t.PhieuMuons)
                    .ThenInclude(p => p.ChiTiets)
                        .ThenInclude(c => c.DauSach)
                            .ThenInclude(d => d.Sach)
                .FirstOrDefaultAsync(t => t.TaiKhoan.Username == studentCode);

            if (the == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var activeLoans = the.PhieuMuons
                .Where(p => p.TrangThai == "Đang mượn")
                .SelectMany(p => p.ChiTiets.Where(c => c.NgayTraThucTe == null).Select(c => new ActiveBorrowItemViewModel
                {
                    BorrowId = p.MaPhieuMuon,
                    BookCode = c.DauSach.Sach.MaSachCode,
                    ControlNumber = c.DauSach.SoKiemSoat,
                    BookTitle = c.DauSach.Sach.TenSach,
                    Author = c.DauSach.Sach.TacGia,
                    BorrowDate = p.NgayMuon,
                    DueDate = p.HanTra
                }))
                .ToList();

            var historyLoans = the.PhieuMuons
                .SelectMany(p => p.ChiTiets.Where(c => c.NgayTraThucTe != null).Select(c => new BorrowHistoryItemViewModel
                {
                    Id = c.MaChiTiet,
                    BookCode = c.DauSach.Sach.MaSachCode,
                    ControlNumber = c.DauSach.SoKiemSoat,
                    BookTitle = c.DauSach.Sach.TenSach,
                    BorrowDate = p.NgayMuon,
                    ReturnDate = c.NgayTraThucTe!.Value,
                    ReturnCondition = c.TinhTrangTra,
                    CompensationFee = c.TienBoiThuong,
                    PaymentStatus = c.DaBoiThuong ? "Đã thanh toán" : (c.TienBoiThuong > 0 ? "Chưa thanh toán" : "Miễn phí")
                }))
                .ToList();

            var model = new MemberProfileViewModel
            {
                FullName = the.HoTen,
                StudentCode = the.TaiKhoan.Username,
                CardNumber = the.SoThe,
                CardIssueDate = the.NgayHetHan.AddYears(-1),
                CardExpiryDate = the.NgayHetHan,
                CardStatus = the.TinhTrangSuDung,
                TotalUnpaidFines = historyLoans.Where(h => h.PaymentStatus == "Chưa thanh toán").Sum(h => h.CompensationFee),
                TotalFinesPaid = historyLoans.Where(h => h.PaymentStatus == "Đã thanh toán").Sum(h => h.CompensationFee),
                ActiveLoans = activeLoans,
                HistoryLoans = historyLoans
            };

            model.UpdateModel = new UpdateProfileInputModel
            {
                FullName = model.FullName,
                StudentCode = model.StudentCode
            };

            return View(model);
        }

        // POST: /Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập liệu.";
                return RedirectToAction(nameof(Profile));
            }

            var the = await _context.TheThuViens
                .Include(t => t.TaiKhoan)
                .FirstOrDefaultAsync(t => t.TaiKhoan.Username == input.StudentCode);

            if (the != null)
            {
                the.HoTen = input.FullName;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Cập nhật thông tin thành công cho độc giả {input.FullName}!";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ExtendBorrow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendBorrow(int borrowId, string? reason)
        {
            var phieu = await _context.PhieuMuons.FindAsync(borrowId);
            if (phieu != null && phieu.TrangThai == "Đang mượn")
            {
                phieu.HanTra = phieu.HanTra.AddDays(7);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Yêu cầu gia hạn thêm 07 ngày cho phiếu mượn #{borrowId} đã được hệ thống phê duyệt tự động thành công!";
            }
            else
            {
                TempData["Error"] = $"Không tìm thấy phiếu mượn #{borrowId} đang hoạt động.";
            }

            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ReportDamageOrLost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReportDamageOrLost(int borrowId, string issueType, string description)
        {
            TempData["Warning"] = $"Đã ghi nhận yêu cầu báo '{issueType}' cho phiếu mượn #{borrowId}. Thủ thư sẽ sớm kiểm tra mức độ tổn thất và thông báo hướng dẫn bồi thường tài liệu.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Bạn đã đăng xuất an toàn khỏi hệ thống.";
            return RedirectToAction("Index", "Home");
        }
    }
}