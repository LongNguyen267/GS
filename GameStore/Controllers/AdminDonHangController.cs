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

        // Xem danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Cập nhật trạng thái đơn hàng
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái mới
            order.Status = newStatus;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // --- QUAN TRỌNG: Gửi thông báo thành công sang View ---
            // Dòng này sẽ kích hoạt SweetAlert2 ở bên View
            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            // -----------------------------------------------------

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}