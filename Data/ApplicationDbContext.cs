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
            base.OnModelCreating(modelBuilder);

            // 1. Tài Khoản - Thẻ Thư Viện (1 - 1)
            modelBuilder.Entity<TheThuVien>()
                .HasOne(t => t.TaiKhoan)
                .WithOne(tk => tk.TheThuVien)
                .HasForeignKey<TheThuVien>(t => t.MaTK)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Sách - Thể Loại (N - 1)
            modelBuilder.Entity<Sach>()
                .HasOne(s => s.TheLoai)
                .WithMany(t => t.Sachs)
                .HasForeignKey(s => s.MaTheLoai)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Sách - Nhà Xuất Bản (N - 1)
            modelBuilder.Entity<Sach>()
                .HasOne(s => s.NhaXuatBan)
                .WithMany(n => n.Sachs)
                .HasForeignKey(s => s.MaNXB)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Đầu Sách - Sách (N - 1)
            modelBuilder.Entity<DauSach>()
                .HasOne(d => d.Sach)
                .WithMany(s => s.DauSachs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Phiếu Mượn - Thẻ Thư Viện (N - 1)
            modelBuilder.Entity<PhieuMuon>()
                .HasOne(p => p.The)
                .WithMany(t => t.PhieuMuons)
                .HasForeignKey(p => p.MaThe)
                .OnDelete(DeleteBehavior.Restrict);

            // 6. Chi Tiết Phiếu Mượn - Phiếu Mượn (N - 1)
            modelBuilder.Entity<ChiTietPhieuMuon>()
                .HasOne(c => c.PhieuMuon)
                .WithMany(p => p.ChiTiets)
                .HasForeignKey(c => c.MaPhieuMuon)
                .OnDelete(DeleteBehavior.Cascade);

            // 7. Chi Tiết Phiếu Mượn - Đầu Sách (N - 1)
            modelBuilder.Entity<ChiTietPhieuMuon>()
                .HasOne(c => c.DauSach)
                .WithMany(d => d.ChiTietPhieuMuons)
                .HasForeignKey(c => c.MaDauSach)
                .OnDelete(DeleteBehavior.Restrict);

            // 8. Tiền bồi thường định dạng decimal(18,2)
            modelBuilder.Entity<ChiTietPhieuMuon>()
                .Property(c => c.TienBoiThuong)
                .HasColumnType("decimal(18,2)");
        }
    }
}