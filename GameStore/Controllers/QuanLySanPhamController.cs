using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Models;
using GameStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

[Authorize(Roles = "Admin")]
public class QuanLySanPhamController : Controller
{
    private readonly GameStoreDBContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public QuanLySanPhamController(GameStoreDBContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.Include(p => p.Brand).Include(p => p.Category).ToListAsync();
        var totalProductsInStock = await _context.Products.SumAsync(p => (int?)p.StockQuantity) ?? 0;
        var brands = await _context.Brands.ToListAsync();
        var categories = await _context.Categories.ToListAsync();

        var viewModel = new QuanLySanPhamViewModel
        {
            Products = products,
            TotalProductsInStock = totalProductsInStock,
            Brands = brands,
            Categories = categories
        };

        return View(viewModel);
    }

    [HttpPost]
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
                CategoryId = categoryId, // THÊM MỚI
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
                product.CategoryId = categoryId; // THÊM MỚI

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

    // Action cho GET request để hiển thị form chỉnh sửa sản phẩm
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        ViewBag.Brands = await _context.Brands.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync(); // ADDED
        return View(product);
    }

    // Action cho POST request để xử lý việc cập nhật sản phẩm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Sku,Description,Price,StockQuantity,ImageUrl,Status,CategoryId,BrandId,ImportPrice,CreatedAt")] Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // BỔ SUNG: Kiểm tra và xử lý các thuộc tính không có trong form
                var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // BỔ SUNG: Giữ lại SKU, ImageUrl, CreatedAt nếu không được chỉnh sửa
                product.Sku = existingProduct.Sku;
                product.ImageUrl = existingProduct.ImageUrl;
                product.CreatedAt = existingProduct.CreatedAt;

                // CẬP NHẬT: Gán ngày sửa sản phẩm
                product.UpdatedAt = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Brands = await _context.Brands.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}