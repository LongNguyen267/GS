using System.Collections.Generic;

namespace GameStore.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal DailyRevenue { get; set; }
        public int NewOrdersCount { get; set; }
        public int TotalProductsCount { get; set; }
        // Thêm các thuộc tính khác nếu bạn muốn hiển thị thêm dữ liệu trên dashboard admin
    }
}