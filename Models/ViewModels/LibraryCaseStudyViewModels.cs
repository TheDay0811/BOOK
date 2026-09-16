using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BOOK.Models.ViewModels
{
    // ================= 1. REGISTER & LIBRARY CARD =================
    public class RegisterWithCardViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên sinh viên.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mã sinh viên (MSSV).")]
        [RegularExpression(@"^[0-9]{8,10}$", ErrorMessage = "MSSV hợp lệ gồm 8-10 chữ số.")]
        [Display(Name = "Mã số sinh viên")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại gồm 10 chữ số.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng thiết lập Mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận Mật khẩu.")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với Nội quy thư viện.")]
        public bool AcceptTerms { get; set; }

        // Thông tin thẻ thư viện tự động liên kết
        public string AutoCardNumber { get; set; } = $"LIB-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
        public DateTime CardIssueDate { get; set; } = DateTime.Today;
        public DateTime CardExpiryDate { get; set; } = DateTime.Today.AddYears(1);
        public string CardStatus { get; set; } = "Sử dụng được"; // "Sử dụng được" hoặc "Không sử dụng"
    }

    // ================= 2. CUỐN SÁCH VẬT LÝ & TỰA SÁCH =================
    public class BookCopyViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string ControlNumber { get; set; } = "001"; // Số kiểm soát (001, 002, 003...)
        public string Status { get; set; } = "Còn"; // [Còn], [Cho mượn], [Mất], [Hỏng]
        public string Notes { get; set; } = "Nguyên vẹn";
        public string ShelfLocation { get; set; } = "Kệ A1 - Tầng 2";
        public DateTime AddedDate { get; set; } = DateTime.Today;
    }

    public class BookTitleManageViewModel
    {
        public int BookId { get; set; }
        public string BookCode { get; set; } = "CS-001"; // Mã sách
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PublisherName { get; set; } = string.Empty;
        public int PublishYear { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Thống kê số lượng các cuốn sách
        public int TotalCopies => Copies.Count;
        public int AvailableCopies => Copies.FindAll(c => c.Status == "Còn").Count;
        public int BorrowedCopies => Copies.FindAll(c => c.Status == "Cho mượn").Count;
        public int DamagedCopies => Copies.FindAll(c => c.Status == "Hỏng").Count;
        public int LostCopies => Copies.FindAll(c => c.Status == "Mất").Count;

        public List<BookCopyViewModel> Copies { get; set; } = new();
    }

    // ================= 3. LẬP PHIẾU MƯỢN SÁCH =================
    public class CardLookupResultViewModel
    {
        public string CardNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public string CardStatus { get; set; } = "Sử dụng được"; // "Sử dụng được" / "Không sử dụng"
        public bool IsExpired => DateTime.Today > ExpiryDate;
        public bool HasUnreturnedBooks { get; set; } = false; // Nếu còn sách chưa trả -> Chặn mượn
        public int ActiveBorrowedCount { get; set; } = 0;
        public bool CanBorrow => CardStatus == "Sử dụng được" && !IsExpired && !HasUnreturnedBooks;
        public string BlockReason { get; set; } = string.Empty;
    }

    public class SelectedBorrowItemViewModel
    {
        public int CopyId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string ControlNumber { get; set; } = string.Empty; // Số kiểm soát (001, 002...)
        public string ShelfLocation { get; set; } = string.Empty;
    }

    public class CreateBorrowTicketViewModel
    {
        public CardLookupResultViewModel CardInfo { get; set; } = new();

        [Required(ErrorMessage = "Vui lòng nhập Số thẻ thư viện.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày mượn.")]
        public DateTime BorrowDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn hạn trả.")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(15); // Hạn mượn tối đa 15 ngày

        public List<SelectedBorrowItemViewModel> SelectedItems { get; set; } = new();

        public string? Notes { get; set; }
    }

    // ================= 4. NHẬN TRẢ SÁCH, MẤT/HỎNG & BỒI THƯỜNG =================
    public class ReturnBookItemViewModel
    {
        public int CopyId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string ControlNumber { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }

        // Tình trạng khi trả: "Bình thường", "Hỏng", "Mất"
        public string ReturnCondition { get; set; } = "Bình thường";
        public string DamageDescription { get; set; } = string.Empty;
        public decimal CompensationFee { get; set; } = 0; // Tiền bồi thường
        public bool IsCompensated { get; set; } = false; // Đã thanh toán bồi thường chưa
    }

    public class ReturnBookDeskViewModel
    {
        public string TicketCode { get; set; } = "PM-2026-0881";
        public string CardNumber { get; set; } = "LIB-2026-1024";
        public string StudentName { get; set; } = "Trần Văn Hoàng";
        public string StudentCode { get; set; } = "22110098";
        public string CurrentCardStatus { get; set; } = "Sử dụng được";
        public DateTime CardExpiryDate { get; set; } = DateTime.Today.AddMonths(6);

        public List<ReturnBookItemViewModel> BorrowedCopies { get; set; } = new();
    }

    // ================= 5. BÁO CÁO SÁCH MẤT/HỎNG THANH LÝ =================
    public class DamagedLostBookRecordViewModel
    {
        public int Id { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ControlNumber { get; set; } = string.Empty; // 001, 002...
        public string ConditionStatus { get; set; } = "Hỏng"; // "Hỏng" hoặc "Mất"
        public DateTime RecordedDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public decimal BookCost { get; set; }
        public string CompensationStatus { get; set; } = "Đã bồi thường"; // "Đã bồi thường" hoặc "Chưa bồi thường"
        public decimal CompensationAmount { get; set; }
        public string LiquidationStatus { get; set; } = "Đề xuất thanh lý"; // "Đề xuất thanh lý", "Đã thanh lý", "Chờ xử lý"
    }

    public class DamagedLostReportViewModel
    {
        public int SelectedYear { get; set; } = DateTime.Today.Year;
        public string SelectedCategory { get; set; } = "Tất cả";
        public string SelectedCondition { get; set; } = "Tất cả"; // Tất cả, Chỉ mất, Chỉ hỏng

        public int TotalDamagedCount => Records.FindAll(r => r.ConditionStatus == "Hỏng").Count;
        public int TotalLostCount => Records.FindAll(r => r.ConditionStatus == "Mất").Count;
        public decimal TotalLiquidationEstimatedValue => Records.Count * 95000;
        public decimal TotalCompensationCollected => 380000;

        public List<DamagedLostBookRecordViewModel> Records { get; set; } = new();
    }
}
