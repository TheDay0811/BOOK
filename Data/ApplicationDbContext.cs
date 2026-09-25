using BOOK.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOOK.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TheLoai> TheLoais => Set<TheLoai>();
        public DbSet<NhaXuatBan> NhaXuatBans => Set<NhaXuatBan>();
        public DbSet<Sach> Sachs => Set<Sach>();
        public DbSet<DauSach> DauSachs => Set<DauSach>();
        public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
        public DbSet<TheThuVien> TheThuViens => Set<TheThuVien>();
        public DbSet<PhieuMuon> PhieuMuons => Set<PhieuMuon>();
        public DbSet<ChiTietPhieuMuon> ChiTietPhieuMuons => Set<ChiTietPhieuMuon>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TheThuVien>()
                .HasOne(t => t.TaiKhoan)
                .WithOne(tk => tk.TheThuVien)
                .HasForeignKey<TheThuVien>(t => t.MaTK);
        }
    }
}