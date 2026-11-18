using System;
using System.Collections.Generic;

namespace BOOK.Models.ViewModels
{
    public class WishlistItemViewModel
    {
        public int MaYeuThich { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PublisherName { get; set; } = string.Empty;
        public int PublishYear { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }
        public bool IsAvailable => AvailableCopies > 0;
        public DateTime AddedDate { get; set; }
    }

    public class WishlistIndexViewModel
    {
        public List<WishlistItemViewModel> Items { get; set; } = new();
    }
}
