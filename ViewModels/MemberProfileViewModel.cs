using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BOOK.ViewModels
{
    public class MemberProfileViewModel
    {
        // Thông tin độc giả / sinh viên
        public string FullName { get; set; } = "Trần Văn Hoàng";
        public string StudentCode { get; set; } = "24DH112970";
        public string Email { get; set; } = "hoang.tv@student.university.edu.vn";
        public string PhoneNumber { get; set; } = "0912 345 678";
        public string Faculty { get; set; } = "Khoa Công Nghệ Thông Tin";
        public string AvatarUrl { get; set; } = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80";

        // Thông tin Thẻ thư viện liên kết
        public string CardNumber { get; set; } = "LIB-24DH112970";
        public DateTime CardIssueDate { get; set; } = DateTime.Today.AddMonths(-6);
        public DateTime CardExpiryDate { get; set; } = DateTime.Today.AddMonths(6);
        public string CardStatus { get; set; } = "Sử dụng được"; // "Sử dụng được" hoặc "Không sử dụng"
        
        public bool IsCardLocked => CardStatus != "Sử dụng được" || DateTime.Today > CardExpiryDate;
        public string LockReason { get; set; } = string.Empty;

        // Quy định hạn mức mượn
        public int MaxBorrowLimit { get; set; } = 3;
        public int CurrentBorrowCount => ActiveLoans?.Count ?? 0;
        public bool CanBorrowMore => CurrentBorrowCount < MaxBorrowLimit && !IsCardLocked;
        public int BorrowPercentage => (int)Math.Round((double)CurrentBorrowCount / MaxBorrowLimit * 100);

        // Danh sách sách đang mượn
        public List<ActiveBorrowItemViewModel> ActiveLoans { get; set; } = new();

        // Lịch sử các lần mượn - trả
        public List<BorrowHistoryItemViewModel> HistoryLoans { get; set; } = new();

        // Tổng kết phí phạt & bồi thường
        public decimal TotalFinesPaid { get; set; } = 36000;
        public decimal TotalUnpaidFines { get; set; } = 0;
        public bool HasUnpaidFines => TotalUnpaidFines > 0;

        // Model hỗ trợ cập nhật thông tin
        public UpdateProfileInputModel UpdateModel { get; set; } = new();
    }

    public class ActiveBorrowItemViewModel
    {
        public int BorrowId { get; set; }
        public string BookCode { get; set; } = "MS01"; // Mã sách
        public string ControlNumber { get; set; } = "002"; // Số kiểm soát (Bản sao)
        public string DisplayCode => $"{BookCode} - Bản {ControlNumber}";
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; } // Tối đa 15 ngày
        public int RenewalCount { get; set; } = 0; // Đã gia hạn bao nhiêu lần

        // Trạng thái hạn trả
        public int DaysRemaining => (DueDate.Date - DateTime.Today).Days;
        public bool IsOverdue => DateTime.Today > DueDate.Date;
        public bool IsNearDue => !IsOverdue && DaysRemaining <= 2;
        public int OverdueDays => IsOverdue ? (DateTime.Today - DueDate.Date).Days : 0;
        public bool CanRenew => !IsOverdue && RenewalCount < 2;

        public string DueStatusBadgeClass
        {
            get
            {
                if (IsOverdue) return "bg-rose-100 text-rose-800 border-rose-200 animate-pulse";
                if (IsNearDue) return "bg-amber-100 text-amber-800 border-amber-200";
                return "bg-emerald-100 text-emerald-800 border-emerald-200";
            }
        }

        public string DueStatusText
        {
            get
            {
                if (IsOverdue) return $"Quá hạn {OverdueDays} ngày";
                if (DaysRemaining == 0) return "Hôm nay là hạn chót";
                if (DaysRemaining == 1) return "Còn 1 ngày (Sắp hết hạn)";
                return $"Còn {DaysRemaining} ngày";
            }
        }
    }

    public class BorrowHistoryItemViewModel
    {
        public int Id { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string ControlNumber { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public string ReturnCondition { get; set; } = "Bình thường"; // Bình thường, Hư hại trang, Mất sách
        public decimal CompensationFee { get; set; } = 0;
        public string PaymentStatus { get; set; } = "Miễn phí"; // Miễn phí, Đã thanh toán, Chưa thanh toán
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateProfileInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Mã sinh viên (MSSV)")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Khoa / Chuyên ngành")]
        public string Faculty { get; set; } = string.Empty;
    }
}
