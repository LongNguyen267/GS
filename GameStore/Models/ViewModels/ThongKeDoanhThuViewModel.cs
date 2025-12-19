using System;

namespace GameStore.ViewModels
{
    public class ThongKeDoanhThuViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueByDate { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now.Date;
        public DateTime EndDate { get; set; } = DateTime.Now.Date;
        public Dictionary<string, decimal> DailyRevenueData { get; set; } = new Dictionary<string, decimal>();
    }
}