using System.ComponentModel.DataAnnotations;
namespace BOOK.Models.Entities
{
    public class TheLoai
    {
        [Key] public int MaTheLoai { get; set; }
        public string TenTheLoai { get; set; } = string.Empty;

        public List<Sach> Sachs { get; set; } = new();
    }
}
