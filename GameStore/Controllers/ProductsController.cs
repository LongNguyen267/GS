using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Models;
using GameStore.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly GameStoreDBContext _context;

        public ProductsController(GameStoreDBContext context)
        {
            _context = context;
        }

        // ===== ACTION INDEX ĐÃ ĐƯỢC NÂNG CẤP ĐỂ LỌC 2 LỚP =====
        // SỬA ĐỔI 1: Thay đổi tham số đầu vào để nhận cả 'type' và 'category'
        // ===== ACTION INDEX ĐÃ SỬA LỖI LINQ TRANSLATION =====
        // ===== ACTION INDEX ĐÃ SỬA LỖI LINQ TRANSLATION =====
        public async Task<IActionResult> Index(string type, string category, string searchQuery, string sortOrder)
        {
            IQueryable<Product> productsQuery = _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.Parent);

            ViewData["PageTitle"] = "Tất cả sản phẩm";

            if (!string.IsNullOrEmpty(type))
            {
                if (type.Equals("MayGame", StringComparison.OrdinalIgnoreCase))
                {
                    var machineParentNames = new List<string> { "Console", "Handheld", "Handheld PC", "Retro Handheld" };
                    var machineParentCategoryIds = await _context.Categories
                        .Where(c => machineParentNames.Contains(c.Name))
                        .Select(c => c.Id).ToListAsync();
                    var machineChildCategoryIds = await _context.Categories
                        .Where(c => c.ParentId.HasValue && machineParentCategoryIds.Contains(c.ParentId.Value))
                        .Select(c => c.Id).ToListAsync();
                    var allMachineCategoryIds = machineParentCategoryIds.Concat(machineChildCategoryIds).ToList();
                    productsQuery = productsQuery.Where(p => p.CategoryId.HasValue && allMachineCategoryIds.Contains(p.CategoryId.Value));
                    ViewData["PageTitle"] = "Máy Game";
                }
                else if (type.Equals("PhuKien", StringComparison.OrdinalIgnoreCase) || type.Equals("DiaGame", StringComparison.OrdinalIgnoreCase))
                {
                    string parentCategoryName = type.Equals("PhuKien") ? "Phụ kiện" : "Đĩa Game";
                    var parentId = await _context.Categories
                        .Where(c => c.Name == parentCategoryName)
                        .Select(c => (int?)c.Id).FirstOrDefaultAsync();

                    if (parentId.HasValue)
                    {
                        var childIds = await _context.Categories
                            .Where(c => c.ParentId == parentId.Value)
                            .Select(c => c.Id).ToListAsync();
                        var allCategoryIds = childIds;
                        allCategoryIds.Add(parentId.Value);
                        productsQuery = productsQuery.Where(p => p.CategoryId.HasValue && allCategoryIds.Contains(p.CategoryId.Value));

                        var subCategories = await _context.Categories.Where(c => c.ParentId == parentId.Value).ToListAsync();
                        ViewData["SubCategories"] = subCategories;
                    }
                    else
                    {
                        productsQuery = productsQuery.Where(p => false);
                    }
                    ViewData["PageTitle"] = parentCategoryName;
                }
            }

            // Lọc theo danh mục con (chỉ dùng cho trang Phụ kiện khi bấm lọc)
            if (!string.IsNullOrEmpty(category))
            {
                string categoryUpper = category.ToUpper();
                productsQuery = productsQuery.Where(p =>
                    p.Category != null &&
                    (p.Category.Name.ToUpper() == categoryUpper ||
                     (p.Category.Parent != null && p.Category.Parent.Name.ToUpper() == categoryUpper))
                );
                ViewData["PageTitle"] = category;
            }

            // TRẢ LẠI LOGIC TÌM KIẾM CŨ CHO TRANG MÁY GAME
            if (!string.IsNullOrEmpty(searchQuery))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(searchQuery) ||
                    (p.Category != null && p.Category.Name.Contains(searchQuery)) ||
                    (p.Category != null && p.Category.Parent != null && p.Category.Parent.Name.Contains(searchQuery))
                );
            }

            // Sắp xếp...
            switch (sortOrder)
            {
                case "price-asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price-desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
            }

            ViewData["CurrentType"] = type;
            var products = await productsQuery.ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductGridPartial", products);
            }

            ViewData["Title"] = ViewData["PageTitle"];
            return View(products);
        }

        // GET: Products/Details/5 (giữ nguyên)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) { return NotFound(); }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) { return NotFound(); }

            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            var optionCategories = new List<Category>();
            var accessoryParentCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Phụ kiện");
            if (accessoryParentCategory != null)
            {
                optionCategories = await _context.Categories
                    .Where(c => c.ParentId == accessoryParentCategory.Id)
                    .Take(4)
                    .ToListAsync();
            }

            var viewModel = new ProductDetailsViewModel
            {
                MainProduct = product,
                RelatedProducts = relatedProducts,
                OptionCategories = optionCategories,
                MainImageUrl = product.ProductImages?.FirstOrDefault(img => img.IsDefault == true)?.ImageUrl
                             ?? product.ProductImages?.FirstOrDefault()?.ImageUrl
                             ?? "/images/placeholder.png",
                AllImageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                ReviewCount = product.Reviews?.Count ?? 0,
                AverageRating = product.Reviews?.Any() == true ? product.Reviews.Average(r => r.Rating) : 0
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return Json(new List<Product>());
            }

            var products = await _context.Products
                .Where(p => p.Name.ToLower().Contains(query.ToLower()))
                .Take(6) // Giới hạn số lượng kết quả gợi ý
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Json(products);
        }
    }
}
