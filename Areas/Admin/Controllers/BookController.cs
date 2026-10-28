using BOOK.Data;
using BOOK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using BOOK.Models.Entities;
namespace BOOK.Areas.Admin.Controllers
{
    public class BookFormViewModel
    {
        public int MaSach { get; set; }
        [Required(ErrorMessage = "Nhập mã sách")] public string MaSachCode { get; set; } = "";
        [Required(ErrorMessage = "Nhập tên sách")] public string TenSach { get; set; } = "";
        [Required(ErrorMessage = "Nhập tác giả")] public string TacGia { get; set; } = "";
        public int NamXuatBan { get; set; } = DateTime.Today.Year;
        public int MaTheLoai { get; set; }
        public int MaNXB { get; set; }
    }
    public class InventoryRow { public string MaSachCode { get; set; } = ""; public string TenSach { get; set; } = ""; public int Tong, Con, ChoMuon, Hong, Mat; }
    public class AuthorRow { public string Name { get; set; } = ""; public int Books { get; set; } }


    [Area("Admin")]
    [Authorize(Roles = "Admin,ThuThu")]
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



        private async Task LoadLookups()
        {
            ViewBag.TheLoais = new SelectList(await _context.TheLoais.OrderBy(t => t.TenTheLoai).ToListAsync(), "MaTheLoai", "TenTheLoai");
            ViewBag.NhaXuatBans = new SelectList(await _context.NhaXuatBans.OrderBy(n => n.TenNXB).ToListAsync(), "MaNXB", "TenNXB");
            ViewBag.TacGias = await _context.Sachs.Select(s => s.TacGia).Distinct().OrderBy(a => a).ToListAsync();
        }

        // ---- Story 18: CRUD đầu sách ----
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View("Edit", new BookFormViewModel());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var s = await _context.Sachs.FindAsync(id);
            if (s == null) return NotFound();
            await LoadLookups();
            return View(new BookFormViewModel
            {
                MaSach = s.MaSach,
                MaSachCode = s.MaSachCode,
                TenSach = s.TenSach,
                TacGia = s.TacGia,
                NamXuatBan = s.NamXuatBan,
                MaTheLoai = s.MaTheLoai,
                MaNXB = s.MaNXB
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(BookFormViewModel m)
        {
            if (await _context.Sachs.AnyAsync(s => s.MaSachCode == m.MaSachCode && s.MaSach != m.MaSach))
                ModelState.AddModelError(nameof(m.MaSachCode), "Mã sách đã tồn tại.");
            if (!ModelState.IsValid) { await LoadLookups(); return View("Edit", m); }

            Sach? s = m.MaSach == 0 ? new Sach() : await _context.Sachs.FindAsync(m.MaSach);
            if (s == null) return NotFound();
            s.MaSachCode = m.MaSachCode.Trim();
            s.TenSach = m.TenSach.Trim();
            s.TacGia = m.TacGia.Trim();
            s.NamXuatBan = m.NamXuatBan;
            s.MaTheLoai = m.MaTheLoai;
            s.MaNXB = m.MaNXB;
            if (m.MaSach == 0) _context.Sachs.Add(s);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu đầu sách.";
            return RedirectToAction(nameof(Details), new { id = s.MaSach });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.Sachs.Include(x => x.DauSachs).ThenInclude(d => d.ChiTietPhieuMuons)
                .FirstOrDefaultAsync(x => x.MaSach == id);
            if (s == null) return NotFound();
            if (s.DauSachs.Any(d => d.ChiTietPhieuMuons.Any()))
                TempData["Error"] = "Sách đã có lịch sử mượn, không thể xóa.";
            else
            {
                _context.DauSachs.RemoveRange(s.DauSachs);
                _context.Sachs.Remove(s);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đầu sách.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---- Story 15 + 20: bản sao / tình trạng ----
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCopy(int bookId, string? notes)
        {
            var existing = await _context.DauSachs.Where(d => d.MaSach == bookId).Select(d => d.SoKiemSoat).ToListAsync();
            var next = existing.Select(x => int.TryParse(x, out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
            _context.DauSachs.Add(new DauSach { MaSach = bookId, SoKiemSoat = next.ToString("D3"), TinhTrang = "Còn", GhiChu = notes ?? "" });
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm bản sao số kiểm soát {next:D3}.";
            return RedirectToAction(nameof(Details), new { id = bookId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCopy(int bookId, int copyId, string tinhTrang, string? ghiChu)
        {
            var copy = await _context.DauSachs.FindAsync(copyId);
            if (copy == null || !new[] { "Còn", "Cho mượn", "Mất", "Hỏng" }.Contains(tinhTrang))
            { TempData["Error"] = "Dữ liệu không hợp lệ."; return RedirectToAction(nameof(Details), new { id = bookId }); }

            var dangMuon = await _context.ChiTietPhieuMuons.AnyAsync(c => c.MaDauSach == copyId && c.NgayTraThucTe == null);
            if (dangMuon && tinhTrang != "Cho mượn")
                TempData["Error"] = "Bản này đang được mượn, hãy ghi nhận trả sách ở quầy mượn trả.";
            else if (!dangMuon && tinhTrang == "Cho mượn")
                TempData["Error"] = "Trạng thái 'Cho mượn' chỉ được đặt khi lập phiếu mượn.";
            else
            {
                copy.TinhTrang = tinhTrang;
                copy.GhiChu = ghiChu ?? "";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật tình trạng.";
            }
            return RedirectToAction(nameof(Details), new { id = bookId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Liquidate(int bookId, int copyId)
        {
            var copy = await _context.DauSachs.FindAsync(copyId);
            if (copy == null || (copy.TinhTrang != "Hỏng" && copy.TinhTrang != "Mất"))
                TempData["Error"] = "Chỉ thanh lý bản sao Hỏng hoặc Mất.";
            else
            {
                copy.GhiChu = $"{copy.GhiChu} [Đề xuất thanh lý {DateTime.Today:dd/MM/yyyy}]".Trim();
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã đánh dấu đề xuất thanh lý.";
            }
            return RedirectToAction(nameof(Details), new { id = bookId });
        }

        public async Task<IActionResult> Inventory()
        {
            var rows = await _context.Sachs.OrderBy(s => s.MaSachCode).Select(s => new InventoryRow
            {
                MaSachCode = s.MaSachCode,
                TenSach = s.TenSach,
                Tong = s.DauSachs.Count,
                Con = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                ChoMuon = s.DauSachs.Count(d => d.TinhTrang == "Cho mượn"),
                Hong = s.DauSachs.Count(d => d.TinhTrang == "Hỏng"),
                Mat = s.DauSachs.Count(d => d.TinhTrang == "Mất")
            }).ToListAsync();
            return View(rows);
        }

        // ---- Story 21: tác giả (lưu dạng text trong Sach nên quản lý = xem + đổi tên) ----
        public async Task<IActionResult> Authors()
        {
            var rows = await _context.Sachs.GroupBy(s => s.TacGia)
                .Select(g => new AuthorRow { Name = g.Key, Books = g.Count() })
                .OrderBy(a => a.Name).ToListAsync();
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameAuthor(string oldName, string newName)
        {
            newName = newName?.Trim() ?? "";
            if (newName == "") TempData["Error"] = "Tên tác giả không được rỗng.";
            else
            {
                var list = await _context.Sachs.Where(s => s.TacGia == oldName).ToListAsync();
                foreach (var s in list) s.TacGia = newName;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã đổi tên tác giả cho {list.Count} sách.";
            }
            return RedirectToAction(nameof(Authors));
        }
    }
}