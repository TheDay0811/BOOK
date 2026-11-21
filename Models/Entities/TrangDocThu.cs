using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class TrangDocThu
    {
        [Key]
        public int MaTrangDocThu { get; set; }

        public int MaSach { get; set; }
        public Sach Sach { get; set; } = null!;

        public int SoTrang { get; set; }

        [MaxLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        public string NoiDung { get; set; } = string.Empty;
    }
}
