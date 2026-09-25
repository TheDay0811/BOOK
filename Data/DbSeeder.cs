using BOOK.Models.Entities;

namespace BOOK.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Sachs.Any()) return; // đã có dữ liệu rồi thì thôi

            var cntt = new TheLoai { TenTheLoai = "Công nghệ thông tin" };
            var kinhTe = new TheLoai { TenTheLoai = "Kinh tế" };
            var vanHoc = new TheLoai { TenTheLoai = "Văn học" };

            var nxbGiaoDuc = new NhaXuatBan { TenNXB = "NXB Giáo Dục" };
            var nxbKhkt = new NhaXuatBan { TenNXB = "NXB Khoa Học Kỹ Thuật" };

            var sachDb = new Sach
            {
                MaSachCode = "CS-001",
                TenSach = "Database System Concepts",
                TacGia = "Silberschatz",
                NamXuatBan = 2022,
                TheLoai = cntt,
                NhaXuatBan = nxbKhkt,
                DauSachs = new List<DauSach>
                {
                    new DauSach { SoKiemSoat = "001", TinhTrang = "Còn" },
                    new DauSach { SoKiemSoat = "002", TinhTrang = "Còn" },
                    new DauSach { SoKiemSoat = "003", TinhTrang = "Cho mượn" }
                }
            };

            var sachKt = new Sach
            {
                MaSachCode = "KT-001",
                TenSach = "Kinh Tế Vi Mô",
                TacGia = "N. Gregory Mankiw",
                NamXuatBan = 2020,
                TheLoai = kinhTe,
                NhaXuatBan = nxbGiaoDuc,
                DauSachs = new List<DauSach>
                {
                    new DauSach { SoKiemSoat = "001", TinhTrang = "Còn" }
                }
            };

            var sachVh = new Sach
            {
                MaSachCode = "VH-001",
                TenSach = "Truyện Kiều",
                TacGia = "Nguyễn Du",
                NamXuatBan = 2019,
                TheLoai = vanHoc,
                NhaXuatBan = nxbGiaoDuc,
                DauSachs = new List<DauSach>
                {
                    new DauSach { SoKiemSoat = "001", TinhTrang = "Hỏng" },
                    new DauSach { SoKiemSoat = "002", TinhTrang = "Còn" }
                }
            };

            var admin = new TaiKhoan { Username = "admin", PasswordHash = "admin123", Role = "Admin" };
            var thuThu = new TaiKhoan { Username = "thuthu", PasswordHash = "thuthu123", Role = "ThuThu" };
            var taiKhoanSv = new TaiKhoan { Username = "sv001", PasswordHash = "123456", Role = "SinhVien" };

            var theSv = new TheThuVien
            {
                SoThe = "LIB-2026-0001",
                HoTen = "Nguyễn Văn A",
                NgayHetHan = DateTime.Today.AddYears(1),
                TinhTrangSuDung = "Sử dụng được",
                TaiKhoan = taiKhoanSv
            };

            var phieuMuon = new PhieuMuon
            {
                NgayMuon = DateTime.Today.AddDays(-5),
                HanTra = DateTime.Today.AddDays(10),
                TrangThai = "Đang mượn",
                The = theSv,
                ChiTiets = new List<ChiTietPhieuMuon>
                {
                    new ChiTietPhieuMuon
                    {
                        DauSach = sachDb.DauSachs[2], // bản 003 đang cho mượn
                        TinhTrangTra = "Bình thường"
                    }
                }
            };

            context.AddRange(admin, thuThu, phieuMuon, sachDb, sachKt, sachVh);
            context.SaveChanges();
        }
    }
}