using System;
using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class YeuThich
    {
        [Key]
        public int MaYeuThich { get; set; }

        public int MaTK { get; set; }
        public TaiKhoan TaiKhoan { get; set; } = null!;

        public int MaSach { get; set; }
        public Sach Sach { get; set; } = null!;

        public DateTime NgayThem { get; set; } = DateTime.Now;
    }
}
