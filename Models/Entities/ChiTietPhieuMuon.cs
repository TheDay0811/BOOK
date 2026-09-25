using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOOK.Models.Entities
{
    public class ChiTietPhieuMuon
    {
        [Key]
        public int MaChiTiet { get; set; }
        public DateTime? NgayTraThucTe { get; set; }
        public string TinhTrangTra { get; set; } = "Bình thường";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienBoiThuong { get; set; } = 0;

        public bool DaBoiThuong { get; set; } = false;

        public int MaPhieuMuon { get; set; }
        public PhieuMuon PhieuMuon { get; set; } = null!;

        public int MaDauSach { get; set; }
        public DauSach DauSach { get; set; } = null!;
    }
}