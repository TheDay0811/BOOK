using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class Sach
    {
        [Key] public int MaSach { get; set; }
        public string MaSachCode { get; set; } = string.Empty; // mã sách hiển thị, vd CS-001
        public string TenSach { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public int NamXuatBan { get; set; }
        public string? HinhAnh { get; set; }
        public string? MoTa { get; set; }
        public int SoTrang { get; set; } = 320;

        public int MaTheLoai { get; set; }
        public TheLoai TheLoai { get; set; } = null!;

        public int MaNXB { get; set; }
        public NhaXuatBan NhaXuatBan { get; set; } = null!;

        public List<DauSach> DauSachs { get; set; } = new();
        public List<TrangDocThu> TrangDocThus { get; set; } = new();
    }
}
