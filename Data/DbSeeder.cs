using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BOOK.Models.Entities;

namespace BOOK.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            // =========================================================================
            // BƯỚC 1: LÀM SẠCH DỮ LIỆU DEMO CŨ NẾU CÒN TỒN TẠI (CHỈ XÓA DỮ LIỆU DEMO)
            // =========================================================================
            var demoUsernames = new[] { "sv001", "sv002", "testuser", "demo" };
            var oldDemoAccounts = context.TaiKhoans
                .Where(t => demoUsernames.Contains(t.Username) && t.Role != "Admin" && t.Role != "Librarian")
                .ToList();

            if (oldDemoAccounts.Any())
            {
                var demoUserIds = oldDemoAccounts.Select(a => a.MaTK).ToList();
                var demoCards = context.TheThuViens
                    .Where(t => demoUserIds.Contains(t.MaTK))
                    .ToList();

                if (demoCards.Any())
                {
                    var demoCardIds = demoCards.Select(c => c.MaThe).ToList();
                    var demoLoans = context.PhieuMuons
                        .Where(p => demoCardIds.Contains(p.MaThe))
                        .ToList();

                    if (demoLoans.Any())
                    {
                        var demoLoanIds = demoLoans.Select(l => l.MaPhieuMuon).ToList();
                        var demoDetails = context.ChiTietPhieuMuons
                            .Where(d => demoLoanIds.Contains(d.MaPhieuMuon))
                            .ToList();

                        context.ChiTietPhieuMuons.RemoveRange(demoDetails);
                        context.PhieuMuons.RemoveRange(demoLoans);
                    }
                    context.TheThuViens.RemoveRange(demoCards);
                }
                context.TaiKhoans.RemoveRange(oldDemoAccounts);
                context.SaveChanges();
            }

            // Xóa các sách mẫu ảo cũ (VH-001, CS-001, CS-002, AI-001, SE-001)
            var mockBookCodes = new[] { "VH-001", "CS-001", "CS-002", "AI-001", "SE-001" };
            var mockBooks = context.Sachs
                .Include(s => s.DauSachs)
                .Where(s => mockBookCodes.Contains(s.MaSachCode))
                .ToList();
            if (mockBooks.Any())
            {
                foreach (var mb in mockBooks)
                {
                    context.DauSachs.RemoveRange(mb.DauSachs);
                }
                context.Sachs.RemoveRange(mockBooks);
                context.SaveChanges();
            }

            // Đảm bảo mọi tài khoản Sinh Viên thật đã đăng ký đều có Thẻ thư viện liên kết
            var studentAccountsWithoutCard = context.TaiKhoans
                .Include(t => t.TheThuVien)
                .Where(t => t.Role == "SinhVien" && t.TheThuVien == null)
                .ToList();

            foreach (var sv in studentAccountsWithoutCard)
            {
                sv.TheThuVien = new TheThuVien
                {
                    SoThe = $"LIB-{sv.Username.Trim().ToUpper()}",
                    HoTen = !string.IsNullOrWhiteSpace(sv.FullName) ? sv.FullName : sv.Username,
                    NgayHetHan = DateTime.Today.AddYears(1),
                    TinhTrangSuDung = "Sử dụng được"
                };
            }
            if (studentAccountsWithoutCard.Any())
            {
                context.SaveChanges();
            }

            // =========================================================================
            // BƯỚC 2: KHỞI TẠO DUY NHẤT 2 TÀI KHOẢN HỆ THỐNG HUFLIT
            // =========================================================================
            // 1. Tài khoản Admin tối cao
            var admin = context.TaiKhoans.FirstOrDefault(t => t.Username == "admin" || t.Username == "admin@huflit.edu.vn" || t.Email == "admin@huflit.edu.vn" || t.Role == "Admin");
            if (admin == null)
            {
                admin = new TaiKhoan
                {
                    Username = "admin",
                    Email = "admin@huflit.edu.vn",
                    FullName = "Quản Trị Hệ Thống",
                    PasswordHash = "Admin@123456",
                    Role = "Admin",
                    PhoneNumber = "02838632052",
                    AvatarUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&auto=format&fit=crop&q=80"
                };
                context.TaiKhoans.Add(admin);
            }
            else
            {
                admin.Username = "admin";
                admin.Email = "admin@huflit.edu.vn";
                admin.FullName = "Quản Trị Hệ Thống";
                admin.PasswordHash = "Admin@123456";
                admin.Role = "Admin";
            }

            // 2. Tài khoản Thủ thư HUFLIT
            var librarian = context.TaiKhoans.FirstOrDefault(t => t.Username == "thuthu" || t.Username == "thuthu@huflit.edu.vn" || t.Email == "thuthu@huflit.edu.vn" || t.Role == "Librarian");
            if (librarian == null)
            {
                librarian = new TaiKhoan
                {
                    Username = "thuthu",
                    Email = "thuthu@huflit.edu.vn",
                    FullName = "Thủ Thư HUFLIT",
                    PasswordHash = "Librarian@123456",
                    Role = "Librarian",
                    PhoneNumber = "02838632052",
                    AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80"
                };
                context.TaiKhoans.Add(librarian);
            }
            else
            {
                librarian.Username = "thuthu";
                librarian.Email = "thuthu@huflit.edu.vn";
                librarian.FullName = "Thủ Thư HUFLIT";
                librarian.PasswordHash = "Librarian@123456";
            }

            context.SaveChanges();

            // =========================================================================
            // BƯỚC 3: KHỞI TẠO DANH MỤC THỂ LOẠI & NHÀ XUẤT BẢN THỰC TẾ
            // =========================================================================
            var categories = new Dictionary<string, TheLoai>
            {
                ["CNTT"] = GetOrCreateCategory(context, "Công nghệ thông tin & Lập trình"),
                ["NgoaiNgu"] = GetOrCreateCategory(context, "Ngoại ngữ & Ngôn ngữ học"),
                ["KinhTe"] = GetOrCreateCategory(context, "Kinh tế & Quản trị kinh doanh"),
                ["Luat"] = GetOrCreateCategory(context, "Luật & Quan hệ quốc tế"),
                ["DuLich"] = GetOrCreateCategory(context, "Du lịch & Khách sạn"),
                ["KhoaHoc"] = GetOrCreateCategory(context, "Khoa học cơ bản & Toán ứng dụng")
            };

            var publishers = new Dictionary<string, NhaXuatBan>
            {
                ["GiaoDuc"] = GetOrCreatePublisher(context, "NXB Giáo Dục Việt Nam"),
                ["KHKT"] = GetOrCreatePublisher(context, "NXB Khoa Học & Kỹ Thuật"),
                ["DHQG"] = GetOrCreatePublisher(context, "NXB Đại Học Quốc Gia"),
                ["Tre"] = GetOrCreatePublisher(context, "NXB Trẻ"),
                ["Cambridge"] = GetOrCreatePublisher(context, "Cambridge University Press")
            };

            context.SaveChanges();

            // =========================================================================
            // BƯỚC 4: KHỞI TẠO ĐẦU SÁCH & BẢN SAO VẬT LÝ THEO SỐ KIỂM SOÁT
            // =========================================================================
            SeedBook(context, "IT-001", "Database System Concepts (Ấn bản thứ 7)", "Abraham Silberschatz & Henry F. Korth", 2022, categories["CNTT"], publishers["KHKT"],
                "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kho lưu thông Hóc Môn"), ("002", "Còn", "Kho lưu thông Hóc Môn"), ("003", "Còn", "Phòng đọc tại chỗ") });

            SeedBook(context, "IT-002", "Clean Code: A Handbook of Agile Software Craftsmanship", "Robert C. Martin", 2021, categories["CNTT"], publishers["KHKT"],
                "https://images.unsplash.com/photo-1532012164546-f432f2e3777f?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kho lưu thông Hóc Môn"), ("002", "Còn", "Kho lưu thông Hóc Môn") });

            SeedBook(context, "NN-001", "Cambridge IELTS 18 Academic with Answers", "Cambridge University Press", 2023, categories["NgoaiNgu"], publishers["Cambridge"],
                "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kệ sách luyện thi IELTS"), ("002", "Còn", "Kệ sách luyện thi IELTS"), ("003", "Còn", "Phòng tự học ngoại ngữ") });

            SeedBook(context, "NN-002", "Ngữ Pháp Tiếng Anh Nâng Cao (Advanced English Grammar)", "TS. Nguyễn Quốc Tuấn", 2022, categories["NgoaiNgu"], publishers["GiaoDuc"],
                "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kho giáo trình cơ bản"), ("002", "Còn", "Kho giáo trình cơ bản") });

            SeedBook(context, "KT-001", "Kinh Tế Vi Mô (Giáo Trình Chuẩn HUFLIT)", "N. Gregory Mankiw", 2023, categories["KinhTe"], publishers["GiaoDuc"],
                "https://images.unsplash.com/photo-1592496431122-2349e0fbc666?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kho sách chuyên ngành Kinh tế"), ("002", "Còn", "Kho sách chuyên ngành Kinh tế") });

            SeedBook(context, "KT-002", "Quản Trị Học Hiện Đại Trong Kỷ Nguyên Số", "Stephen P. Robbins & Mary Coulter", 2023, categories["KinhTe"], publishers["Tre"],
                "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kho sách Quản trị"), ("002", "Còn", "Kho sách Quản trị") });

            SeedBook(context, "DL-001", "Quản Trị Nhà Hàng Khách Sạn & Dịch Vụ Du Lịch", "PGS. TS. Trần Đình Phùng", 2022, categories["DuLich"], publishers["DHQG"],
                "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=600&auto=format&fit=crop&q=80",
                new[] { ("001", "Còn", "Kho tài liệu Du lịch"), ("002", "Còn", "Kho tài liệu Du lịch") });

            SeedBook(context, "IT-003", "Trí Tuệ Nhân Tạo & Học Máy Thực Hành Với Python", "Aurélien Géron", 2024, categories["CNTT"], publishers["KHKT"],
                "/uploads/books/book_f51d322d2798414d8380fbc196cf4515.jpeg",
                new[] { ("001", "Còn", "Kho công nghệ cao"), ("002", "Còn", "Kho công nghệ cao") });

            context.SaveChanges();
        }

        private static TheLoai GetOrCreateCategory(ApplicationDbContext context, string name)
        {
            var item = context.TheLoais.FirstOrDefault(t => t.TenTheLoai == name);
            if (item == null)
            {
                item = new TheLoai { TenTheLoai = name };
                context.TheLoais.Add(item);
                context.SaveChanges();
            }
            return item;
        }

        private static NhaXuatBan GetOrCreatePublisher(ApplicationDbContext context, string name)
        {
            var item = context.NhaXuatBans.FirstOrDefault(n => n.TenNXB == name);
            if (item == null)
            {
                item = new NhaXuatBan { TenNXB = name };
                context.NhaXuatBans.Add(item);
                context.SaveChanges();
            }
            return item;
        }

        private static void SeedBook(ApplicationDbContext context, string code, string title, string author, int year, TheLoai category, NhaXuatBan publisher, string? imageUrl, (string sokiemsoat, string tinhtrang, string ghichu)[] copies)
        {
            var sach = context.Sachs.Include(s => s.DauSachs).FirstOrDefault(s => s.MaSachCode == code);
            if (sach == null)
            {
                sach = new Sach
                {
                    MaSachCode = code,
                    TenSach = title,
                    TacGia = author,
                    NamXuatBan = year,
                    MaTheLoai = category.MaTheLoai,
                    MaNXB = publisher.MaNXB,
                    HinhAnh = imageUrl,
                    DauSachs = copies.Select(c => new DauSach
                    {
                        SoKiemSoat = c.sokiemsoat,
                        TinhTrang = c.tinhtrang,
                        GhiChu = c.ghichu
                    }).ToList()
                };
                context.Sachs.Add(sach);
            }
            else
            {
                sach.TenSach = title;
                sach.TacGia = author;
                sach.NamXuatBan = year;
                sach.MaTheLoai = category.MaTheLoai;
                sach.MaNXB = publisher.MaNXB;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    sach.HinhAnh = imageUrl;
                }

                foreach (var c in copies)
                {
                    var existingCopy = sach.DauSachs.FirstOrDefault(d => d.SoKiemSoat == c.sokiemsoat);
                    if (existingCopy == null)
                    {
                        sach.DauSachs.Add(new DauSach
                        {
                            SoKiemSoat = c.sokiemsoat,
                            TinhTrang = c.tinhtrang,
                            GhiChu = c.ghichu
                        });
                    }
                    else
                    {
                        existingCopy.TinhTrang = c.tinhtrang;
                        existingCopy.GhiChu = c.ghichu;
                    }
                }
            }
        }
    }
}