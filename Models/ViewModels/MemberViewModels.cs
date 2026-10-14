using System;
using System.Collections.Generic;

namespace BOOK.Models.ViewModels
{
    public class MemberActiveBorrowViewModel
    {
        public int BorrowId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewalCount { get; set; }
        public bool IsOverdue => DateTime.Today > DueDate;
        public int OverdueDays => IsOverdue ? (DateTime.Today - DueDate).Days : 0;
        public decimal LateFee => OverdueDays * 2000;
    }

    public class MemberBorrowHistoryViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public string Status { get; set; } = "Đã hoàn tất";
        public decimal FinePaid { get; set; }
    }

    public class MemberLateFeeViewModel
    {
        public decimal TotalUnpaidFine { get; set; }
        public string VietQrUrl { get; set; } = string.Empty;
        public string BankName { get; set; } = "Ngân hàng TMCP Ngoại Thương Việt Nam (Vietcombank)";
        public string AccountNumber { get; set; } = "102888666999";
        public string AccountHolder { get; set; } = "THƯ VIỆN ĐẠI HỌC NGOẠI NGỮ TIN HỌC TPHCM (HUFLIT)";
        public string TransferDescription { get; set; } = "NOPPHAT HUFLIT";
    }

    public class MemberPortalViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string MemberCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Faculty { get; set; } = "Khoa Công Nghệ Thông Tin (HUFLIT)";
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);

        public List<MemberActiveBorrowViewModel> ActiveLoans { get; set; } = new();
        public List<MemberBorrowHistoryViewModel> BorrowHistory { get; set; } = new();
        public MemberLateFeeViewModel LateFeeInfo { get; set; } = new();
    }
}
