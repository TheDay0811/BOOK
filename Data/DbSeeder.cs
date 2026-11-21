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

            SeedBookDescriptionsAndPreviews(context);
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

        private static void SeedBookDescriptionsAndPreviews(ApplicationDbContext context)
        {
            var metadataMap = new Dictionary<string, (string Description, int PageCount, (int Page, string Title, string Content)[] Pages)>
            {
                ["IT-001"] = (
                    "Giáo trình kinh điển về hệ quản trị cơ sở dữ liệu quan hệ, mô hình thực thể kết hợp (ER), đại số quan hệ, SQL chuẩn hóa, giao dịch ACID và tối ưu hóa truy vấn.",
                    1376,
                    new[]
                    {
                        (1, "Lời nói đầu & Giới thiệu giáo trình", "Database System Concepts (Ấn bản thứ 7) là tài liệu nền tảng dành cho sinh viên ngành Công nghệ Thông tin tại HUFLIT.\n\nCuốn sách trình bày có hệ thống từ các khái niệm cốt lõi về lưu trữ dữ liệu, mô hình quan hệ, ngôn ngữ truy vấn SQL cho tới thiết kế lược đồ, chỉ mục B+-Tree, kiểm soát đồng thời (Concurrency Control) và phục hồi dữ liệu sau sự cố.\n\nMỗi chương đều đi kèm ví dụ cơ sở dữ liệu trường đại học trực quan, giúp người học dễ dàng liên hệ giữa lý thuyết và thực hành triển khai trên SQL Server hoặc PostgreSQL."),
                        (2, "Mục lục tóm tắt (Phần I & II)", "PHẦN I: CƠ SỞ DỮ LIỆU QUAN HỆ\n• Chương 1: Giới thiệu tổng quan về Hệ cơ sở dữ liệu\n• Chương 2: Giới thiệu mô hình quan hệ & khóa ngoại\n• Chương 3: Ngôn ngữ truy vấn SQL cơ bản\n• Chương 4: SQL trung cấp & Phép kết (JOIN)\n\nPHẦN II: THIẾT KẾ CƠ SỞ DỮ LIỆU\n• Chương 6: Thiết kế CSDL bằng mô hình Thực thể - Kết hợp (E-R)\n• Chương 7: Chuẩn hóa dữ liệu (1NF, 2NF, 3NF, BCNF)"),
                        (3, "Chương 1: Vì sao cần Hệ quản trị CSDL?", "Trước khi hệ quản trị cơ sở dữ liệu (DBMS) ra đời, các tổ chức thường lưu trữ thông tin trong các tập tin văn bản rời rạc. Cách tiếp cận này dẫn tới nhiều hệ lụy nghiêm trọng:\n\n1. Dư thừa và không nhất quán dữ liệu (Data Redundancy & Inconsistency): Cùng một thông tin sinh viên có thể bị lưu ở nhiều phòng ban khác nhau và cập nhật lệch pha.\n2. Khó khăn trong truy xuất dữ liệu mới: Mỗi yêu cầu thống kê đột xuất đều phải viết lại chương trình xử lý tệp.\n3. Vấn đề toàn vẹn (Integrity) và tính nguyên tử (Atomicity) khi giao dịch bị gián đoạn giữa chừng."),
                        (4, "Chương 3: Cấu trúc cơ bản của truy vấn SQL", "Một câu lệnh truy vấn dữ liệu chuẩn trong SQL thường xoay quanh ba mệnh đề cốt lõi:\n\nSELECT A1, A2, ..., An\nFROM r1, r2, ..., rm\nWHERE P;\n\nTrong đó:\n• SELECT liệt kê các thuộc tính cần trích xuất trong kết quả.\n• FROM xác định danh sách các bảng (quan hệ) tham gia truy vấn.\n• WHERE biểu diễn vị từ điều kiện lọc các bộ dữ liệu thỏa mãn.\n\nLưu ý thực hành: Luôn đặt chỉ mục (Index) hợp lý trên khóa ngoại và cột tìm kiếm thường xuyên để tránh quét toàn bảng (Table Scan)."),
                        (5, "Case Study thực hành: Giao dịch ACID", "Xét nghiệp vụ chuyển khoản hoặc thu hồi tài liệu thư viện: mọi thao tác cập nhật trạng thái bản sao sách và ghi nhận phiếu trả phải diễn ra trọn vẹn trong cùng một Transaction.\n\n• Atomicity (Nguyên tử): Toàn bộ thao tác thành công hoặc hoàn tác (Rollback) hoàn toàn.\n• Consistency (Nhất quán): Đảm bảo ràng buộc khóa ngoại và trạng thái thẻ thư viện luôn hợp lệ.\n• Isolation (Cô lập): Hai giao dịch mượn cùng một cuốn sách vật lý không thể cùng lúc cấp phát một số kiểm soát duy nhất.")
                    }
                ),
                ["IT-002"] = (
                    "Cẩm nang viết mã sạch (Clean Code) giúp lập trình viên nâng cao khả năng đọc hiểu, bảo trì, đặt tên biến/hàm chuẩn mực và tái cấu trúc hệ thống phần mềm bền vững.",
                    464,
                    new[]
                    {
                        (1, "Lời mở đầu: Chi phí của mã nguồn rối rắm", "Bất kỳ lập trình viên nào từng tham gia dự án phần mềm kéo dài đều đã nếm trải cảm giác tốc độ phát triển chậm dần theo thời gian. Nguyên nhân không nằm ở ngôn ngữ lập trình, mà ở chất lượng của mã nguồn.\n\nClean Code không chỉ là chuyện code chạy đúng hôm nay, mà là viết code sao cho đồng đội — và chính bạn sáu tháng sau — có thể đọc hiểu ngay lập tức mà không cần đoán mò.\n\nQuy tắc hướng đạo sinh (Boy Scout Rule): Hãy luôn để lại đoạn mã sạch hơn lúc bạn mới mở nó ra."),
                        (2, "Mục lục các nguyên tắc cốt lõi", "• Chương 1: Clean Code là gì?\n• Chương 2: Đặt tên có ý nghĩa (Meaningful Names)\n• Chương 3: Thiết kế Hàm (Functions)\n• Chương 4: Bình luận đúng chỗ (Comments)\n• Chương 5: Định dạng mã nguồn (Formatting)\n• Chương 6: Đối tượng và Cấu trúc dữ liệu\n• Chương 7: Xử lý ngoại lệ (Error Handling)\n• Chương 9: Viết Unit Test sạch sẽ (Unit Tests)"),
                        (3, "Chương 2: Đặt tên có ý nghĩa", "Tên biến, tên hàm và tên lớp phải trả lời trực tiếp 3 câu hỏi: Tại sao nó tồn tại? Nó làm gì? Nó được dùng như thế nào?\n\nVí dụ chưa tốt:\nint d; // elapsed time in days\n\nVí dụ rõ ràng:\nint elapsedTimeInDays;\nint daysSinceCreation;\n\nKhi tên gọi đã tự giải thích ý nghĩa, chúng ta không còn cần các dòng comment thừa thãi để dịch lại ý đồ của biến."),
                        (4, "Chương 3: Hàm nhỏ và chỉ làm một việc", "Quy tắc vàng đầu tiên của hàm là: HÀM PHẢI NHỎ. Quy tắc thứ hai là: HÀM PHẢI NHỎ HƠN NỮA!\n\nMỗi hàm chỉ nên đảm nhiệm duy nhất một nhiệm vụ (Do One Thing), giữ ở một mức trừu tượng đồng nhất và hạn chế lồng ghép if/else quá sâu.\n\nSử dụng Early Return (thoát sớm khi điều kiện không hợp lệ) giúp luồng logic chính luôn thẳng hàng, dễ đọc và dễ kiểm thử đơn vị (Unit Test)."),
                        (5, "Chương 9: Ba định luật của TDD & Unit Test", "Mã kiểm thử (Test code) cũng quan trọng ngang hàng với mã sản phẩm (Production code). Bộ test sạch sẽ phải tuân theo chuẩn F.I.R.S.T:\n\n• Fast: Chạy nhanh để lập trình viên chạy thường xuyên.\n• Independent: Các bài test không phụ thuộc thứ tự của nhau.\n• Repeatable: Chạy ổn định ở mọi môi trường.\n• Self-Validating: Tự kết luận Pass/Fail rõ ràng.\n• Timely: Viết kịp thời cùng tính năng mới.")
                    }
                ),
                ["NN-001"] = (
                    "Bộ đề luyện thi IELTS Academic chuẩn khảo thí Cambridge (Ấn bản 18) gồm 4 bài thi đầy đủ 4 kỹ năng Listening, Reading, Writing và Speaking kèm đáp án chi tiết.",
                    144,
                    new[]
                    {
                        (1, "Giới thiệu cấu trúc bộ đề IELTS 18", "Cambridge IELTS 18 Academic mang đến 4 đề thi hoàn chỉnh mô phỏng chính xác định dạng đề thi IELTS Học thuật thực tế.\n\nTài liệu phù hợp cho sinh viên HUFLIT ôn luyện đạt chuẩn đầu ra tiếng Anh hoặc chinh phục mục tiêu IELTS 6.5 - 8.0+.\n\nPhần cuối sách cung cấp Audio Script, đáp án gợi ý (Answer Key) và bài viết mẫu (Sample Writing Answers) kèm nhận xét của giám khảo."),
                        (2, "Hướng dẫn phân bổ thời gian làm bài", "• Listening (30 phút + 10 phút chuyển đáp án trên giấy): 4 Parts, 40 câu hỏi tăng dần độ khó.\n• Academic Reading (60 phút): 3 bài đọc học thuật dài với 40 câu hỏi dạng True/False/Not Given, Matching Headings, Summary Completion.\n• Academic Writing (60 phút): Task 1 (mô tả biểu đồ/quy trình tối thiểu 150 từ) & Task 2 (bài luận học thuật tối thiểu 250 từ).\n• Speaking (11 - 14 phút): Phỏng vấn trực tiếp 3 phần."),
                        (3, "Test 1 — Reading Passage 1 (Trích đoạn)", "URBAN FARMING IN MODERN CITIES\n\nOn top of a striking exhibition hall in Paris, rows of strawberries and aromatic herbs grow without a single gram of conventional soil. Instead, vertical aeroponic towers mist plant roots with a carefully calibrated nutrient solution.\n\nProponents argue that rooftop agriculture reduces food miles, cools overheated urban buildings, and reconnects city dwellers with seasonal produce. Yet economists caution that high energy costs still limit which crops remain commercially viable."),
                        (4, "Reading Practice Questions (Mẫu câu hỏi)", "Questions 1–4: Do the following statements agree with the information given in Reading Passage 1?\n\nWrite TRUE if the statement agrees with the information;\nFALSE if the statement contradicts the information;\nNOT GIVEN if there is no information on this.\n\n1. The Paris rooftop farm relies entirely on traditional soil beds.\n2. Vertical towers spray nutrients directly onto plant roots.\n3. Rooftop farms can help lower temperatures on urban structures."),
                        (5, "Writing Task 2 — Chiến lược lập dàn ý Band 7.5+", "Topic: Some people believe that universities should focus exclusively on practical career skills rather than theoretical knowledge. To what extent do you agree or disagree?\n\nGợi ý cấu trúc 4 đoạn chuẩn mạch lạc:\n• Introduction: Paraphrase đề bài + Nêu rõ quan điểm cân bằng.\n• Body 1: Vai trò thiết yếu của kỹ năng thực hành đối với khả năng thích ứng nghề nghiệp.\n• Body 2: Giá trị lâu dài của nền tảng lý thuyết trong tư duy phản biện và nghiên cứu.\n• Conclusion: Khẳng định sự kết hợp hài hòa cả hai yếu tố.")
                    }
                ),
                ["NN-002"] = (
                    "Tài liệu hệ thống hóa toàn diện ngữ pháp tiếng Anh nâng cao: câu phức, mệnh đề quan hệ rút gọn, đảo ngữ, thức giả định và cấu trúc học thuật dùng trong biên phiên dịch.",
                    380,
                    new[]
                    {
                        (1, "Lời giới thiệu giáo trình Ngữ pháp Nâng cao", "Cuốn sách được biên soạn dành cho sinh viên chuyên ngành Ngôn ngữ Anh và thí sinh luyện thi chứng chỉ B2-C1.\n\nNội dung đi sâu giải thích bản chất ngữ nghĩa của từng cấu trúc ngữ pháp phức tạp, phân biệt rõ văn phong học thuật (Academic Register) với giao tiếp thông thường, đồng thời chỉ ra các lỗi diễn đạt tiếng Anh mà người Việt thường mắc phải."),
                        (2, "Mục lục chuyên đề trọng tâm", "• Chuyên đề 1: Hệ thống 12 thì & Sự phối hợp thì (Sequence of Tenses)\n• Chuyên đề 2: Mệnh đề quan hệ & Rút gọn mệnh đề\n• Chuyên đề 3: Câu điều kiện hỗn hợp & Thức giả định (Subjunctive)\n• Chuyên đề 4: Đảo ngữ (Inversion) nhấn mạnh học thuật\n• Chuyên đề 5: Câu chẻ (Cleft Sentences) & Liên kết văn bản"),
                        (3, "Chuyên đề 4: Cấu trúc Đảo ngữ nhấn mạnh", "Đảo ngữ (Inversion) thường được sử dụng trong văn viết trang trọng hoặc bài luận học thuật để nhấn mạnh ý nghĩa phủ định hoặc mức độ.\n\n1. Sau các trạng từ phủ định đứng đầu câu:\n• Never have I witnessed such remarkable academic dedication.\n• Not only does reading broaden vocabulary, but it also sharpens analytical thinking.\n• Hardly had the lecture begun when the electricity went out."),
                        (4, "Chuyên đề 3: Thức giả định (Subjunctive Mood)", "Thức giả định hiện đại thường xuất hiện sau các động từ hoặc tính từ biểu thị yêu cầu, đề nghị, tính cấp thiết:\n\nCấu trúc: S + suggest/recommend/insist/demand + that + S + (should) + V(bare-infinitive)\n\nVí dụ:\n• The library committee recommended that every student return borrowed textbooks on time.\n• It is imperative that the system verify the reader's card status before issuing a new loan."),
                        (5, "Bài tập vận dụng & Đáp án gợi ý", "Viết lại các câu sau sử dụng cấu trúc Đảo ngữ:\n\n1. Students are allowed to borrow up to three books only when their library card is active.\n→ Only when their library card is active are students allowed to borrow up to three books.\n\n2. We rarely encounter such a comprehensive reference textbook.\n→ Rarely do we encounter such a comprehensive reference textbook.")
                    }
                ),
                ["KT-001"] = (
                    "Giáo trình Kinh Tế Vi Mô chuẩn hóa phục vụ sinh viên khối Kinh tế - Quản trị, phân tích hành vi người tiêu dùng, cung cầu thị trường, độ co giãn và cấu trúc cạnh tranh.",
                    512,
                    new[]
                    {
                        (1, "Tổng quan về Kinh tế học Vi mô", "Kinh tế học nghiên cứu cách thức xã hội quản lý các nguồn lực khan hiếm. Trong đó, Kinh tế vi mô tập trung phân tích các quyết định ở cấp độ từng hộ gia đình, từng doanh nghiệp và sự tương tác của họ trên từng thị trường cụ thể.\n\nCuốn giáo trình của N. Gregory Mankiw nổi tiếng toàn cầu nhờ văn phong sáng rõ, minh họa bằng những tình huống kinh tế đời thực gần gũi với sinh viên."),
                        (2, "10 Nguyên lý của Kinh tế học (Trích chương 1)", "• Nguyên lý 1: Con người đối mặt với sự đánh đổi (Trade-offs).\n• Nguyên lý 2: Chi phí của một thứ là cái bạn phải từ bỏ để có được nó (Chi phí cơ hội).\n• Nguyên lý 3: Con người duy lý suy nghĩ tại điểm cận biên (Marginal Changes).\n• Nguyên lý 4: Con người phản ứng với các động cơ khuyến khích.\n• Nguyên lý 5: Thương mại có thể làm cho mọi người đều được lợi."),
                        (3, "Chương 4: Cung, Cầu và Cân bằng thị trường", "Trong thị trường cạnh tranh hoàn hảo, giá cả và sản lượng được quyết định bởi sự giao nhau của đường Cầu (dốc xuống) và đường Cung (dốc lên).\n\n• Quy luật Cầu: Khi các yếu tố khác không đổi, giá hàng hóa tăng thì lượng cầu giảm.\n• Quy luật Cung: Khi giá tăng, lượng cung có xu hướng tăng.\n• Trạng thái Cân bằng (Equilibrium): Tại mức giá cân bằng P*, lượng cung bằng đúng lượng cầu (Qs = Qd)."),
                        (4, "Chương 5: Độ co giãn và Ứng dụng thực tế", "Độ co giãn của cầu theo giá (Price Elasticity of Demand) đo lường mức độ phản ứng của lượng cầu khi giá thay đổi 1%:\n\nEd = (% Thay đổi lượng cầu) / (% Thay đổi giá)\n\n• Nếu |Ed| > 1: Cầu co giãn nhiều -> Giảm giá giúp tăng tổng doanh thu.\n• Nếu |Ed| < 1: Cầu ít co giãn -> Tăng giá làm tăng tổng doanh thu."),
                        (5, "Câu hỏi thảo luận & Bài tập tình huống", "Tình huống thực tế: Một hãng hàng không nhận thấy giá vé hạng phổ thông giảm 15% giúp lượng khách đặt vé tăng 30%.\n\n1. Tính hệ số co giãn của cầu theo giá đối với vé hạng phổ thông.\n2. Doanh thu của hãng thay đổi theo chiều hướng nào?\n3. Kết quả này giúp bộ phận định giá ra quyết định ra sao vào mùa thấp điểm?")
                    }
                ),
                ["KT-002"] = (
                    "Giáo trình Quản trị học hiện đại tiếp cận theo 4 chức năng POLC (Hoạch định, Tổ chức, Lãnh đạo, Kiểm soát) gắn với chuyển đổi số và quản trị nhân lực thế hệ mới.",
                    620,
                    new[]
                    {
                        (1, "Giới thiệu Quản trị học trong Kỷ nguyên số", "Thế giới kinh doanh hiện đại biến đổi nhanh chóng dưới tác động của trí tuệ nhân tạo, làm việc từ xa và chuỗi cung ứng toàn cầu.\n\nCuốn sách cung cấp khung tư duy toàn diện giúp nhà quản trị tương lai cân bằng giữa hiệu suất (Efficiency — làm đúng cách) và hiệu quả (Effectiveness — làm đúng việc)."),
                        (2, "4 Chức năng cốt lõi của Nhà quản trị (POLC)", "1. Planning (Hoạch định): Thiết lập mục tiêu, xây dựng chiến lược và kế hoạch phối hợp hành động.\n2. Organizing (Tổ chức): Phân công công việc, thiết lập cơ cấu bộ máy và phân bổ nguồn lực.\n3. Leading (Lãnh đạo): Truyền cảm hứng, động viên nhân sự và giải quyết xung đột nhóm.\n4. Controlling (Kiểm soát): Theo dõi hiệu suất, đo lường kết quả và điều chỉnh sai lệch."),
                        (3, "Ra quyết định quản trị dựa trên dữ liệu", "Trong kỷ nguyên số, trực giác quản trị cần được bổ trợ bởi phân tích dữ liệu (Data-Driven Decision Making).\n\nQuy trình ra quyết định 8 bước bắt đầu từ việc nhận diện đúng vấn đề cốt lõi, xác định tiêu chí đánh giá, cân nhắc trọng số, liệt kê các phương án thay thế và đánh giá rủi ro triển khai."),
                        (4, "Văn hóa doanh nghiệp & Đổi mới sáng tạo", "Văn hóa tổ chức là hệ thống giá trị, niềm tin và chuẩn mực chia sẻ chi phối hành vi của các thành viên.\n\nNhững doanh nghiệp thích ứng tốt thường khuyến khích thử nghiệm có kiểm soát, tôn trọng phản biện mang tính xây dựng và học hỏi nhanh từ thất bại."),
                        (5, "Case Study: Chuyển đổi số tại doanh nghiệp dịch vụ", "Một hệ thống thư viện đại học muốn chuyển đổi quy trình mượn - trả thủ công sang nền tảng số tích hợp tra cứu nhanh và thanh toán QR.\n\nHãy phân tích vai trò của nhà quản trị ở 4 khâu: Lập kế hoạch chuyển đổi, Tổ chức đào tạo thủ thư, Dẫn dắt thay đổi thói quen người đọc và Kiểm soát tỷ lệ sai sót.")
                    }
                ),
                ["DL-001"] = (
                    "Giáo trình chuyên sâu nghiệp vụ quản trị khách sạn, nhà hàng, chuẩn hóa quy trình phục vụ lưu trú, ẩm thực (F&B) và nâng cao trải nghiệm khách hàng quốc tế.",
                    410,
                    new[]
                    {
                        (1, "Tổng quan ngành Du lịch & Lưu trú", "Ngành Dịch vụ Du lịch - Khách sạn là một trong những lĩnh vực mũi nhọn đòi hỏi sự kết hợp tinh tế giữa quy trình vận hành chuẩn quốc tế (SOP) và nghệ thuật hiếu khách.\n\nGiáo trình trang bị kiến thức thực tiễn cho sinh viên chuyên ngành Quản trị Dịch vụ Du lịch và Lữ hành tại HUFLIT."),
                        (2, "Cơ cấu tổ chức Khối Tiền sảnh (Front Office)", "Bộ phận Tiền sảnh được ví như 'trái tim' của khách sạn, là điểm chạm đầu tiên và cuối cùng với khách hàng:\n\n• Đặt phòng (Reservation)\n• Lễ tân (Reception / Check-in & Check-out)\n• Hỗ trợ khách hàng (Concierge & Bell Desk)\n• Thu ngân tiền sảnh (Front Office Cashier)"),
                        (3, "Quản trị Bộ phận Ẩm thực (Food & Beverage)", "Hoạt động F&B đóng góp tỷ trọng doanh thu quan trọng trong khách sạn 4–5 sao. Quản trị hiệu quả đòi hỏi kiểm soát chặt chẽ Food Cost, tiêu chuẩn vệ sinh an toàn thực phẩm HACCP và nghệ thuật thiết kế thực đơn (Menu Engineering)."),
                        (4, "Tiêu chuẩn chất lượng dịch vụ SERVQUAL", "Khách hàng đánh giá chất lượng dịch vụ dựa trên 5 yếu tố cốt lõi:\n1. Độ tin cậy (Reliability)\n2. Tinh thần đáp ứng (Responsiveness)\n3. Năng lực phục vụ & sự bảo đảm (Assurance)\n4. Sự đồng cảm (Empathy)\n5. Phương tiện hữu hình (Tangibles)"),
                        (5, "Tình huống xử lý phàn nàn của khách (L.E.A.R.N)", "Áp dụng quy trình 5 bước L.E.A.R.N khi tiếp nhận phản hồi chưa hài lòng:\n• Listen: Lắng nghe chân thành, không ngắt lời.\n• Empathize: Thể hiện sự thấu cảm.\n• Apologize: Xin lỗi thiện chí.\n• React / Resolve: Đưa ra giải pháp khắc phục nhanh chóng.\n• Notify: Ghi nhận và theo dõi sự hài lòng sau xử lý.")
                    }
                ),
                ["IT-003"] = (
                    "Tài liệu thực hành Học máy (Machine Learning) và Học sâu (Deep Learning) toàn diện với Scikit-Learn, Keras và PyTorch từ bài toán hồi quy đến mạng nơ-ron hiện đại.",
                    856,
                    new[]
                    {
                        (1, "Giới thiệu Hệ sinh thái AI & Học máy", "Machine Learning là khoa học lập trình giúp máy tính tự học quy luật từ dữ liệu mà không cần viết cứng từng câu lệnh điều kiện.\n\nTừ hệ thống gợi ý sách thông minh tại thư viện, lọc thư rác đến nhận diện hình ảnh và mô hình ngôn ngữ lớn (LLM), Học máy đang định hình lại toàn bộ ngành công nghiệp phần mềm."),
                        (2, "Phân loại các nhóm thuật toán Học máy", "• Học có giám sát (Supervised Learning): Dữ liệu huấn luyện đã gắn nhãn (Hồi quy tuyến tính, Random Forest, XGBoost, SVM).\n• Học không giám sát (Unsupervised Learning): Phân cụm khách hàng (K-Means), giảm chiều dữ liệu (PCA).\n• Học tăng cường (Reinforcement Learning): Tác nhân học chiến lược tối ưu qua phần thưởng."),
                        (3, "Quy trình một dự án Machine Learning chuẩn", "1. Xác định rõ mục tiêu nghiệp vụ và thước đo đánh giá (RMSE, F1-Score, ROC-AUC).\n2. Khám phá và làm sạch dữ liệu (EDA, xử lý giá trị thiếu, chuẩn hóa đặc trưng).\n3. Tách tập Train / Validation / Test ngay từ đầu để tránh rò rỉ dữ liệu (Data Leakage).\n4. Huấn luyện, tinh chỉnh siêu tham số và đánh giá chéo (Cross-Validation)."),
                        (4, "Phòng tránh Overfitting & Regularization", "Khi mô hình học quá khớp nhiễu trên tập huấn luyện nhưng dự đoán kém trên dữ liệu mới, hiện tượng Overfitting xảy ra.\n\nCác giải pháp khắc phục phổ biến:\n• Thu thập thêm dữ liệu chất lượng.\n• Giảm độ phức tạp mô hình.\n• Áp dụng điều chuẩn Ridge (L2), Lasso (L1) hoặc Dropout trong mạng nơ-ron."),
                        (5, "Thực hành Python với Scikit-Learn Pipeline", "from sklearn.pipeline import Pipeline\nfrom sklearn.preprocessing import StandardScaler\nfrom sklearn.ensemble import RandomForestClassifier\n\npipeline = Pipeline([\n    ('scaler', StandardScaler()),\n    ('clf', RandomForestClassifier(n_estimators=200, random_state=42))\n])\npipeline.fit(X_train, y_train)\n\nSử dụng Pipeline giúp đóng gói đồng bộ bước tiền xử lý và huấn luyện, tránh sai lệch khi triển khai thực tế.")
                    }
                )
            };

            var books = context.Sachs.Include(s => s.TrangDocThus).ToList();
            foreach (var book in books)
            {
                if (metadataMap.TryGetValue(book.MaSachCode, out var info))
                {
                    if (string.IsNullOrWhiteSpace(book.MoTa))
                    {
                        book.MoTa = info.Description;
                    }
                    if (book.SoTrang <= 0)
                    {
                        book.SoTrang = info.PageCount;
                    }
                    if (!book.TrangDocThus.Any())
                    {
                        foreach (var p in info.Pages)
                        {
                            book.TrangDocThus.Add(new TrangDocThu
                            {
                                SoTrang = p.Page,
                                TieuDe = p.Title,
                                NoiDung = p.Content
                            });
                        }
                    }
                }
                else
                {
                    if (book.SoTrang <= 0)
                    {
                        book.SoTrang = 320;
                    }
                }
            }
        }
    }
}