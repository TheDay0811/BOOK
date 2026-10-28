using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Models.Entities;
using BOOK.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
namespace BOOK.Areas.Admin.Controllers
{
    public class InventoryRow { public string MaSachCode { get; set; } = ""; public string TenSach { get; set; } = ""; public int Tong, Con, ChoMuon, Hong, Mat; }
    public class AuthorRow { public string Name { get; set; } = ""; public int Books { get; set; } }


    [Area("Admin")]
    [Authorize(Roles = "Admin,Librarian,ThuThu")]
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BookController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Admin/Book/Index
        public async Task<IActionResult> Index(string? search, int? categoryId, string? status)
        {
            var query = _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .Include(s => s.DauSachs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(s => s.TenSach.ToLower().Contains(q) ||
                                         s.TacGia.ToLower().Contains(q) ||
                                         s.MaSachCode.ToLower().Contains(q));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.MaTheLoai == categoryId.Value);
            }

            if (status == "available")
            {
                query = query.Where(s => s.DauSachs.Any(d => d.TinhTrang == "Còn"));
            }
            else if (status == "out_of_stock")
            {
                query = query.Where(s => !s.DauSachs.Any(d => d.TinhTrang == "Còn"));
            }

            var sachList = await query.OrderByDescending(s => s.MaSach).ToListAsync();

            var books = sachList.Select(s => new BookItemViewModel
            {
                Id = s.MaSach,
                Title = s.TenSach,
                Author = s.TacGia,
                CategoryName = s.TheLoai.TenTheLoai,
                PublisherName = s.NhaXuatBan.TenNXB,
                PublishYear = s.NamXuatBan,
                ISBN = s.MaSachCode,
                TotalCopies = s.DauSachs.Count,
                AvailableCopies = s.DauSachs.Count(d => d.TinhTrang == "Còn"),
                ShelfLocation = $"Kệ {s.MaSachCode} - Tầng 2",
                CoverImageUrl = !string.IsNullOrEmpty(s.HinhAnh) ? s.HinhAnh : "/uploads/books/default-book.svg"
            }).ToList();

            ViewBag.Categories = await _context.TheLoais
                .Select(t => new SelectListItem { Value = t.MaTheLoai.ToString(), Text = t.TenTheLoai })
                .ToListAsync();

            ViewBag.Publishers = await _context.NhaXuatBans
                .Select(n => new SelectListItem { Value = n.MaNXB.ToString(), Text = n.TenNXB })
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentStatus = status;

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

        // POST: /Admin/Book/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ các thông tin bắt buộc.";
                return RedirectToAction(nameof(Index));
            }

            string code = string.IsNullOrWhiteSpace(model.BookCode)
                ? $"BK-{DateTime.Now:MMddHHmm}"
                : model.BookCode.Trim().ToUpper();

            if (await _context.Sachs.AnyAsync(s => s.MaSachCode == code))
            {
                TempData["Error"] = $"Mã tựa sách [{code}] đã tồn tại trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            string? imagePath = await SaveBookImageAsync(model.ImageFile, model.CoverImageUrl);

            var sach = new Sach
            {
                MaSachCode = code,
                TenSach = model.Title.Trim(),
                TacGia = model.Author.Trim(),
                NamXuatBan = model.PublishYear,
                MaTheLoai = model.CategoryId,
                MaNXB = model.PublisherId,
                HinhAnh = imagePath
            };

            int copiesCount = Math.Clamp(model.InitialCopiesCount, 1, 50);
            for (int i = 1; i <= copiesCount; i++)
            {
                sach.DauSachs.Add(new DauSach
                {
                    SoKiemSoat = $"{i:D3}",
                    TinhTrang = "Còn",
                    GhiChu = model.ShelfLocation ?? "Kho lưu thông"
                });
            }

            _context.Sachs.Add(sach);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Thêm mới tựa sách \"{sach.TenSach}\" ({code}) thành công cùng {copiesCount} bản sao vật lý!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Book/GetBook/1 (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetBook(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.TheLoai)
                .Include(s => s.NhaXuatBan)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tựa sách." });
            }

            return Json(new
            {
                success = true,
                id = sach.MaSach,
                bookCode = sach.MaSachCode,
                title = sach.TenSach,
                author = sach.TacGia,
                categoryId = sach.MaTheLoai,
                publisherId = sach.MaNXB,
                publishYear = sach.NamXuatBan,
                shelfLocation = $"Kệ {sach.MaSachCode} - Tầng 2",
                coverImageUrl = sach.HinhAnh ?? ""
            });
        }

        // POST: /Admin/Book/Edit (AJAX hoặc Form POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditBookInputModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var sach = await _context.Sachs.FindAsync(model.BookId);
            if (sach == null)
            {
                if (isAjax) return Json(new { success = false, message = "Không tìm thấy tựa sách cần sửa." });
                TempData["Error"] = "Không tìm thấy tựa sách cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            sach.TenSach = model.Title.Trim();
            sach.TacGia = model.Author.Trim();
            sach.MaTheLoai = model.CategoryId;
            sach.MaNXB = model.PublisherId;
            sach.NamXuatBan = model.PublishYear;

            var newImage = await SaveBookImageAsync(model.ImageFile, model.CoverImageUrl);
            if (!string.IsNullOrEmpty(newImage))
            {
                sach.HinhAnh = newImage;
            }

            await _context.SaveChangesAsync();

            var msg = $"Cập nhật tựa sách \"{sach.TenSach}\" thành công!";
            if (isAjax)
            {
                return Json(new { success = true, message = msg });
            }

            TempData["Success"] = msg;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Book/AddCopies
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCopies(int bookId, int numberOfCopies, string? shelfLocation)
        {
            var sach = await _context.Sachs
                .Include(s => s.DauSachs)
                .FirstOrDefaultAsync(s => s.MaSach == bookId);

            if (sach == null)
            {
                TempData["Error"] = "Không tìm thấy tựa sách để bổ sung số lượng cuốn.";
                return RedirectToAction(nameof(Index));
            }

            if (numberOfCopies < 1 || numberOfCopies > 50)
            {
                TempData["Error"] = "Số lượng cuốn sách bổ sung phải từ 1 đến 50 cuốn mỗi lần.";
                return RedirectToAction(nameof(Index));
            }

            // Tìm số kiểm soát cao nhất hiện có
            int highestControlNumber = 0;
            foreach (var d in sach.DauSachs)
            {
                if (int.TryParse(d.SoKiemSoat, out int num) && num > highestControlNumber)
                {
                    highestControlNumber = num;
                }
            }

            var note = !string.IsNullOrWhiteSpace(shelfLocation) ? shelfLocation.Trim() : "Kho lưu thông - Bổ sung mới";

            for (int i = 1; i <= numberOfCopies; i++)
            {
                int newNum = highestControlNumber + i;
                sach.DauSachs.Add(new DauSach
                {
                    SoKiemSoat = $"{newNum:D3}",
                    TinhTrang = "Còn",
                    GhiChu = note
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã bổ sung thành công {numberOfCopies} cuốn sách mới cho tựa \"{sach.TenSach}\" (Số kiểm soát từ {highestControlNumber + 1:D3} đến {highestControlNumber + numberOfCopies:D3}).";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> SaveBookImageAsync(IFormFile? file, string? fallbackUrl)
        {
            if (file != null && file.Length > 0)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (allowedExts.Contains(ext))
                {
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "books");
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    var fileName = $"book_{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    return $"/uploads/books/{fileName}";
                }
            }
            if (!string.IsNullOrWhiteSpace(fallbackUrl))
            {
                return fallbackUrl.Trim();
            }
            return null;
        }

        // POST: /Admin/Book/Delete/1
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.DauSachs)
                .FirstOrDefaultAsync(s => s.MaSach == id);

            if (sach == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tựa sách để xóa." });
            }

            // Kiểm tra ràng buộc: có cuốn nào đang "Cho mượn" không
            if (sach.DauSachs.Any(d => d.TinhTrang == "Cho mượn"))
            {
                return Json(new { success = false, message = "Không thể xóa tựa sách này vì hiện có bản sao đang được sinh viên mượn!" });
            }

            // Kiểm tra nếu có chi tiết phiếu mượn chưa trả
            var copyIds = sach.DauSachs.Select(d => d.MaDauSach).ToList();
            var hasActiveLoans = await _context.ChiTietPhieuMuons
                .AnyAsync(c => copyIds.Contains(c.MaDauSach) && c.NgayTraThucTe == null);

            if (hasActiveLoans)
            {
                return Json(new { success = false, message = "Không thể xóa sách vì đang nằm trong phiếu mượn chưa hoàn tất!" });
            }

            _context.DauSachs.RemoveRange(sach.DauSachs);
            _context.Sachs.Remove(sach);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã xóa tựa sách \"{sach.TenSach}\" và các bản sao liên quan khỏi kho." });
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