using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [MỚI] Dùng cho [NotMapped]
using Microsoft.AspNetCore.Http; // [MỚI] Dùng cho IFormFile

namespace GameStore.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Display(Name = "Nội dung")]
        public string Message { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // --- CÁC TRƯỜNG CŨ CỦA VOUCHER ---
        public string? VoucherCode { get; set; }
        public int DiscountPercent { get; set; } = 0;
        public int? ApplyToBrandId { get; set; }
        public int? ApplyToCategoryId { get; set; }
        public int VoucherQuantity { get; set; } = 0;

        // --- [MỚI] PHẦN XỬ LÝ ẢNH ---

        [Display(Name = "Đường dẫn ảnh")]
        public string? ImageUrl { get; set; } // Cái này sẽ tạo cột trong Database

        [NotMapped] // Cái này chỉ dùng để hứng file upload, KHÔNG tạo cột trong Database
        [Display(Name = "Chọn hình ảnh")]
        public IFormFile? ImageFile { get; set; }
    }
}