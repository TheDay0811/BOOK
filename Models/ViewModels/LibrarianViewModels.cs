using System;
using System.Collections.Generic;

namespace BOOK.Models.ViewModels
{
    public class PendingBorrowRequestViewModel
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberCode { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime PickupDueDate { get; set; }
        public string Status { get; set; } = "Chờ duyệt"; // Chờ duyệt, Đã duyệt, Đã hủy
    }

    public class BookReturnItemViewModel
    {
        public int BorrowId { get; set; }
        public string BorrowCode { get; set; } = string.Empty;
        public string MemberCode { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public string ConditionStatus { get; set; } = "Intact"; // Intact, MinorDamage, MajorDamage, Lost
        public decimal EstimatedFine { get; set; }
    }

    public class RenewalRequestViewModel
    {
        public int RequestId { get; set; }
        public int BorrowId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime CurrentDueDate { get; set; }
        public DateTime RequestedNewDueDate { get; set; }
        public int TimesRenewed { get; set; }
        public int MaxAllowedRenewals { get; set; } = 2;
        public string Reason { get; set; } = string.Empty;
    }

    public class OverdueAlertViewModel
    {
        public int BorrowId { get; set; }
        public string MemberCode { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public decimal AccumulatedFine { get; set; }
        public DateTime? LastNotifiedDate { get; set; }
    }

    public class LibrarianDashboardViewModel
    {
        public int PendingBorrowsCount => PendingRequests.Count;
        public int TodayReturnsCount { get; set; } = 0;
        public int PendingRenewalsCount => RenewalRequests.Count;
        public int OverdueAlertsCount => OverdueAlerts.Count;

        public List<PendingBorrowRequestViewModel> PendingRequests { get; set; } = new();
        public List<BookReturnItemViewModel> ActiveBorrowsForReturn { get; set; } = new();
        public List<RenewalRequestViewModel> RenewalRequests { get; set; } = new();
        public List<OverdueAlertViewModel> OverdueAlerts { get; set; } = new();
    }
}
