using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class PhieuMuon
    {
        [Key] public int MaPhieuMuon { get; set; }
        public DateTime NgayMuon { get; set; } = DateTime.Today;
        public DateTime HanTra { get; set; } = DateTime.Today.AddDays(15);
        public string TrangThai { get; set; } = "Đang mượn"; // Đang mượn / Đã trả

        public int MaThe { get; set; }
        public TheThuVien The { get; set; } = null!;

        public List<ChiTietPhieuMuon> ChiTiets { get; set; } = new();
    }
}
