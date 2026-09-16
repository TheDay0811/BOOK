using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;
using BOOK.ViewModels;

namespace BOOK.Controllers
{
    public class AccountController : Controller
    {
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

            string role = "Member";
            string fullName = "Trần Văn Hoàng";
            string studentCode = "24DH112970";
            string email = "hoang.tv@student.university.edu.vn";
            string cardNumber = "LIB-24DH112970";
            string cardStatus = "Sử dụng được";

            if (username.Contains("admin"))
            {
                role = "Admin";
                fullName = "Quản trị viên Hệ thống";
                studentCode = "ADMIN-01";
                email = "admin@smartlib.edu.vn";
                cardNumber = "LIB-ADMIN";
                cardStatus = "Sử dụng được";
            }
            else if (username.Contains("thuthu"))
            {
                role = "Librarian";
                fullName = "Thủ thư Nguyễn Thị Mai";
                studentCode = "LIB-STAFF";
                email = "mai.nt@smartlib.edu.vn";
                cardNumber = "LIB-STAFF";
                cardStatus = "Sử dụng được";
            }

            // Tạo Claims Identity xác thực Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, studentCode),
                new Claim(ClaimTypes.Role, role),
                new Claim("CardNumber", cardNumber),
                new Claim("CardStatus", cardStatus),
                new Claim("ActiveLoansCount", "2")
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
            else if (role == "Librarian")
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
                // Chuyển hướng trực tiếp tới trang Hồ sơ cá nhân & Quản lý mượn trả
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
        public IActionResult Register(RegisterViewModel model)
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

            // Tự động gán mã thẻ thư viện liên kết: LIB-{MSSV}
            var assignedCardNumber = $"LIB-{model.StudentCode.Trim().ToUpper()}";
            model.CardNumber = assignedCardNumber;
            model.CardIssueDate = DateTime.Today;
            model.CardExpiryDate = DateTime.Today.AddYears(1);
            model.CardStatus = "Sử dụng được";

            TempData["Success"] = $"Đăng ký tài khoản thành công cho độc giả {model.FullName}! Hệ thống đã tự động cấp Thẻ thư viện số [{assignedCardNumber}] có hạn sử dụng đến ngày {model.CardExpiryDate:dd/MM/yyyy}. Bạn có thể đăng nhập ngay bây giờ.";

            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            // Lấy thông tin từ Claims hoặc dữ liệu mẫu chuẩn nghiệp vụ Case Study
            var currentName = User.Identity?.Name ?? "Trần Văn Hoàng";
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "24DH112970";
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "hoang.tv@student.university.edu.vn";
            var cardNumber = User.FindFirst("CardNumber")?.Value ?? $"LIB-{studentCode}";
            var cardStatus = User.FindFirst("CardStatus")?.Value ?? "Sử dụng được";

            var model = new MemberProfileViewModel
            {
                FullName = currentName,
                StudentCode = studentCode,
                Email = email,
                PhoneNumber = "0912 345 678",
                Faculty = "Khoa Công Nghệ Thông Tin",
                CardNumber = cardNumber,
                CardIssueDate = DateTime.Today.AddMonths(-6),
                CardExpiryDate = DateTime.Today.AddMonths(6),
                CardStatus = cardStatus,
                TotalFinesPaid = 36000,
                TotalUnpaidFines = 0,
                ActiveLoans = new List<ActiveBorrowItemViewModel>
                {
                    new ActiveBorrowItemViewModel
                    {
                        BorrowId = 101,
                        BookCode = "CS01",
                        ControlNumber = "002",
                        BookTitle = "Cấu Trúc Dữ Liệu & Giải Thuật Ứng Dụng",
                        Author = "PGS.TS Nguyễn Đức Lam",
                        CategoryName = "Khoa học máy tính",
                        CoverImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=300&auto=format&fit=crop&q=80",
                        BorrowDate = DateTime.Today.AddDays(-10),
                        DueDate = DateTime.Today.AddDays(5),
                        RenewalCount = 0
                    },
                    new ActiveBorrowItemViewModel
                    {
                        BorrowId = 102,
                        BookCode = "CS04",
                        ControlNumber = "001",
                        BookTitle = "Lập Trình Web Hiện Đại Với ASP.NET Core 8 & C#",
                        Author = "TS. Lê Hoàng Quân",
                        CategoryName = "Công nghệ phần mềm",
                        CoverImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=300&auto=format&fit=crop&q=80",
                        BorrowDate = DateTime.Today.AddDays(-14),
                        DueDate = DateTime.Today.AddDays(1),
                        RenewalCount = 1
                    }
                },
                HistoryLoans = new List<BorrowHistoryItemViewModel>
                {
                    new BorrowHistoryItemViewModel
                    {
                        Id = 1,
                        BookCode = "CS02",
                        ControlNumber = "003",
                        BookTitle = "Phân Tích & Thiết Kế Hệ Thống Thông Tin Hiện Đại",
                        BorrowDate = DateTime.Today.AddMonths(-2),
                        ReturnDate = DateTime.Today.AddMonths(-2).AddDays(12),
                        ReturnCondition = "Bình thường (Đúng hạn)",
                        CompensationFee = 0,
                        PaymentStatus = "Miễn phí",
                        Notes = "Sách nguyên vẹn, trả trước hạn"
                    },
                    new BorrowHistoryItemViewModel
                    {
                        Id = 2,
                        BookCode = "DB03",
                        ControlNumber = "001",
                        BookTitle = "Cơ Sở Dữ Liệu Quan Hệ Nâng Cao & Tối Ưu Truy Vấn SQL",
                        BorrowDate = DateTime.Today.AddMonths(-3),
                        ReturnDate = DateTime.Today.AddMonths(-3).AddDays(18),
                        ReturnCondition = "Trễ hạn 3 ngày",
                        CompensationFee = 6000,
                        PaymentStatus = "Đã thanh toán",
                        Notes = "Phạt quá hạn 2.000đ/ngày x 3 ngày"
                    },
                    new BorrowHistoryItemViewModel
                    {
                        Id = 3,
                        BookCode = "NET05",
                        ControlNumber = "002",
                        BookTitle = "Mạng Máy Tính & Truyền Số Liệu Căn Bản",
                        BorrowDate = DateTime.Today.AddMonths(-4),
                        ReturnDate = DateTime.Today.AddMonths(-4).AddDays(14),
                        ReturnCondition = "Hư hỏng trang bìa (Bồi thường)",
                        CompensationFee = 30000,
                        PaymentStatus = "Đã thanh toán",
                        Notes = "Bồi thường phí sửa chữa đóng bìa sách"
                    }
                }
            };

            model.UpdateModel = new UpdateProfileInputModel
            {
                FullName = model.FullName,
                StudentCode = model.StudentCode,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Faculty = model.Faculty
            };

            return View(model);
        }

        // POST: /Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(UpdateProfileInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập liệu.";
                return RedirectToAction(nameof(Profile));
            }

            TempData["Success"] = $"Cập nhật thông tin thành công cho độc giả {input.FullName}!";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ExtendBorrow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExtendBorrow(int borrowId, string? reason)
        {
            TempData["Success"] = $"Yêu cầu gia hạn thêm 07 ngày cho phiếu mượn #{borrowId} đã được hệ thống phê duyệt tự động thành công!";
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
