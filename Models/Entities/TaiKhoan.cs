using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class TaiKhoan
    {
        [Key] public int MaTK { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "SinhVien"; // Admin / Librarian (ThuThu) / SinhVien

        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        public TheThuVien? TheThuVien { get; set; }
    }
}
