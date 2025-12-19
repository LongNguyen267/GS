using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    public class AdminDonHangController : Controller
    {
        private readonly GameStoreDBContext _context;

        public AdminDonHangController(GameStoreDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                                    .Include(o => o.User) // Bao gồm thông tin người dùng
                                    .OrderByDescending(o => o.OrderDate)
                                    .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                                    .Include(o => o.OrderDetails)
                                        .ThenInclude(od => od.Product) // Bao gồm thông tin sản phẩm
                                    .Include(o => o.User)
                                    .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = newStatus;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}