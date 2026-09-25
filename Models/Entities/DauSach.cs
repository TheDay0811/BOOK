using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class DauSach
    {
        [Key] public int MaDauSach { get; set; }
        public string SoKiemSoat { get; set; } = "001"; // 001, 002, 003...
        public string TinhTrang { get; set; } = "Còn";  // Còn / Cho mượn / Mất / Hỏng
        public string GhiChu { get; set; } = string.Empty;

        public int MaSach { get; set; }
        public Sach Sach { get; set; } = null!;

        public List<ChiTietPhieuMuon> ChiTietPhieuMuons { get; set; } = new();
    }
}
