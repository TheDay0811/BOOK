using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOOK.Models.Entities
{
    public class TheThuVien
    {
        [Key] public int MaThe { get; set; }
        public string SoThe { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public DateTime NgayHetHan { get; set; }
        public string TinhTrangSuDung { get; set; } = "Sử dụng được";


        public int MaTK { get; set; }
        public TaiKhoan TaiKhoan { get; set; } = null!;

        public List<PhieuMuon> PhieuMuons { get; set; } = new();
    }
}
