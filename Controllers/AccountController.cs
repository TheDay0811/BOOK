using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;
using BOOK.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                if (User.IsInRole("Librarian") || User.IsInRole("ThuThu"))
                {
                    return RedirectToAction("Index", "Borrow", new { area = "Admin" });
                }
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
                .FirstOrDefaultAsync(tk => (
                    tk.Username.ToLower() == username || 
                    (tk.Email != null && tk.Email.ToLower() == username) ||
                    (username == "admin" && (tk.Username.ToLower() == "admin@huflit.edu.vn" || tk.Role == "Admin")) ||
                    (username == "admin@huflit.edu.vn" && (tk.Username.ToLower() == "admin" || tk.Role == "Admin")) ||
                    (username == "thuthu" && (tk.Username.ToLower() == "thuthu@huflit.edu.vn" || tk.Role == "Librarian" || tk.Role == "ThuThu")) ||
                    (username == "thuthu@huflit.edu.vn" && (tk.Username.ToLower() == "thuthu" || tk.Role == "Librarian" || tk.Role == "ThuThu"))
                ) && tk.PasswordHash == model.Password);

            if (taiKhoan == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập / Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Tự động cấp lại thẻ thư viện nếu tài khoản sinh viên chưa có thẻ
            if (taiKhoan.Role == "SinhVien" && taiKhoan.TheThuVien == null)
            {
                taiKhoan.TheThuVien = new TheThuVien
                {
                    SoThe = $"LIB-{taiKhoan.Username.Trim().ToUpper()}",
                    HoTen = !string.IsNullOrWhiteSpace(taiKhoan.FullName) ? taiKhoan.FullName : taiKhoan.Username,
                    NgayHetHan = DateTime.Today.AddYears(1),
                    TinhTrangSuDung = "Sử dụng được"
                };
                await _context.SaveChangesAsync();
            }

            string role = taiKhoan.Role; // "Admin" / "Librarian" / "ThuThu" / "SinhVien"
            string fullName = !string.IsNullOrWhiteSpace(taiKhoan.FullName)
                ? taiKhoan.FullName
                : (taiKhoan.TheThuVien?.HoTen ?? taiKhoan.Username);
            string studentCode = taiKhoan.Username;
            string cardNumber = taiKhoan.TheThuVien?.SoThe ?? "";
            string cardStatus = taiKhoan.TheThuVien?.TinhTrangSuDung ?? "";
            string avatarUrl = !string.IsNullOrEmpty(taiKhoan.AvatarUrl) ? taiKhoan.AvatarUrl : "/uploads/avatars/default-avatar.svg";

            // Tạo Claims Identity xác thực Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.NameIdentifier, studentCode),
                new Claim(ClaimTypes.Role, role),
                new Claim("CardNumber", cardNumber),
                new Claim("CardStatus", cardStatus),
                new Claim("AvatarUrl", avatarUrl)
            };

            if (!string.IsNullOrEmpty(taiKhoan.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, taiKhoan.Email));
            }

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
            else if (role == "ThuThu" || role == "Librarian")
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
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                PasswordHash = model.Password, // case study không yêu cầu hash, giữ đơn giản
                Role = "SinhVien",
                AvatarUrl = "/uploads/avatars/default-avatar.svg"
            };

            var assignedCardNumber = $"LIB-{model.StudentCode.Trim().ToUpper()}";
            var newThe = new TheThuVien
            {
                SoThe = assignedCardNumber,
                HoTen = model.FullName.Trim(),
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
        [Authorize]
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
                .FirstOrDefaultAsync(t => t.TaiKhoan.Username == studentCode || (t.TaiKhoan.Email != null && t.TaiKhoan.Email == studentCode));

            if (the == null)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                if (User.IsInRole("Librarian") || User.IsInRole("ThuThu"))
                {
                    return RedirectToAction("Index", "Borrow", new { area = "Admin" });
                }

                // Kiểm tra tài khoản người dùng trong bảng TaiKhoan
                var userAccount = await _context.TaiKhoans
                    .Include(t => t.TheThuVien)
                    .FirstOrDefaultAsync(t => t.Username == studentCode || (t.Email != null && t.Email == studentCode));

                if (userAccount != null)
                {
                    // Tự động khởi tạo thẻ thư viện nếu chưa có
                    userAccount.TheThuVien = new TheThuVien
                    {
                        SoThe = $"LIB-{userAccount.Username.Trim().ToUpper()}",
                        HoTen = !string.IsNullOrWhiteSpace(userAccount.FullName) ? userAccount.FullName : userAccount.Username,
                        NgayHetHan = DateTime.Today.AddYears(1),
                        TinhTrangSuDung = "Sử dụng được"
                    };
                    await _context.SaveChangesAsync();

                    the = await _context.TheThuViens
                        .Include(t => t.TaiKhoan)
                        .Include(t => t.PhieuMuons)
                            .ThenInclude(p => p.ChiTiets)
                                .ThenInclude(c => c.DauSach)
                                    .ThenInclude(d => d.Sach)
                        .FirstOrDefaultAsync(t => t.MaThe == userAccount.TheThuVien.MaThe);
                }
                
                if (the == null)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["Error"] = "Không tìm thấy hồ sơ thẻ thư viện. Vui lòng đăng nhập lại hoặc liên hệ thủ thư.";
                    return RedirectToAction(nameof(Login));
                }
            }

            var activeLoans = the.PhieuMuons
                .Where(p => p.TrangThai == "Đang mượn")
                .SelectMany(p => p.ChiTiets.Where(c => c.NgayTraThucTe == null).Select(c => new ActiveBorrowItemViewModel
                {
                    BorrowId = p.MaPhieuMuon,
                    BookCode = c.DauSach?.Sach?.MaSachCode ?? "",
                    ControlNumber = c.DauSach?.SoKiemSoat ?? "",
                    BookTitle = c.DauSach?.Sach?.TenSach ?? "",
                    Author = c.DauSach?.Sach?.TacGia ?? "",
                    CoverImageUrl = !string.IsNullOrEmpty(c.DauSach?.Sach?.HinhAnh) ? c.DauSach.Sach.HinhAnh : "/uploads/books/default-book.svg",
                    BorrowDate = p.NgayMuon,
                    DueDate = p.HanTra
                }))
                .ToList();

            var pendingLoans = the.PhieuMuons
                .Where(p => p.TrangThai == "Chờ duyệt")
                .SelectMany(p => p.ChiTiets.Select(c => new PendingBorrowItemViewModel
                {
                    BorrowId = p.MaPhieuMuon,
                    BookCode = c.DauSach?.Sach?.MaSachCode ?? "",
                    ControlNumber = c.DauSach?.SoKiemSoat ?? "",
                    BookTitle = c.DauSach?.Sach?.TenSach ?? "",
                    Author = c.DauSach?.Sach?.TacGia ?? "",
                    CoverImageUrl = !string.IsNullOrEmpty(c.DauSach?.Sach?.HinhAnh) ? c.DauSach.Sach.HinhAnh : "/uploads/books/default-book.svg",
                    RequestDate = p.NgayMuon,
                    DueDate = p.HanTra,
                    Status = p.TrangThai
                }))
                .ToList();

            var historyLoans = the.PhieuMuons
                .SelectMany(p => p.ChiTiets.Where(c => c.NgayTraThucTe != null).Select(c => new BorrowHistoryItemViewModel
                {
                    Id = c.MaChiTiet,
                    BookCode = c.DauSach?.Sach?.MaSachCode ?? "",
                    ControlNumber = c.DauSach?.SoKiemSoat ?? "",
                    BookTitle = c.DauSach?.Sach?.TenSach ?? "",
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
                StudentCode = the.TaiKhoan?.Username ?? studentCode,
                Email = the.TaiKhoan?.Email ?? $"{studentCode.ToLower()}@student.huflit.edu.vn",
                PhoneNumber = the.TaiKhoan?.PhoneNumber ?? "",
                AvatarUrl = !string.IsNullOrEmpty(the.TaiKhoan?.AvatarUrl) ? the.TaiKhoan.AvatarUrl : "/uploads/avatars/default-avatar.svg",
                CardNumber = the.SoThe,
                CardIssueDate = the.NgayHetHan.AddYears(-1),
                CardExpiryDate = the.NgayHetHan,
                CardStatus = the.TinhTrangSuDung,
                TotalUnpaidFines = historyLoans.Where(h => h.PaymentStatus == "Chưa thanh toán").Sum(h => h.CompensationFee),
                TotalFinesPaid = historyLoans.Where(h => h.PaymentStatus == "Đã thanh toán").Sum(h => h.CompensationFee),
                ActiveLoans = activeLoans,
                PendingLoans = pendingLoans,
                HistoryLoans = historyLoans
            };

            model.UpdateModel = new UpdateProfileInputModel
            {
                FullName = model.FullName,
                StudentCode = model.StudentCode,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            return View(model);
        }

        // POST: /Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileInputModel? input)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json");

            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? input?.StudentCode
                              ?? Request.Form["StudentCode"].FirstOrDefault()
                              ?? Request.Form["UpdateModel.StudentCode"].FirstOrDefault();

            var fullName = input?.FullName
                           ?? Request.Form["FullName"].FirstOrDefault()
                           ?? Request.Form["UpdateModel.FullName"].FirstOrDefault();

            var email = input?.Email
                        ?? Request.Form["Email"].FirstOrDefault()
                        ?? Request.Form["UpdateModel.Email"].FirstOrDefault();

            var phoneNumber = input?.PhoneNumber
                              ?? Request.Form["PhoneNumber"].FirstOrDefault()
                              ?? Request.Form["UpdateModel.PhoneNumber"].FirstOrDefault();

            bool isPrivilegedUser = User.IsInRole("Admin") || User.IsInRole("Librarian") || User.IsInRole("ThuThu");

            if (isPrivilegedUser && string.IsNullOrWhiteSpace(fullName))
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Vui lòng nhập Họ và tên độc giả." });
                }
                TempData["Error"] = "Vui lòng nhập Họ và tên độc giả.";
                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(studentCode))
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." });
                }
                TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Login));
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.TheThuVien)
                .FirstOrDefaultAsync(t => t.Username == studentCode || (t.Email != null && t.Email == studentCode));

            if (taiKhoan == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ tài khoản người dùng." });
                }
                TempData["Error"] = "Không tìm thấy hồ sơ tài khoản người dùng.";
                return RedirectToAction(nameof(Profile));
            }

            if (isPrivilegedUser && !string.IsNullOrWhiteSpace(fullName))
            {
                taiKhoan.FullName = fullName.Trim();
                if (taiKhoan.TheThuVien != null)
                {
                    taiKhoan.TheThuVien.HoTen = fullName.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                taiKhoan.Email = email.Trim();
            }
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                taiKhoan.PhoneNumber = phoneNumber.Trim();
            }

            await _context.SaveChangesAsync();

            // Cập nhật ngay Authentication Cookie với Claims mới để Navbar & Profile đồng bộ
            string role = taiKhoan.Role;
            string cardNumber = taiKhoan.TheThuVien?.SoThe ?? "";
            string cardStatus = taiKhoan.TheThuVien?.TinhTrangSuDung ?? "";
            string avatarUrl = !string.IsNullOrEmpty(taiKhoan.AvatarUrl) ? taiKhoan.AvatarUrl : "/uploads/avatars/default-avatar.svg";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, taiKhoan.FullName ?? taiKhoan.Username),
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.Username),
                new Claim(ClaimTypes.Role, role),
                new Claim("CardNumber", cardNumber),
                new Claim("CardStatus", cardStatus),
                new Claim("AvatarUrl", avatarUrl)
            };
            if (!string.IsNullOrEmpty(taiKhoan.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, taiKhoan.Email));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true });

            var successMsg = $"Cập nhật thông tin thành công cho độc giả {taiKhoan.FullName}!";
            if (isAjax)
            {
                return Json(new { success = true, message = successMsg, fullName = taiKhoan.FullName, email = taiKhoan.Email, phoneNumber = taiKhoan.PhoneNumber });
            }

            TempData["Success"] = successMsg;
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/UploadAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
        {
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                return RedirectToAction(nameof(Login));
            }

            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn tệp ảnh đại diện.";
                return RedirectToAction(nameof(Profile));
            }

            if (avatarFile.Length > 2 * 1024 * 1024) // 2MB
            {
                TempData["Error"] = "Dung lượng ảnh không được vượt quá 2MB.";
                return RedirectToAction(nameof(Profile));
            }

            var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Chỉ chấp nhận các định dạng ảnh: JPG, JPEG, PNG, WEBP.";
                return RedirectToAction(nameof(Profile));
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{studentCode}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{uniqueFileName}";

            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(tk => tk.Username == studentCode);
            if (taiKhoan != null)
            {
                taiKhoan.AvatarUrl = avatarUrl;
                await _context.SaveChangesAsync();

                // Cập nhật lại Claim Identity trong Cookie để ảnh hiển thị ngay lập tức
                if (User.Identity is ClaimsIdentity identity)
                {
                    var existingAvatarClaim = identity.FindFirst("AvatarUrl");
                    if (existingAvatarClaim != null)
                    {
                        identity.RemoveClaim(existingAvatarClaim);
                    }
                    identity.AddClaim(new Claim("AvatarUrl", avatarUrl));
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity),
                        new AuthenticationProperties { IsPersistent = true });
                }
            }

            TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/CancelBorrowRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBorrowRequest(int borrowId)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json");

            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                if (isAjax) return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                return RedirectToAction(nameof(Login));
            }

            var phieu = await _context.PhieuMuons
                .Include(p => p.The)
                    .ThenInclude(t => t.TaiKhoan)
                .Include(p => p.ChiTiets)
                    .ThenInclude(c => c.DauSach)
                        .ThenInclude(d => d.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == borrowId);

            if (phieu == null || phieu.The?.TaiKhoan?.Username != studentCode)
            {
                var msg = "Không tìm thấy yêu cầu mượn sách hoặc bạn không có quyền thao tác.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Profile));
            }

            if (phieu.TrangThai != "Chờ duyệt")
            {
                var msg = $"Không thể hủy yêu cầu này vì trạng thái hiện tại là '{phieu.TrangThai}'.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Profile));
            }

            // Hoàn lại tình trạng đầu sách về "Còn"
            string bookTitle = "tài liệu";
            foreach (var c in phieu.ChiTiets)
            {
                if (c.DauSach != null)
                {
                    c.DauSach.TinhTrang = "Còn";
                    if (c.DauSach.Sach != null)
                    {
                        bookTitle = c.DauSach.Sach.TenSach;
                    }
                }
            }

            // Cập nhật trạng thái phiếu mượn sang "Đã hủy"
            phieu.TrangThai = "Đã hủy";
            await _context.SaveChangesAsync();

            var successMsg = $"Đã hủy yêu cầu đăng ký mượn cuốn sách \"{bookTitle}\" (Mã phiếu #{borrowId}) thành công!";
            if (isAjax)
            {
                return Json(new { success = true, message = successMsg });
            }

            TempData["Success"] = successMsg;
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordInputModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json");

            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                if (isAjax) return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                 ?? "Vui lòng kiểm tra lại thông tin mật khẩu.";
                if (isAjax) return Json(new { success = false, message = firstError });
                TempData["Error"] = firstError;
                return RedirectToAction(nameof(Profile));
            }

            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(tk => tk.Username == studentCode);
            if (taiKhoan == null)
            {
                var msg = "Không tìm thấy tài khoản người dùng.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Profile));
            }

            // Kiểm tra mật khẩu hiện tại
            if (taiKhoan.PasswordHash != model.CurrentPassword)
            {
                var msg = "Mật khẩu hiện tại không chính xác. Vui lòng kiểm tra lại.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Profile));
            }

            // Cập nhật mật khẩu mới
            taiKhoan.PasswordHash = model.NewPassword;
            await _context.SaveChangesAsync();

            var successMsg = "Đổi mật khẩu thành công! Mật khẩu mới đã có hiệu lực.";
            if (isAjax)
            {
                return Json(new { success = true, message = successMsg });
            }

            TempData["Success"] = successMsg;
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ExtendBorrow  → tạo yêu cầu chờ thủ thư duyệt (tối đa 2 lần)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendBorrow(int borrowId, string? reason)
        {
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var phieu = await _context.PhieuMuons.FirstOrDefaultAsync(p =>
                p.MaPhieuMuon == borrowId && p.The.TaiKhoan.Username == studentCode && p.TrangThai == "Đang mượn");

            if (phieu == null)
                TempData["Error"] = $"Không tìm thấy phiếu mượn #{borrowId} đang hoạt động.";
            else if (await _context.YeuCauGiaHans.AnyAsync(g => g.MaPhieuMuon == borrowId && g.TrangThai == "Chờ duyệt"))
                TempData["Warning"] = "Phiếu này đã có yêu cầu gia hạn đang chờ duyệt.";
            else if (await _context.YeuCauGiaHans.CountAsync(g => g.MaPhieuMuon == borrowId && g.TrangThai == "Đã duyệt") >= 2)
                TempData["Error"] = "Mỗi phiếu chỉ được gia hạn tối đa 2 lần.";
            else
            {
                _context.YeuCauGiaHans.Add(new YeuCauGiaHan { MaPhieuMuon = borrowId, LyDo = reason ?? "" });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã gửi yêu cầu gia hạn phiếu #{borrowId}, chờ thủ thư duyệt.";
            }
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ReportDamageOrLost → ghi chú vào các bản sao đang mượn để thủ thư thấy
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportDamageOrLost(int borrowId, string issueType, string description)
        {
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var phieu = await _context.PhieuMuons
                .Include(p => p.ChiTiets).ThenInclude(c => c.DauSach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == borrowId && p.The.TaiKhoan.Username == studentCode && p.TrangThai == "Đang mượn");

            if (phieu == null)
            {
                TempData["Error"] = $"Không tìm thấy phiếu mượn #{borrowId} đang hoạt động.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var c in phieu.ChiTiets.Where(c => c.NgayTraThucTe == null))
                c.DauSach.GhiChu = $"{c.DauSach.GhiChu} [Độc giả báo {issueType} {DateTime.Today:dd/MM/yyyy}: {description}]".Trim();

            await _context.SaveChangesAsync();
            TempData["Warning"] = $"Đã ghi nhận báo '{issueType}' cho phiếu #{borrowId}. Thủ thư sẽ kiểm tra và hướng dẫn bồi thường.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/PayFine → thanh toán 1 khoản phạt/bồi thường
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(int chiTietId)
        {
            var studentCode = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ct = await _context.ChiTietPhieuMuons
                .Include(c => c.PhieuMuon).ThenInclude(p => p.The)
                .FirstOrDefaultAsync(c => c.MaChiTiet == chiTietId && c.PhieuMuon.The.TaiKhoan.Username == studentCode);

            if (ct == null || ct.TienBoiThuong <= 0 || ct.DaBoiThuong)
            {
                TempData["Error"] = "Khoản phạt không hợp lệ hoặc đã thanh toán.";
                return RedirectToAction(nameof(Profile));
            }

            ct.DaBoiThuong = true;
            var the = ct.PhieuMuon.The;

            // Hết nợ mất/hỏng thì thẻ có hiệu lực lại
            var conNo = await _context.ChiTietPhieuMuons.AnyAsync(c =>
                c.PhieuMuon.MaThe == the.MaThe && c.MaChiTiet != chiTietId && !c.DaBoiThuong &&
                c.TienBoiThuong > 0 && (c.TinhTrangTra == "Hỏng" || c.TinhTrangTra == "Mất"));
            if (!conNo && the.TinhTrangSuDung == "Không sử dụng")
                the.TinhTrangSuDung = "Sử dụng được";

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã thanh toán {ct.TienBoiThuong:N0} VNĐ.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ConfirmStudentPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmStudentPayment(string? note)
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
                .FirstOrDefaultAsync(t => t.TaiKhoan.Username == studentCode || (t.TaiKhoan.Email != null && t.TaiKhoan.Email == studentCode));

            if (the == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var unpaidFine = the.PhieuMuons
                .SelectMany(p => p.ChiTiets)
                .Where(c => !c.DaBoiThuong && c.TienBoiThuong > 0)
                .Sum(c => c.TienBoiThuong);

            // Gửi thông báo đến Ban Thủ Thư
            _context.ThongBaos.Add(new ThongBao
            {
                TargetUser = "AdminGroup",
                Title = "Độc giả gửi xác nhận thanh toán bồi thường",
                Message = $"Độc giả {the.HoTen} (Mã thẻ: {the.SoThe}, MSSV: {the.TaiKhoan.Username}) thông báo đã thanh toán số tiền {unpaidFine:N0} VNĐ tại quầy/chuyển khoản. Ghi chú: {note ?? "Thanh toán trực tiếp tại quầy cơ sở Hóc Môn"}.",
                LinkUrl = "/Admin/Borrow/Return",
                Type = "Warning",
                DaDoc = false,
                CreatedAt = DateTime.Now
            });

            // Gửi thông báo cho chính sinh viên
            _context.ThongBaos.Add(new ThongBao
            {
                TargetUser = the.TaiKhoan.Username,
                Title = "Đã gửi thông báo xác nhận thanh toán",
                Message = $"Hệ thống đã chuyển thông tin xác nhận thanh toán {unpaidFine:N0} VNĐ của bạn đến Thủ thư. Vui lòng chờ Thủ thư đối soát tại quầy để kích hoạt lại thẻ.",
                LinkUrl = "/Account/Profile",
                Type = "Info",
                DaDoc = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi thông báo xác nhận thanh toán tới Thủ thư thành công. Thủ thư sẽ đối soát và mở lại thẻ cho bạn.";
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