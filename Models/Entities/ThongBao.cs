using System;
using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.Entities
{
    public class ThongBao
    {
        [Key]
        public int MaThongBao { get; set; }

        /// <summary>
        /// Username người nhận (Ví dụ: "admin", "thuthu", MSSV sinh viên "24DH112970", hoặc "ALL")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TargetUser { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(255)]
        public string? LinkUrl { get; set; }

        [StringLength(50)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Danger

        public bool DaDoc { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
