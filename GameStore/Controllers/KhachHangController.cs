using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly GameStoreDBContext _context;

        public KhachHangController(GameStoreDBContext context)
        {
            _context = context;
        }

        // ==========================================
        // KHU VỰC 1: CHỨC NĂNG CỦA ADMIN
        // ==========================================

        // 1. Xem danh sách khách hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            // Lọc ra những user có role là Customer
            var customers = usersWithRoles.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Customer")).ToList();

            return View(customers);
        }

        // 2. [GET] Hiển thị form Sửa thông tin khách hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // 3. [POST] Xử lý lưu thông tin sau khi sửa
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user)
        {
            // Tìm user cũ trong database
            var existingUser = await _context.Users.FindAsync(user.Id);

            if (existingUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các thông tin cho phép sửa
                    existingUser.FullName = user.FullName;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Email = user.Email;

                    // Lưu ý: KHÔNG cập nhật Password, Username, CreatedAt tại đây để bảo mật
                    // Nếu muốn sửa địa chỉ, thêm dòng: existingUser.Address = user.Address;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }

        // ==========================================
        // KHU VỰC 2: CHỨC NĂNG CỦA KHÁCH HÀNG (CUSTOMER)
        // ==========================================

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> OrderHistory()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userIdInt))
            {
                return Challenge();
            }

            var userOrders = await _context.Orders
                .Where(o => o.UserId == userIdInt)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(userOrders);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userIdInt))
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Bảo mật: Chỉ xem được đơn hàng của chính mình
            if (order.UserId != userIdInt)
            {
                return Forbid();
            }

            return View(order);
        }

        // Hàm phụ trợ kiểm tra user tồn tại
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}