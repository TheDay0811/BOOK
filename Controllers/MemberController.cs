using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BOOK.Models.ViewModels;

namespace BOOK.Controllers
{
    public class MemberController : Controller
    {
        // GET: /Member -> Chuyển hướng sang Hồ sơ thật tại /Account/Profile
        public IActionResult Index()
        {
            return RedirectToAction("Profile", "Account");
        }
    }
}
