using System;
using System.Collections.Generic;

namespace GameStore.Models;

public partial class Order
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    // Trạng thái đơn hàng (VD: Đang xử lý, Đang giao, Đã hủy)
    public string Status { get; set; } = null!;

    // Phương thức thanh toán (VD: "COD", "VnPay", "Momo")
    public string? PaymentMethod { get; set; }

    // === [MỚI THÊM] CÁC TRƯỜNG CHO THANH TOÁN ONLINE ===

    // Mã giao dịch từ cổng thanh toán (VD: VNP1424567). 
    // Dùng để tra soát khi có khiếu nại.
    public string? PaymentTransactionId { get; set; }

    // Trạng thái thanh toán (VD: "Unpaid" - Chưa trả, "Paid" - Đã trả, "Failed" - Thất bại)
    // Bạn nên set mặc định là "Unpaid" hoặc false
    public string PaymentStatus { get; set; } = "Unpaid";

    // ===================================================

    public int? DiscountId { get; set; }

    public virtual Discount? Discount { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual User? User { get; set; }

    public string? PhoneNumber { get; set; }
}