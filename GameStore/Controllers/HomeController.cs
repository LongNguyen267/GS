using System.Diagnostics;
using GameStore.Models;
using GameStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace GameStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly GameStoreDBContext _context;

        public HomeController(GameStoreDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchQuery, string? category)
        {
            // Kiểm tra vai trò của người dùng và chuyển hướng đến trang dashboard phù hợp
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole("Employee"))
                {
                    return RedirectToAction("Index", "Employee");
                }
            }

            var featuredProducts = await _context.Products
                .OrderBy(p => Guid.NewGuid())
                .Take(10)
                .ToListAsync();

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(searchQuery.ToLower()));
            }

            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category.Name == category);
            }

            var discounts = await _context.Discounts.Where(d => d.IsActive == true).ToListAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = featuredProducts,
                Products = await productsQuery.ToListAsync(),
                Discounts = discounts,
                SearchQuery = searchQuery,
                CurrentCategory = category
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Contact()
        {
            var brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
            return View(brands);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // --- [ĐÃ SỬA] HÀM LẤY THÔNG BÁO + HÌNH ẢNH ---
        [HttpGet]
        public IActionResult GetThongBao()
        {
            var list = _context.Notifications
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedDate)
                .Take(10) // Lấy 10 tin mới nhất
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Message,
                    x.CreatedDate,
                    x.VoucherCode,

                    // [QUAN TRỌNG] Thêm dòng này để lấy ảnh gửi xuống Layout
                    imageUrl = x.ImageUrl
                })
                .ToList();

            return Json(new { data = list });
        }
        // ------------------------------------------------
    }
}