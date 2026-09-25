using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.ViewModels;

namespace BOOK.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Book/Index
        public async Task<IActionResult> Index(string? search, string? category, string? status)
        {
            var query = _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(s => s.TenSach.ToLower().Contains(q) || s.TacGia.ToLower().Contains(q));
            }

            var sachList = await query.ToListAsync();

            var books = sachList.Select(s => new BookItemViewModel
            {
                Id = s.MaSach,
                Title = s.TenSach,
                Author = s.TacGia,
                CategoryName = s.TheLoai.TenTheLoai,
                PublisherName = s.NhaXuatBan.TenNXB,
                PublishYear = s.NamXuatBan,
                TotalCopies = s.DauSachs.Count,
                AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn")
            }).ToList();

            return View(books);
        }

        // GET: /Admin/Book/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                return NotFound();
            }

            var model = new BookTitleManageViewModel
            {
                BookId = sach.MaSach,
                BookCode = sach.MaSachCode,
                Title = sach.TenSach,
                Author = sach.TacGia,
                CategoryName = sach.TheLoai.TenTheLoai,
                PublisherName = sach.NhaXuatBan.TenNXB,
                PublishYear = sach.NamXuatBan,
                Copies = sach.DauSachs.Select(d => new BookCopyViewModel
                {
                    Id = d.MaDauSach,
                    BookId = sach.MaSach,
                    BookCode = sach.MaSachCode,
                    ControlNumber = d.SoKiemSoat,
                    Status = d.TinhTrang,
                    Notes = d.GhiChu
                }).ToList()
            };

            return View(model);
        }
    }
}