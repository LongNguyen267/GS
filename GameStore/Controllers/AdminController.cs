using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using GameStore.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO; // [QUAN TRỌNG] Thêm cái này để xử lý File

public class AdminController : Controller
{
    private readonly GameStoreDBContext _context;

    public AdminController(GameStoreDBContext context)
    {
        _context = context;
    }

    // --- DASHBOARD ---
    public async Task<IActionResult> Index()
    {
        var viewModel = new AdminDashboardViewModel
        {
            DailyRevenue = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == DateTime.Now.Date)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,

            NewOrdersCount = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == DateTime.Now.Date)
                .CountAsync(),

            TotalProductsCount = await _context.Products.CountAsync()
        };

        return View(viewModel);
    }

    // ==========================================================
    // PHẦN 1: QUẢN LÝ THÔNG BÁO
    // ==========================================================

    [HttpGet]
    public IActionResult CreateNotification()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNotification(Notification model)
    {
        if (ModelState.IsValid)
        {
            // --- XỬ LÝ ẢNH CHO THÔNG BÁO ---
            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "notifications");

                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                string filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                model.ImageUrl = "/images/notifications/" + fileName;
            }
            // --------------------------------

            model.CreatedDate = DateTime.Now;
            model.IsActive = true;

            _context.Notifications.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng thông báo thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // ==========================================================
    // PHẦN 2: QUẢN LÝ MÃ GIẢM GIÁ (ĐÃ CẬP NHẬT LƯU ẢNH)
    // ==========================================================

    [HttpGet]
    public IActionResult ManageVoucher()
    {
        ViewBag.Brands = _context.Brands.ToList();
        ViewBag.Categories = _context.Categories.ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVoucher(Notification model)
    {
        // [BƯỚC CHỮA BỆNH QUAN TRỌNG NHẤT]
        // Xóa lỗi "Thiếu tiêu đề" khỏi danh sách kiểm tra
        // Để code có thể chạy tiếp xuống dưới mà không bị chặn lại
        ModelState.Remove("Title");
        ModelState.Remove("Message"); // Xóa luôn Message cho chắc (nếu ní có cài required)

        if (ModelState.IsValid)
        {
            // --- CODE LƯU ẢNH ---
            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "notifications");

                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                string filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                model.ImageUrl = "/images/notifications/" + fileName;
            }

            // --- CÁC THÔNG TIN KHÁC ---
            model.CreatedDate = DateTime.Now;
            model.IsActive = true;

            // Tự động điền Tiêu đề (Bây giờ nó mới có tác dụng nè!)
            if (string.IsNullOrEmpty(model.Title))
            {
                model.Title = $"🎁 TẶNG BẠN MÃ: {model.VoucherCode} - GIẢM {model.DiscountPercent}%";
            }

            // Tự động điền Nội dung
            if (string.IsNullOrEmpty(model.Message))
            {
                model.Message = $"Nhập mã {model.VoucherCode} khi thanh toán để được giảm ngay {model.DiscountPercent}%. Số lượng có hạn!";
            }

            if (model.ApplyToBrandId == 0) model.ApplyToBrandId = null;
            if (model.ApplyToCategoryId == 0) model.ApplyToCategoryId = null;

            _context.Notifications.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tạo mã & Đăng thông báo thành công!";
            return RedirectToAction(nameof(ManageVoucher));
        }

        // Nếu vẫn còn lỗi khác (VD: chưa nhập Mã, chưa nhập %) thì load lại trang
        ViewBag.Brands = _context.Brands.ToList();
        ViewBag.Categories = _context.Categories.ToList();
        return View("ManageVoucher", model);
    }

    // ==========================================================
    // PHẦN 3: API AJAX
    // ==========================================================
    [HttpGet]
    public IActionResult GetCategoriesByBrand(int brandId)
    {
        if (brandId == 0)
        {
            var allCats = _context.Categories
                .Select(c => new { c.Id, c.Name })
                .ToList();
            return Json(allCats);
        }

        var categories = _context.Products
            .Where(p => p.BrandId == brandId)
            .Include(p => p.Category)
            .Select(p => p.Category)
            .Distinct()
            .Where(c => c != null)
            .Select(c => new { c.Id, c.Name })
            .ToList();

        return Json(categories);
    }
}