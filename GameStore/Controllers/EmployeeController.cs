using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Models;
using GameStore.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    // Cập nhật: Controller này chỉ dành cho Admin
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly GameStoreDBContext _context;

        public EmployeeController(GameStoreDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalProductsInStock = await _context.Products.SumAsync(p => (int?)p.StockQuantity) ?? 0;

            var viewModel = new EmployeeDashboardViewModel
            {
                TotalProductsInStock = totalProductsInStock
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders.ToListAsync();

            var viewModel = new EmployeeDashboardViewModel
            {
                Orders = orders
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ViewOrders(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                                        .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                                        .OrderByDescending(o => o.OrderDate)
                                        .ToListAsync();

            var viewModel = new EmployeeDashboardViewModel
            {
                Orders = orders
            };
            return View("Orders", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = newStatus;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Orders));
        }
    }
}