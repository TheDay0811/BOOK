using System;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class BorrowController : Controller
    {
        [HttpPost]
        public IActionResult CreateRequest(BorrowRequestInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin đặt mượn chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }

            // In real app, persist borrow request to database
            TempData["Success"] = $"Đặt mượn thành công sách \"{model.BookTitle}\"! Vui lòng đến nhận sách trước 17:00 ngày {model.PickupDate:dd/MM/yyyy}.";
            return RedirectToAction("Details", "Book", new { id = model.BookId });
        }
    }
}
