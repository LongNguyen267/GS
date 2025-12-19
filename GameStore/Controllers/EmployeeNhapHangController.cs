using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using GameStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Collections.Generic;

[Authorize(Roles = "Employee")]
public class EmployeeNhapHangController : Controller
{
    private readonly GameStoreDBContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public EmployeeNhapHangController(GameStoreDBContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new NhapHangViewModel
        {
            Brands = await _context.Brands.ToListAsync(),
            // THÊM MỚI: Lấy danh sách các danh mục
            Categories = await _context.Categories.ToListAsync(),
            Products = await _context.Products.Include(p => p.Brand).ToListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    // THAY ĐỔI: Thêm tham số categoryId
    public async Task<IActionResult> NhapSanPham(string productName, int brandId, int categoryId, int quantity, decimal price, decimal importPrice, IFormFile productImage)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Name.Trim().ToLower() == productName.Trim().ToLower());
        string imageUrl = null;

        if (productImage != null && productImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "image/product");
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + productImage.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await productImage.CopyToAsync(fileStream);
            }
            imageUrl = "/image/product/" + uniqueFileName;
        }

        if (product == null)
        {
            var newProduct = new Product
            {
                Name = productName,
                Sku = Guid.NewGuid().ToString(),
                BrandId = brandId,
                // THÊM MỚI: Gán CategoryId
                CategoryId = categoryId,
                StockQuantity = quantity,
                Price = price,
                ImportPrice = importPrice,
                ImageUrl = imageUrl,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sản phẩm mới đã được thêm thành công!";
        }
        else
        {
            if (product.BrandId == brandId)
            {
                product.StockQuantity += quantity;
                product.Price = price;
                product.ImportPrice = importPrice;
                // THÊM MỚI: Cập nhật CategoryId
                product.CategoryId = categoryId;

                if (imageUrl != null)
                {
                    product.ImageUrl = imageUrl;
                }
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Số lượng sản phẩm đã được cập nhật thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Nhà cung cấp không khớp với sản phẩm hiện có!";
            }
        }
        return RedirectToAction(nameof(Index));
    }
}