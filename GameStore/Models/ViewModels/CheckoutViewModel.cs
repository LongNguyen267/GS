using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GameStore.Controllers;

namespace GameStore.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string ShippingAddress { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = null!;

        public List<CheckoutController.CartItem> CartItems { get; set; } = new List<CheckoutController.CartItem>();
        public string? VoucherCode { get; set; }
    }
}