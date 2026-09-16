using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Range(1, 30, ErrorMessage = "Thời gian mượn tối đa 30 ngày.")]
        public int DurationDays { get; set; } = 14;

        [MaxLength(255, ErrorMessage = "Ghi chú không quá 255 ký tự.")]
        public string? Notes { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý với nội quy thư viện để mượn sách.")]
        public bool AcceptTerms { get; set; }
    }

    public class BookDetailsViewModel
    {
        public BookItemViewModel Book { get; set; } = new();
        public List<BookReviewViewModel> Reviews { get; set; } = new();
        public List<BookItemViewModel> RelatedBooks { get; set; } = new();
        public CreateReviewInputModel NewReview { get; set; } = new();
        public BorrowRequestInputModel BorrowForm { get; set; } = new();
    }
}
