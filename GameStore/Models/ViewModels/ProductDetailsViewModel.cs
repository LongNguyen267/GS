// Thư mục: ViewModels/ProductDetailsViewModel.cs

using GameStore.Models;
using System.Collections.Generic;

namespace GameStore.ViewModels
{
    public class ProductDetailsViewModel
    {
        // Dữ liệu gốc
        public Product MainProduct { get; set; } = new Product();
        public IEnumerable<Product> RelatedProducts { get; set; } = new List<Product>(); // <-- Đổi thành IEnumerable
        public IEnumerable<Category> OptionCategories { get; set; } = new List<Category>(); // <-- Đổi thành IEnumerable

        // Dữ liệu đã được xử lý sẵn để View hiển thị
        public string MainImageUrl { get; set; } = "";
        public IEnumerable<string> AllImageUrls { get; set; } = new List<string>(); // <-- Đổi thành IEnumerable
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }

        // Có thể giữ lại constructor để khởi tạo nếu muốn
        public ProductDetailsViewModel()
        {
            RelatedProducts = new List<Product>();
            OptionCategories = new List<Category>();
            AllImageUrls = new List<string>();
        }
    }
}