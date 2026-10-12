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

        public static void SeedBooks(ApplicationDbContext context)
        {
            // Thêm/sửa sách: thêm hoặc sửa 1 dòng ở đây (Mã, Tên, Tác giả, Năm, Thể loại, NXB, Số bản sao), rồi F5
            var books = new (string Code, string Title, string Author, int Year, string Category, string Publisher, int Copies)[]
            {
                // Công nghệ thông tin
                ("CS-002", "Clean Code: A Handbook of Agile Software Craftsmanship", "Robert C. Martin", 2008, "Công nghệ thông tin", "Prentice Hall", 3),
                ("CS-003", "The Pragmatic Programmer (20th Anniversary Edition)", "Andrew Hunt, David Thomas", 2019, "Công nghệ thông tin", "Addison-Wesley", 2),
                ("CS-004", "Introduction to Algorithms (4th Edition)", "Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein", 2022, "Công nghệ thông tin", "The MIT Press", 3),
                ("CS-005", "Design Patterns: Elements of Reusable Object-Oriented Software", "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides", 1994, "Công nghệ thông tin", "Addison-Wesley", 2),
                ("CS-006", "Refactoring (2nd Edition)", "Martin Fowler", 2018, "Công nghệ thông tin", "Addison-Wesley", 2),
                ("CS-007", "Code Complete (2nd Edition)", "Steve McConnell", 2004, "Công nghệ thông tin", "Microsoft Press", 2),
                ("CS-008", "Computer Networking: A Top-Down Approach (8th Edition)", "James F. Kurose, Keith W. Ross", 2021, "Công nghệ thông tin", "Pearson", 3),
                ("CS-009", "Operating System Concepts (10th Edition)", "Abraham Silberschatz, Peter B. Galvin, Greg Gagne", 2018, "Công nghệ thông tin", "Wiley", 3),
                ("CS-010", "Artificial Intelligence: A Modern Approach (4th Edition)", "Stuart Russell, Peter Norvig", 2020, "Công nghệ thông tin", "Pearson", 2),
                ("CS-011", "Software Engineering (10th Edition)", "Ian Sommerville", 2016, "Công nghệ thông tin", "Pearson", 3),
                ("CS-012", "Designing Data-Intensive Applications", "Martin Kleppmann", 2017, "Công nghệ thông tin", "O'Reilly Media", 2),
                ("CS-013", "Head First Design Patterns (2nd Edition)", "Eric Freeman, Elisabeth Robson", 2020, "Công nghệ thông tin", "O'Reilly Media", 2),
                ("CS-014", "C# in Depth (4th Edition)", "Jon Skeet", 2019, "Công nghệ thông tin", "Manning Publications", 2),
                ("CS-015", "The Mythical Man-Month (Anniversary Edition)", "Frederick P. Brooks Jr.", 1995, "Công nghệ thông tin", "Addison-Wesley", 2),
                ("CS-016", "Python Crash Course (3rd Edition)", "Eric Matthes", 2023, "Công nghệ thông tin", "No Starch Press", 2),
                ("CS-017", "Fundamentals of Database Systems (7th Edition)", "Ramez Elmasri, Shamkant B. Navathe", 2016, "Công nghệ thông tin", "Pearson", 3),
                // Kinh tế
                ("KT-002", "Principles of Economics (9th Edition)", "N. Gregory Mankiw", 2020, "Kinh tế", "Cengage Learning", 3),
                ("KT-003", "Marketing Management (15th Edition)", "Philip Kotler, Kevin Lane Keller", 2016, "Kinh tế", "Pearson", 2),
                ("KT-004", "Good to Great", "Jim Collins", 2001, "Kinh tế", "HarperBusiness", 2),
                ("KT-005", "Thinking, Fast and Slow", "Daniel Kahneman", 2011, "Kinh tế", "Farrar, Straus and Giroux", 2),
                ("KT-006", "The Lean Startup", "Eric Ries", 2011, "Kinh tế", "Crown Business", 2),
                ("KT-007", "Principles of Corporate Finance (13th Edition)", "Richard A. Brealey, Stewart C. Myers, Franklin Allen", 2020, "Kinh tế", "McGraw-Hill", 2),
                // Văn học (năm = năm xuất bản lần đầu, sửa theo bản thật của thư viện)
                ("VH-002", "Dế Mèn phiêu lưu ký", "Tô Hoài", 1941, "Văn học", "NXB Kim Đồng", 3),
                ("VH-003", "Tắt đèn", "Ngô Tất Tố", 1939, "Văn học", "NXB Văn Học", 2),
                ("VH-004", "Chí Phèo", "Nam Cao", 1941, "Văn học", "NXB Văn Học", 2),
                ("VH-005", "Cho tôi xin một vé đi tuổi thơ", "Nguyễn Nhật Ánh", 2008, "Văn học", "NXB Trẻ", 3),
                // Ngoại ngữ
                ("NN-001", "English Grammar in Use (5th Edition)", "Raymond Murphy", 2019, "Ngoại ngữ", "Cambridge University Press", 3),
                ("NN-002", "Practical English Usage (4th Edition)", "Michael Swan", 2016, "Ngoại ngữ", "Oxford University Press", 2),
                ("NN-003", "Oxford Advanced Learner's Dictionary (10th Edition)", "A S Hornby", 2020, "Ngoại ngữ", "Oxford University Press", 2),
            };

            // ISBN để lấy ảnh bìa (sách chưa biết ISBN thì không cần khai báo, sẽ dùng ảnh mặc định)
            var isbns = new Dictionary<string, string>
            {
                ["CS-001"] = "9780078022159",
                ["CS-002"] = "9780132350884",
                ["CS-003"] = "9780135957059",
                ["CS-004"] = "9780262046305",
                ["CS-005"] = "9780201633610",
                ["CS-006"] = "9780134757599",
                ["CS-007"] = "9780735619678",
                ["CS-010"] = "9780134610993",
                ["CS-011"] = "9780133943030",
                ["CS-012"] = "9781449373320",
                ["CS-013"] = "9781492078005",
                ["CS-014"] = "9781617294532",
                ["CS-015"] = "9780201835953",
                ["CS-016"] = "9781718502703",
                ["CS-017"] = "9780133970777",
                ["KT-003"] = "9780133856460",
                ["KT-004"] = "9780066620992",
                ["KT-005"] = "9780374275631",
                ["KT-006"] = "9780307887894",
                ["NN-001"] = "9781108457651",
                ["NN-002"] = "9780194202442",
            };

            foreach (var (code, title, author, year, category, publisher, copies) in books)
            {
                var theLoai = context.TheLoais.FirstOrDefault(t => t.TenTheLoai == category)
                              ?? new TheLoai { TenTheLoai = category };
                var nxb = context.NhaXuatBans.FirstOrDefault(n => n.TenNXB == publisher)
                          ?? new NhaXuatBan { TenNXB = publisher };

                // Chưa có mã sách này thì tạo mới, có rồi thì cập nhật thông tin
                var sach = context.Sachs.FirstOrDefault(s => s.MaSachCode == code);
                if (sach == null)
                {
                    sach = new Sach { MaSachCode = code };
                    context.Sachs.Add(sach);
                }

                sach.TenSach = title;
                sach.TacGia = author;
                sach.NamXuatBan = year;
                sach.TheLoai = theLoai;
                sach.NhaXuatBan = nxb;
                sach.Isbn = isbns.GetValueOrDefault(code, "");
                context.SaveChanges();

                // Chỉ thêm bản sao nếu thiếu, không bao giờ xóa bản sao
                var have = context.DauSachs.Count(d => d.MaSach == sach.MaSach);
                for (int i = have + 1; i <= copies; i++)
                {
                    context.DauSachs.Add(new DauSach { MaSach = sach.MaSach, SoKiemSoat = i.ToString("000"), TinhTrang = "Còn" });
                }
                context.SaveChanges();
            }

            // Gán ISBN cho sách mẫu có sẵn từ hàm Seed (CS-001...)
            foreach (var (code, isbn) in isbns)
            {
                var existing = context.Sachs.FirstOrDefault(x => x.MaSachCode == code);
                if (existing != null && existing.Isbn != isbn)
                {
                    existing.Isbn = isbn;
                }
            }
            context.SaveChanges();
        }
    }
}