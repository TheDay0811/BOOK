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
            modelBuilder.Entity<Sach>()
                .HasOne(s => s.TheLoai).WithMany(t => t.Sachs).HasForeignKey(s => s.MaTheLoai);

            modelBuilder.Entity<Sach>()
                .HasOne(s => s.NhaXuatBan).WithMany(n => n.Sachs).HasForeignKey(s => s.MaNXB);

            modelBuilder.Entity<DauSach>()
                .HasOne(d => d.Sach).WithMany(s => s.DauSachs).HasForeignKey(d => d.MaSach);

            modelBuilder.Entity<PhieuMuon>()
                .HasOne(p => p.The).WithMany(t => t.PhieuMuons).HasForeignKey(p => p.MaThe);

            modelBuilder.Entity<ChiTietPhieuMuon>()
                .HasOne(c => c.PhieuMuon).WithMany(p => p.ChiTiets).HasForeignKey(c => c.MaPhieuMuon);

            modelBuilder.Entity<ChiTietPhieuMuon>()
                .HasOne(c => c.DauSach).WithMany(d => d.ChiTietPhieuMuons).HasForeignKey(c => c.MaDauSach);
        }
    }
}