using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class DanhGia
    {
        [Key] public int MaDanhGia { get; set; }
        public int MaSach { get; set; }
        public Sach Sach { get; set; } = null!;
        public int MaTK { get; set; }
        public TaiKhoan TaiKhoan { get; set; } = null!;
        public int SoSao { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    public class YeuCauGiaHan
    {
        [Key] public int MaYeuCau { get; set; }
        public int MaPhieuMuon { get; set; }
        public PhieuMuon PhieuMuon { get; set; } = null!;
        public string LyDo { get; set; } = string.Empty;
        public DateTime NgayYeuCau { get; set; } = DateTime.Now;
        public string TrangThai { get; set; } = "Chờ duyệt";
    }
}
