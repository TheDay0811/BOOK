using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập hoặc Email.")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
