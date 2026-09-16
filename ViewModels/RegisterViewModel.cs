using System;
using System.ComponentModel.DataAnnotations;

namespace BOOK.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên sinh viên.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mã số sinh viên (MSSV).")]
        [RegularExpression(@"^[a-zA-Z0-9]{6,15}$", ErrorMessage = "MSSV phải từ 6 đến 15 ký tự chữ hoặc số (ví dụ: 22110098).")]
        [Display(Name = "Mã số sinh viên (MSSV)")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng (ví dụ: sinhvien@university.edu.vn).")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại liên hệ.")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 chữ số, bắt đầu bằng 03, 05, 07, 08, 09).")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng thiết lập Mật khẩu đăng nhập.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có độ dài từ 6 đến 100 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận lại Mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần tích chọn đồng ý với Nội quy và Điều khoản của Thư viện.")]
        [Display(Name = "Đồng ý điều khoản")]
        public bool AcceptTerms { get; set; }

        // ================= THÔNG TIN THẺ THƯ VIỆN TỰ ĐỘNG LIÊN KẾT =================
        [Display(Name = "Số thẻ thư viện")]
        public string CardNumber { get; set; } = string.Empty;

        [Display(Name = "Ngày cấp thẻ")]
        [DataType(DataType.Date)]
        public DateTime CardIssueDate { get; set; } = DateTime.Today;

        [Display(Name = "Ngày hết hạn thẻ")]
        [DataType(DataType.Date)]
        public DateTime CardExpiryDate { get; set; } = DateTime.Today.AddYears(1);

        [Display(Name = "Tình trạng thẻ")]
        public string CardStatus { get; set; } = "Sử dụng được";

        // Helper hiển thị mã thẻ tự động theo quy tắc LIB-{MSSV}
        public string DisplayCardNumber => !string.IsNullOrWhiteSpace(StudentCode)
            ? $"LIB-{StudentCode.Trim().ToUpper()}"
            : (!string.IsNullOrWhiteSpace(CardNumber) ? CardNumber : "LIB-CHƯA CẤP");
    }
}
