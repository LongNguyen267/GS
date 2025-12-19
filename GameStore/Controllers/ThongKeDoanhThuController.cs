using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameStore.ViewModels;

public class ThongKeDoanhThuController : Controller
{
    private readonly GameStoreDBContext _context;

    public ThongKeDoanhThuController(GameStoreDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
    {
        var viewModel = new ThongKeDoanhThuViewModel
        {
            StartDate = startDate ?? DateTime.Now.Date,
            EndDate = endDate ?? DateTime.Now.Date
        };

        viewModel.TotalRevenue = await _context.Orders
            .Where(o => o.Status == "Delivered")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        viewModel.RevenueByDate = await _context.Orders
            .Where(o => o.Status == "Delivered" && o.OrderDate >= viewModel.StartDate && o.OrderDate <= viewModel.EndDate.AddDays(1))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        // THÊM MỚI: Lấy dữ liệu doanh thu hàng ngày cho biểu đồ
        var last30Days = DateTime.Now.Date.AddDays(-29);
        viewModel.DailyRevenueData = await _context.Orders
            .Where(o => o.Status == "Delivered" && o.OrderDate >= last30Days)
            .GroupBy(o => o.OrderDate.Value.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .ToDictionaryAsync(x => x.Date.ToString("dd/MM"), x => x.Total);

        return View(viewModel);
    }
}