using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;

namespace BOOK.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Notification/GetNotifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian") || User.IsInRole("ThuThu");

            var query = _context.ThongBaos.AsQueryable();

            if (isStaff)
            {
                query = query.Where(n => n.TargetUser == username || 
                                         n.TargetUser == "AdminGroup" || 
                                         n.TargetUser == "Librarian" || 
                                         n.TargetUser == "Admin" ||
                                         n.TargetUser == "admin@huflit.edu.vn" ||
                                         n.TargetUser == "thuthu@huflit.edu.vn");
            }
            else
            {
                query = query.Where(n => n.TargetUser == username);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    id = n.MaThongBao,
                    title = n.Title,
                    message = n.Message,
                    linkUrl = !string.IsNullOrEmpty(n.LinkUrl) ? n.LinkUrl : "#",
                    type = n.Type,
                    isRead = n.DaDoc,
                    timeAgo = n.CreatedAt.ToString("HH:mm dd/MM/yyyy")
                })
                .ToListAsync();

            var unreadCount = await query.CountAsync(n => !n.DaDoc);

            return Json(new
            {
                success = true,
                unreadCount = unreadCount,
                notifications = notifications
            });
        }

        // POST: /Notification/MarkAsRead?id=5
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int? id)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "";
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian") || User.IsInRole("ThuThu");

            var query = _context.ThongBaos.AsQueryable();
            if (isStaff)
            {
                query = query.Where(n => n.TargetUser == username || 
                                         n.TargetUser == "AdminGroup" || 
                                         n.TargetUser == "Librarian" || 
                                         n.TargetUser == "Admin" ||
                                         n.TargetUser == "admin@huflit.edu.vn" ||
                                         n.TargetUser == "thuthu@huflit.edu.vn");
            }
            else
            {
                query = query.Where(n => n.TargetUser == username);
            }

            if (id.HasValue && id.Value > 0)
            {
                var notif = await query.FirstOrDefaultAsync(n => n.MaThongBao == id.Value);
                if (notif != null)
                {
                    notif.DaDoc = true;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Mark all as read
                var unreadList = await query.Where(n => !n.DaDoc).ToListAsync();
                foreach (var n in unreadList)
                {
                    n.DaDoc = true;
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }
    }
}
