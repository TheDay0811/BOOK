using System;
using System.Collections.Generic;

namespace BOOK.Models.ViewModels
{
    public class AdminKpiCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Subtext { get; set; } = string.Empty;
        public string ChangeText { get; set; } = string.Empty;
        public bool IsPositive { get; set; } = true;
        public string IconName { get; set; } = string.Empty;
        public string ColorClass { get; set; } = "blue"; // blue, emerald, amber, rose
    }

    public class QuickPendingBorrowViewModel
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime PickupDueDate { get; set; }
        public string Priority { get; set; } = "Bình thường"; // Gấp, Ưu tiên, Bình thường
    }

    public class AdminDashboardViewModel
    {
        public AdminKpiCardViewModel KpiTotalBooks { get; set; } = new();
        public AdminKpiCardViewModel KpiActiveBorrows { get; set; } = new();
        public AdminKpiCardViewModel KpiPendingBorrows { get; set; } = new();
        public AdminKpiCardViewModel KpiOverdueBooks { get; set; } = new();

        public List<QuickPendingBorrowViewModel> RecentPendingRequests { get; set; } = new();
        
        // Chart statistics
        public List<string> MonthlyLabels { get; set; } = new();
        public List<int> MonthlyBorrowCounts { get; set; } = new();
        public List<int> MonthlyReturnCounts { get; set; } = new();

        public List<string> CategoryLabels { get; set; } = new();
        public List<int> CategoryBookCounts { get; set; } = new();
    }
}
