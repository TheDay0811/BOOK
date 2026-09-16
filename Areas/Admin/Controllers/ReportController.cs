using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportController : Controller
    {
        // GET: /Admin/Report/DamagedLostBooks
        public IActionResult DamagedLostBooks(int? year, string? condition)
        {
            var model = new DamagedLostReportViewModel
            {
                SelectedYear = year ?? DateTime.Today.Year,
                SelectedCondition = condition ?? "Tất cả",
                Records = new List<DamagedLostBookRecordViewModel>
                {
                    new DamagedLostBookRecordViewModel
                    {
                        Id = 1,
                        BookCode = "CS-001",
                        BookTitle = "Thiết Kế Kiến Trúc Phần Mềm Doanh Nghiệp",
                        CategoryName = "Công Nghệ Thông Tin",
                        ControlNumber = "004",
                        ConditionStatus = "Hỏng",
                        RecordedDate = DateTime.Today.AddDays(-12),
                        Notes = "Rách 20 trang phụ lục, gãy gáy sách do dính nước",
                        StudentName = "Lê Thị Mai Linh",
                        StudentCode = "23110255",
                        BookCost = 145000,
                        CompensationStatus = "Đã bồi thường",
                        CompensationAmount = 70000,
                        LiquidationStatus = "Đề xuất thanh lý"
                    },
                    new DamagedLostBookRecordViewModel
                    {
                        Id = 2,
                        BookCode = "AI-002",
                        BookTitle = "Học Máy & Trí Tuệ Nhân Tạo Ứng Dụng",
                        CategoryName = "Khoa Học Dữ Liệu",
                        ControlNumber = "003",
                        ConditionStatus = "Mất",
                        RecordedDate = DateTime.Today.AddDays(-25),
                        Notes = "Sinh viên báo mất trong đợt đi thực tập doanh nghiệp",
                        StudentName = "Vũ Đình Trọng",
                        StudentCode = "21120042",
                        BookCost = 160000,
                        CompensationStatus = "Đã bồi thường",
                        CompensationAmount = 160000,
                        LiquidationStatus = "Đề xuất thanh lý"
                    },
                    new DamagedLostBookRecordViewModel
                    {
                        Id = 3,
                        BookCode = "KT-003",
                        BookTitle = "Quản Trị Chiến Lược & Chuyển Đổi Số",
                        CategoryName = "Kinh Tế & Quản Trị",
                        ControlNumber = "002",
                        ConditionStatus = "Mất",
                        RecordedDate = DateTime.Today.AddDays(-40),
                        Notes = "Mất sách chưa rõ nguyên nhân, sinh viên chưa đến bồi thường",
                        StudentName = "Nguyễn Văn Hùng",
                        StudentCode = "20110415",
                        BookCost = 120000,
                        CompensationStatus = "Chưa bồi thường",
                        CompensationAmount = 0,
                        LiquidationStatus = "Chờ xử lý (Thẻ đang khóa)"
                    },
                    new DamagedLostBookRecordViewModel
                    {
                        Id = 4,
                        BookCode = "SEC-004",
                        BookTitle = "Cơ Sở An Toàn Thông Tin & Mật Mã",
                        CategoryName = "An Toàn Thông Tin",
                        ControlNumber = "006",
                        ConditionStatus = "Hỏng",
                        RecordedDate = DateTime.Today.AddDays(-60),
                        Notes = "Sách cũ bị mối mọt xâm thực theo thời gian",
                        StudentName = "Lưu kho",
                        StudentCode = "KH-01",
                        BookCost = 90000,
                        CompensationStatus = "Đã bồi thường",
                        CompensationAmount = 0,
                        LiquidationStatus = "Đề xuất thanh lý"
                    }
                }
            };

            return View(model);
        }
    }
}
