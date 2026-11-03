# BÁO CÁO KỸ THUẬT: KIẾN TRÚC HỆ THỐNG & CHIẾN LƯỢC TÍCH HỢP NOSQL MONGODB
**Đề tài:** Hệ thống Quản lý Thư viện Số (Library Management System)  
**Công nghệ:** ASP.NET Core 8 MVC, SQL Server (RDBMS), MongoDB (NoSQL Document Store), Ollama Local LLM (Qwen 2.5 7B / Gemma 3), Google Gemini AI  
**Mô hình áp dụng:** Polyglot Persistence (Kiến trúc Đa cơ sở dữ liệu)

---

## 1. ĐẶT VẤN ĐỀ VÀ BỐI CẢNH KỸ THUẬT

### 1.1. Đặc thù nghiệp vụ quản lý thư viện
Hệ thống Quản lý Thư viện có hai nhóm nghiệp vụ mang bản chất dữ liệu đối lập nhau:

1. **Nhóm nghiệp vụ lưu thông có cấu trúc & ràng buộc giao dịch cao (Structured & ACID):**
   - Quản lý đầu sách vật lý theo số kiểm soát (`SoKiemSoat`).
   - Quy trình Mượn - Trả - Bồi thường - Phạt trễ hạn (2.000đ/ngày).
   - Ràng buộc quan hệ chặt chẽ: Một tài khoản gắn liền với một thẻ thư viện; một thẻ gắn với nhiều phiếu mượn; mỗi phiếu mượn chi tiết gắn với đúng một bản sao sách vật lý.
   - **Yêu cầu sống còn:** Tính toàn vẹn dữ liệu (Integrity), khóa ngoại (Foreign Keys), cơ chế khóa giao dịch (ACID Transactions) để loại trừ triệt để tình trạng *Race Condition* (hai sinh viên cùng bấm mượn một bản sao sách vật lý tại cùng một thời điểm).

2. **Nhóm nghiệp vụ phi cấu trúc & dữ liệu tăng trưởng nhanh (Unstructured & High-Velocity):**
   - **Nhật ký kiểm toán an toàn hệ thống (System Audit Logs):** Ghi nhận liên tục mọi hành động đăng nhập, khóa/mở thẻ bạn đọc, thu tiền phạt, xuất báo cáo.
   - **Lịch sử hội thoại Trợ lý ảo AI (Chatbot Logs):** Các câu hỏi - đáp của sinh viên với Trợ lý ảo AI (Ollama Local LLM / Google Gemini) có độ dài tự do, phi cấu trúc, không có lược đồ cố định (Schema-less).

---

## 2. PHÂN TÍCH SO SÁNH: VÌ SAO KHÔNG BÊ NGUYÊN RDBMS SANG NOSQL THUẦN?

Trong quá trình thực nghiệm kỹ thuật, việc cố gắng chuyển đổi 100% các bảng quan hệ sang MongoDB Document thông qua Entity Framework Core dẫn tới các giới hạn kỹ thuật cơ bản:

| Tiêu chí | Cố ép 100% sang MongoDB (Pure Document) | Kiến trúc Hybrid: SQL Server + MongoDB (Được chọn) |
| :--- | :--- | :--- |
| **Hỗ trợ truy vấn quan hệ (`.Include()`)** | ❌ **Thất bại:** MongoDB EF Core ném lỗi `System.InvalidOperationException: Including navigation is not supported as the navigation is not embedded in same resource`. Không hỗ trợ JOIN đa bảng giữa các Collection riêng biệt. | ✅ **Tối ưu:** SQL Server xử lý các câu truy vấn JOIN, GROUP BY, SUM, COUNT trong các màn hình Báo cáo (`ReportController`) với tốc độ mili-giây. |
| **Quản lý khóa chính (Primary Key)** | ❌ MongoDB không hỗ trợ cột số nguyên tự tăng `IDENTITY(1,1)` ở cấp database; dễ bị lỗi trùng lặp `_id` khi thêm bản ghi đồng thời. | ✅ SQL Server tự động sinh mã ID số học cho sách, phiếu mượn; MongoDB dùng chuẩn `ObjectId` cho Log. |
| **Toàn vẹn khóa ngoại (Foreign Keys)** | ❌ NoSQL không có cơ chế `ON DELETE RESTRICT`. Nếu xóa một cuốn sách đang được mượn, dữ liệu phiếu mượn sẽ bị mồ côi (Orphaned Data). | ✅ SQL Server tự động kiểm tra và chặn thao tác xóa nếu sách đang nằm trong phiếu mượn hợp lệ. |
| **Lưu trữ Log & Dữ liệu AI** | ❌ Bảng SQL quan hệ lưu log sẽ nhanh chóng bị phình to (Table Bloat), làm chậm các câu truy vấn nghiệp vụ chính. | ✅ **Tuyệt hảo:** MongoDB lưu trữ Document JSON linh hoạt, dễ dàng mở rộng (Scale Horizontal) và ghi tốc độ cực cao. |

> 💡 **Kết luận Kiến trúc:** Áp dụng mô hình **Polyglot Persistence** (Mỗi công nghệ cơ sở dữ liệu làm đúng thế mạnh tự nhiên nhất của nó) là chuẩn mực kỹ thuật cao cấp nhất cho dự án này.

---

## 3. SƠ ĐỒ KIẾN TRÚC POLYGLOT PERSISTENCE & AI TELEMETRY

```
                   +---------------------------------------------+
                   |          Giao diện Người dùng (Web)         |
                   |   - Quản trị viên (Admin) / Thủ thư         |
                   |   - Sinh viên / Bạn đọc (Client)            |
                   +---------------------------------------------+
                                          |
                                    (HTTP Requests)
                                          v
+---------------------------------------------------------------------------------+
|                         TẦNG ỨNG DỤNG (ASP.NET CORE 8 MVC)                      |
|                                                                                 |
|   +--------------------------+                 +----------------------------+   |
|   |  Controllers Nghiệp Vụ   |                 |   Controllers Mở Rộng      |   |
|   |  - BookController        |                 |   - ChatController (AI)    |   |
|   |  - BorrowController      |                 |   - HomeController         |   |
|   |  - ReportController      |                 |   - AccountController      |   |
|   +--------------------------+                 +----------------------------+   |
|                |                                       |            |           |
|     (Entity Framework Core)                            |     (MongoDB.Driver)   |
|                v                                       v            v           |
|   +--------------------------+          +-------------------+ +---------------+ |
|   |   ApplicationDbContext   |          | ILibraryChatService| |ILibraryMongoLog| |
+---|--------------------------|----------|-------------------|-|---------------|-+
                 |                        |  (Ollama/Gemini)  |         |
                 v                        +-------------------+         v
   +---------------------------+                 |       +----------------------------+
   |   SQL SERVER (RDBMS)      |                 v       |      MONGODB (NoSQL)       |
   |   Database: LibraryDB     |     +-----------------+ |    Database: LibraryDB     |
   |                           |     | Ollama Local LLM| |                            |
   |  - Sachs, DauSachs        |     | (qwen2.5-7b /   | |  - system_audit_logs       |
   |  - TheLoais, NhaXuatBans  |     |  gemma3-4b)     | |    (Nhật ký bảo mật)       |
   |  - TaiKhoans, TheThuViens |     +-----------------+ |  - chat_history_logs       |
   |  - PhieuMuons, ChiTiets   |                         |    (Lịch sử trò chuyện AI) |
   |  - ThongBaos, DanhGias    |                         +----------------------------+
   +---------------------------+
```

---

## 4. CHI TIẾT THIẾT KẾ MÔ HÌNH DỮ LIỆU TRÊN MONGODB

### 4.1. Collection: `system_audit_logs` (Nhật ký kiểm toán hệ thống)
Mỗi sự kiện nghiệp vụ quan trọng trong thư viện đều được tự động lưu lại dưới dạng Document JSON:
```json
{
  "_id": { "$oid": "6726f1c4a12b3c4d5e6f7a8b" },
  "Action": "BORROW",
  "Username": "thuthu",
  "UserRole": "Librarian",
  "Details": "Thủ thư tạo phiếu mượn mã #1 cho sinh viên Nguyễn Văn A (LIB-24DH112970).",
  "IpAddress": "127.0.0.1",
  "Timestamp": { "$date": "2026-11-03T01:47:54.000Z" }
}
```

### 4.2. Collection: `chat_history_logs` (Lịch sử hội thoại Trợ lý ảo AI)
Lưu trữ toàn bộ các câu hỏi đáp của sinh viên với Thủ thư AI để phân tích nhu cầu tìm kiếm sách:
```json
{
  "_id": { "$oid": "6726f1c4a12b3c4d5e6f7a8c" },
  "Username": "Khách độc giả",
  "UserPrompt": "Tôi muốn mượn 5 cuốn sách một lúc có được không?",
  "BotResponse": "Xin chào! Theo nội quy mượn sách tại Thư viện HUFLIT cơ sở Hóc Môn, bạn có thể mượn tối đa 3 cuốn sách trong 15 ngày. Do đó, bạn không thể mượn 5 cuốn sách cùng một lúc theo quy định hiện hành.",
  "ModelUsed": "qwen2.5:7b-instruct",
  "Timestamp": { "$date": "2026-11-03T02:16:54.000Z" }
}
```

---

## 5. CƠ CHẾ TỰ ĐỘNG HÓA 100% (ZERO-CONFIGURATION)

Để đảm bảo dự án có thể chạy trên bất kỳ máy tính nào (bao gồm cả máy của Giảng viên khi chấm thi):
- Sử dụng **`Mongo2Go`** được tích hợp qua lớp `MongoRunnerService.cs`.
- Khi ứng dụng ASP.NET Core khởi động:
  1. Tự động kiểm tra xem máy đã cài sẵn MongoDB hay chưa.
  2. Nếu chưa, ứng dụng sẽ **tự động khởi động một máy chủ MongoDB độc lập** trong thư mục `.mongodb_data`.
  3. Lớp `MongoSeeder.cs` tự động đẩy các tài liệu mẫu vào MongoDB để sẵn sàng demo.
  4. Người dùng không cần cài đặt thêm bất kỳ phần mềm nào bằng tay.
- Tích hợp 3 tầng AI linh hoạt:
  1. **Tầng 1 (Local LLM):** Kết nối trực tiếp vào Ollama (`qwen2.5:7b-instruct` hoặc `gemma3:4b`) chạy 100% nội bộ, miễn phí, không tốn API key.
  2. **Tầng 2 (Cloud LLM):** Kết nối vào Google Gemini API khi cấu hình API key.
  3. **Tầng 3 (Fallback Rule):** Bộ tri thức nội quy thư viện HUFLIT tự sinh câu trả lời khi máy không có cả Ollama lẫn internet.

---

## 6. KỊCH BẢN DEMO BẢO VỆ TRƯỚC GIẢNG VIÊN (DEMO SCRIPT)

Khi giảng viên yêu cầu nghiệm thu tính năng MongoDB & AI:

1. **Bước 1: Khởi động ứng dụng**
   - Mở Terminal và gõ: `dotnet run`
   - Web mở tại địa chỉ: `http://localhost:5043`
   - Mở trang giám sát trực quan: `http://localhost:5043/Home/MongoStatus`

2. **Bước 2: Tương tác sinh dữ liệu mới với AI**
   - Bấm vào biểu tượng **"Trợ lý Thủ thư ảo HUFLIT"** ở góc dưới cùng bên phải màn hình.
   - Nhập một câu hỏi bất kỳ, ví dụ: *"Tôi muốn mượn 5 cuốn sách có được không?"* hoặc *"Quy định phạt quá hạn sách thế nào?"*.
   - Trợ lý AI (Qwen 2.5 chạy qua Ollama) sẽ tư duy và phản hồi chi tiết, lịch sự.

3. **Bước 3: Mở MongoDB Compass cho Giảng viên xem**
   - Copy chuỗi kết nối hiển thị trên màn hình `MongoStatus` (dạng `mongodb://127.0.0.1:<port>/`).
   - Mở ứng dụng **MongoDB Compass**, dán chuỗi kết nối và bấm **Connect**.
   - Chọn database **`LibraryDB`**, mở collection **`chat_history_logs`**:
   - Cho giảng viên thấy trực tiếp câu hỏi vừa chat xuất hiện ngay lập tức dưới dạng Document JSON với trường `"ModelUsed": "qwen2.5:7b-instruct"`.

---

## 7. BỘ CÂU HỎI VẤN ĐÁP THƯỜNG GẶP (Q&A VỚI GIẢNG VIÊN)

#### ❓ Câu hỏi 1: "Tại sao nhóm không đưa toàn bộ các bảng sách và mượn trả sang MongoDB mà lại dùng song song hai Database?"
> **Trả lời:**  
> *"Thưa Thầy/Cô, việc lựa chọn Database phải xuất phát từ bản chất của nghiệp vụ. Nghiệp vụ lưu thông mượn/trả sách đòi hỏi tính toàn vẹn giao dịch ACID tuyệt đối (không thể để xảy ra trường hợp hai độc giả cùng mượn một bản sao sách vật lý duy nhất, hay tiền phạt bị sai lệch số học). RDBMS (SQL Server) sinh ra để giải quyết hoàn hảo bài toán ACID và quan hệ khóa ngoại này.*  
> *Ngược lại, dữ liệu Audit Logs và dữ liệu hội thoại AI Chatbot có cấu trúc thay đổi liên tục và khối lượng ghi rất lớn. Nếu lưu vào SQL Server sẽ làm phình to bảng và nghẽn hệ thống. Vì vậy, nhóm em quyết định áp dụng mô hình **Polyglot Persistence**: dùng SQL Server cho giao dịch nghiệp vụ cốt lõi và dùng MongoDB cho dữ liệu phi cấu trúc, nhật ký truy vết. Đây cũng là kiến trúc tiêu chuẩn đang được áp dụng tại các hệ thống lớn thực tế."*

#### ❓ Câu hỏi 2: "Ưu điểm của việc lưu trữ Chat AI và System Logs trên MongoDB là gì?"
> **Trả lời:**  
> *"Thưa Thầy/Cô, có 3 ưu điểm vượt trội:*  
> *1. **Schema-less:** Hội thoại AI có thể thay đổi cấu trúc linh hoạt (số lượng token, model sử dụng, ngữ cảnh) mà không cần chỉnh sửa schema bảng.*  
> *2. **Tốc độ ghi (Write Throughput):** MongoDB sử dụng cơ chế ghi trong bộ nhớ rồi đồng bộ ngầm, giúp ứng dụng ghi log liên tục với tốc độ cao mà không làm chậm trải nghiệm duyệt web của người dùng.*  
> *3. **Dễ dàng mở rộng:** Dữ liệu log tăng theo thời gian có thể dễ dàng tách Sharding hoặc thiết lập thời gian tự hủy (TTL Index) trên MongoDB rất thuận tiện."*

#### ❓ Câu hỏi 3: "Trợ lý ảo AI hoạt động theo cơ chế nào? Có phải AI thật không?"
> **Trả lời:**  
> *"Dạ thưa Thầy/Cô, đây là AI thật 100%. Hệ thống được xây dựng theo kiến trúc đa tầng (Multi-tier LLM Adapter):*  
> *1. Khi chạy trên môi trường cục bộ, web kết nối trực tiếp với máy chủ Ollama chạy model Qwen 2.5 7B Instruct (hoặc Google Gemma 3), hoàn toàn bảo mật và không phụ thuộc internet.*  
> *2. Khi cấu hình API Key, hệ thống có thể linh hoạt chuyển sang Google Gemini 1.5 Flash trên Cloud.*  
> *3. Mọi truy vấn và câu trả lời đều được định tuyến và lưu vết tự động vào MongoDB dưới dạng JSON Document để phục vụ phân tích trải nghiệm độc giả."*
