using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; // <-- THÊM CÁI NÀY

public class KhachHangController : Controller
{
    private readonly GameStoreDBContext _context;

    public KhachHangController(GameStoreDBContext context)
    {
        _context = context;
    }

    // === 1. CHỨC NĂNG CỦA ADMIN ===
    // (Action Index của bạn, thêm [Authorize(Roles = "Admin")])
    [Authorize(Roles = "Admin")] // <-- BÁO CHO HỆ THỐNG BIẾT CHỈ ADMIN ĐƯỢC VÀO ĐÂY
    public async Task<IActionResult> Index()
    {
        // ... (code liệt kê tất cả khách hàng của bạn)
        var usersWithRoles = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();
        var customers = usersWithRoles.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Customer")).ToList();
        return View(customers);
    }


    // === 2. CÁC CHỨC NĂNG CỦA KHÁCH HÀNG ===

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> OrderHistory()
    {
        // 1. LẤY ID DẠNG CHUỖI
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 2. CHUYỂN ĐỔI SANG SỐ (INT)
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userIdInt))
        {
            return Challenge(); // Yêu cầu đăng nhập hoặc báo lỗi
        }

        // 3. SO SÁNH SỐ VỚI SỐ (ĐÃ SỬA)
        var userOrders = await _context.Orders
            .Where(o => o.UserId == userIdInt) // <-- SỬA TỪ userId sang userIdInt
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(userOrders);
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> OrderDetails(int id) // 'id' này là OrderId
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

        if (order.UserId != userIdInt)
        {
            return Forbid();
        }

        return View(order);
    }
}