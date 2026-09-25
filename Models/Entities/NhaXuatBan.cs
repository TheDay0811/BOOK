using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class NhaXuatBan
    {
        [Key] public int MaNXB { get; set; }
        public string TenNXB { get; set; } = string.Empty;

        public List<Sach> Sachs { get; set; } = new();
    }
}
