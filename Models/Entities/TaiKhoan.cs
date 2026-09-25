using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class TaiKhoan
    {
        [Key] public int MaTK { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "SinhVien"; // Admin / ThuThu / SinhVien

        public TheThuVien? TheThuVien { get; set; }
    }
}
