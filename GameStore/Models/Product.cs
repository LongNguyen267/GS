using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace GameStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Sku { get; set; } = null!;
        public string? Description { get; set; }
        public string? Specifications { get; set; } // <<-- ĐÃ THÊM THUỘC TÍNH NÀY
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = null!;
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal? ImportPrice { get; set; }

        // Các thuộc tính navigation
        public virtual Brand? Brand { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Thuộc tính để xử lý file hình ảnh được upload
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}