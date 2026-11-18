# 📚 SMARTLIB - HỆ THỐNG QUẢN LÝ THƯ VIỆN SỐ HUFLIT

> Hệ thống Quản lý Thư viện Số hiện đại áp dụng kiến trúc **Polyglot Persistence (SQL Server + MongoDB NoSQL)** kết hợp Trợ lý Trí tuệ Nhân tạo **AI Telemetry (Ollama Local LLM / Google Gemini)**.

---

## 🌟 Tính Năng Nổi Bật

### 1. Dành cho Độc giả & Sinh viên
- **Tra cứu danh mục sách:** Tìm kiếm nhanh, lọc theo Thể loại, Tác giả, Nhà xuất bản, Tình trạng còn sách.
- **Chi tiết tựa sách:** Xem thông tin xuất bản, tình trạng các bản sao vật lý trong kho, vị trí kệ sách tại quầy lưu thông.
- **❤️ Sách yêu thích (Wishlist):** Lưu các tựa sách quan tâm để dễ dàng theo dõi và đăng ký mượn khi cần.
- **Đăng ký mượn sách trực tuyến:** Kiểm tra tính hợp lệ của Thẻ thư viện, hạn mức mượn (tối đa 3 cuốn), cam kết hạn trả 15 ngày.
- **Trợ lý ảo AI:** Hỏi đáp, tư vấn tìm sách thông minh được hỗ trợ bởi Ollama LLM (Qwen 2.5 / Gemma 3) hoặc Google Gemini.
- **Hồ sơ cá nhân:** Theo dõi sách đang mượn, lịch sử mượn - trả, chuông thông báo nhắc nhở.

### 2. Dành cho Thủ thư & Quản trị viên (Admin Area)
- **Dashboard phân tích:** Thống kê tổng quan sách, phiếu mượn, tài khoản bạn đọc, tiền phạt.
- **Quản lý lưu thông (Mượn - Trả):** Duyệt yêu cầu mượn, tiếp nhận trả sách, xử lý trường hợp Hỏng/Mất và lập phiếu thu bồi thường.
- **Quản lý kho tài liệu:** Quản lý tựa sách, danh mục thể loại, nhà xuất bản, bản sao vật lý (Đầu sách theo mã số kiểm soát).
- **Nhật ký hệ thống (Audit Logs):** Toàn bộ hành động quan trọng và lịch sử hội thoại AI được lưu trữ trên MongoDB.

---

## 🛠️ Công Nghệ Sử Dụng

- **Backend:** C# / .NET 8 (ASP.NET Core MVC)
- **Database Quan hệ (RDBMS):** Microsoft SQL Server (Entity Framework Core)
- **Database Phi cấu trúc (NoSQL):** MongoDB (Lưu trữ Chatbot History & System Audit Logs)
- **AI Integration:** Ollama (Local LLM: Qwen 2.5 7B, Gemma 3:4b) / Google Gemini API
- **Frontend:** Tailwind CSS, Lucide Icons, JavaScript (Fetch API)

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Dự Án

### Yêu cầu môi trường:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) với khối lượng công việc *ASP.NET and web development*.
- [SQL Server](https://www.microsoft.com/sql-server) (hoặc SQL Server LocalDB đi kèm Visual Studio).
- [MongoDB Community Server](https://www.mongodb.com/try/download/community) (chạy tại cổng mặc định `mongodb://127.0.0.1:27017`).

### Các bước chạy:
1. **Clone repository:**
   ```bash
   git clone https://github.com/TheDay0811/BOOK.git
   cd BOOK
   ```
2. **Cấu hình chuỗi kết nối:**
   Kiểm tra file `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
     "MongoConnection": "mongodb://127.0.0.1:27017"
   }
   ```
3. **Chạy ứng dụng:**
   - Mở `BOOK.slnx` bằng Visual Studio.
   - Nhấn **F5** để khởi chạy.
   - Hệ thống sẽ tự động chạy Migration và nạp dữ liệu mẫu ban đầu (`DbSeeder`).

---

## 🔑 Tài Khoản Thử Nghiệm

| Vai trò | Tên đăng nhập (Username) | Mật khẩu mặc định | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `Admin@123` | Toàn quyền quản trị hệ thống |
| **Thủ thư (Librarian)** | `thuthu01` | `ThuThu@123` | Quản lý mượn trả, kiểm duyệt |
| **Sinh viên (Độc giả)** | `sv001` | `123456` | Thẻ thư viện còn hạn, có thể mượn |
| **Sinh viên (Bị khóa thẻ)** | `sv002` | `123456` | Thẻ tạm khóa do làm mất sách |
