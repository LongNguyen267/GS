using System;

namespace GameStore.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal DailyRevenue { get; set; }      // Doanh thu hôm nay
        public decimal MonthlyRevenue { get; set; }    // Doanh thu tháng này
        public decimal TotalRevenue { get; set; }      // Tổng doanh thu
        public int NewOrdersCount { get; set; }        // Số đơn hàng mới (Chờ xử lý)
        public int TotalProductsCount { get; set; }    // Tổng số sản phẩm
        public int TotalCustomersCount { get; set; }   // Tổng số khách hàng
    }
}