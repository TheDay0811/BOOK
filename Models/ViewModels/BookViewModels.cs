using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BOOK.Models.ViewModels
{
    public class BookItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PublisherName { get; set; } = string.Empty;
        public int PublishYear { get; set; }
        public string ISBN { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }
        public string ShelfLocation { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public int BorrowCount { get; set; }
        public bool IsAvailable => AvailableCopies > 0;
        public string Description { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string Language { get; set; } = "Tiếng Việt";
    }

    public class BookListViewModel
    {
        public string? SearchQuery { get; set; }
        public List<int> SelectedCategoryIds { get; set; } = new();
        public List<int> SelectedAuthorIds { get; set; } = new();
        public string? AvailabilityFilter { get; set; } = "all"; // all, available, out_of_stock
        public string? SortBy { get; set; } = "newest"; // newest, popular, title_asc, rating_desc
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 9;

        // Filter Options
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Authors { get; set; } = new();
        public List<SelectListItem> Publishers { get; set; } = new();

        // Data
        public List<BookItemViewModel> Books { get; set; } = new();
    }

    public class BookReviewViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserAvatarUrl { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserRole { get; set; } = "Độc giả";
    }

    public class CreateReviewInputModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao.")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Vui lòng nhập nội dung cảm nhận của bạn.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung nhận xét từ 10 đến 1000 ký tự.")]
        public string Content { get; set; } = string.Empty;
    }

    public class BorrowRequestInputModel
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày dự kiến đến nhận sách.")]
        [DataType(DataType.Date)]
        public DateTime PickupDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn thời hạn mượn.")]
        [Range(1, 15, ErrorMessage = "Thời gian mượn tối đa 15 ngày theo quy định.")]
        public int DurationDays { get; set; } = 15;

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [MaxLength(255, ErrorMessage = "Ghi chú không quá 255 ký tự.")]
        public string? Notes { get; set; }

        public bool AcceptTerms { get; set; }
    }

    public class BookDetailsViewModel
    {
        public BookItemViewModel Book { get; set; } = new();
        public List<BookReviewViewModel> Reviews { get; set; } = new();
        public List<BookItemViewModel> RelatedBooks { get; set; } = new();
        public CreateReviewInputModel NewReview { get; set; } = new();
        public BorrowRequestInputModel BorrowForm { get; set; } = new();
        public List<BookCopyViewModel> Copies { get; set; } = new();
        public string UserCardStatus { get; set; } = "Sử dụng được";
        public bool HasUnreturnedBooks { get; set; } = false;
        public int CurrentBorrowedCount { get; set; } = 0;
        public bool IsUserAuthenticated { get; set; } = false;
    }

    public class CreateBookInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tựa sách.")]
        [Display(Name = "Tên tựa sách")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Mã tựa sách (Code)")]
        public string? BookCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tác giả.")]
        [Display(Name = "Tác giả")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn thể loại.")]
        [Display(Name = "Thể loại")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà xuất bản.")]
        [Display(Name = "Nhà xuất bản")]
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm xuất bản.")]
        [Range(1900, 2050, ErrorMessage = "Năm xuất bản không hợp lệ.")]
        [Display(Name = "Năm xuất bản")]
        public int PublishYear { get; set; } = DateTime.Today.Year;

        [Range(1, 100, ErrorMessage = "Số lượng bản sao ban đầu từ 1 đến 100.")]
        [Display(Name = "Số bản sao ban đầu")]
        public int InitialCopiesCount { get; set; } = 3;

        [Display(Name = "Vị trí kệ sách")]
        public string? ShelfLocation { get; set; } = "Kệ A1 - Tầng 2";

        [Display(Name = "URL ảnh bìa")]
        public string? CoverImageUrl { get; set; }

        [Display(Name = "File ảnh bìa")]
        public IFormFile? ImageFile { get; set; }
    }

    public class EditBookInputModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên tựa sách.")]
        public string Title { get; set; } = string.Empty;

        public string BookCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tác giả.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn thể loại.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà xuất bản.")]
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm xuất bản.")]
        [Range(1900, 2050, ErrorMessage = "Năm xuất bản không hợp lệ.")]
        public int PublishYear { get; set; }

        public string? ShelfLocation { get; set; }
        public string? CoverImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }
    }

    public class CategoryItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public string IconName { get; set; } = "book-open";
        public string ColorClass { get; set; } = "text-navy-800 bg-blue-50";
    }

    public class PublisherItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }

    public class HomeIndexViewModel
    {
        public List<CategoryItemViewModel> Categories { get; set; } = new();
        public List<BookItemViewModel> NewBooks { get; set; } = new();
        public List<BookItemViewModel> PopularBooks { get; set; } = new();
        public int TotalBooksCount { get; set; }
        public int TotalCopiesCount { get; set; }
    }
}
