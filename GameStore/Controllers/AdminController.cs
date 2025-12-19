using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using GameStore.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Controllers
{
    [Authorize(Roles = "Admin")] // Bảo mật: Chỉ Admin mới vào được
    public class AdminController : Controller
    {
        private readonly GameStoreDBContext _context;

        public AdminController(GameStoreDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today; // Lấy ngày hôm nay (00:00:00)

            // 1. Tính Doanh thu HÔM NAY (Trừ đơn đã Hủy)
            var dailyRevenue = await _context.Orders
                .Where(o => o.OrderDate.HasValue
                            && o.OrderDate.Value.Date == today
                            && o.Status != "Cancelled") // Quan trọng: Không tính đơn hủy
                .SumAsync(o => o.TotalAmount);

            // 2. Tính Doanh thu THÁNG NÀY (Trừ đơn đã Hủy)
            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate.HasValue
                            && o.OrderDate.Value.Month == today.Month
                            && o.OrderDate.Value.Year == today.Year
                            && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            // 3. Tính TỔNG Doanh thu toàn thời gian
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            // 4. Đếm số đơn hàng MỚI (Trạng thái là Pending - Chờ xử lý)
            // Cái này quan trọng để Admin biết có bao nhiêu đơn cần làm ngay
            var newOrdersCount = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            // 5. Đếm tổng số sản phẩm và khách hàng
            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users.CountAsync();

            // Đổ dữ liệu vào ViewModel
            var viewModel = new AdminDashboardViewModel
            {
                DailyRevenue = dailyRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalRevenue = totalRevenue,
                NewOrdersCount = newOrdersCount,
                TotalProductsCount = totalProducts,
                TotalCustomersCount = totalCustomers
            };

            return View(viewModel);
        }
    }
}