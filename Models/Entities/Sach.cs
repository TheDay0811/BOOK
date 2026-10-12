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
        public string Isbn { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string AnhBiaUrl => string.IsNullOrEmpty(Isbn)
            ? "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400&auto=format&fit=crop&q=80"
            : $"https://covers.openlibrary.org/b/isbn/{Isbn}-L.jpg?default=false";
        public int MaTheLoai { get; set; }
        public TheLoai TheLoai { get; set; } = null!;

        public int MaNXB { get; set; }
        public NhaXuatBan NhaXuatBan { get; set; } = null!;

        public List<DauSach> DauSachs { get; set; } = new();
    }
}
