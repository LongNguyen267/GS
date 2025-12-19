using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";
        private readonly GameStoreDBContext _context;

        public CartController(GameStoreDBContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng từ session
        private List<CartItem> GetCart()
        {
            var cartString = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(cartString) ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(cartString) ?? new List<CartItem>();
        }

        // Lưu giỏ hàng vào session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        // === HÀM HELPER MỚI: Tính tổng tiền (bất đồng bộ) ===
        private async Task<decimal> CalculateGrandTotal(List<CartItem> cart)
        {
            decimal total = 0;
            var productIds = cart.Select(item => item.ProductId).ToList();

            // Lấy tất cả sản phẩm trong giỏ hàng chỉ bằng 1 lệnh gọi DB
            var products = await _context.Products
                                     .Where(p => productIds.Contains(p.Id))
                                     .ToListAsync();

            foreach (var item in cart)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    total += product.Price * item.Quantity;
                }
            }
            return total;
        }

        // Action hiển thị giỏ hàng
        public async Task<IActionResult> Index() // <-- Chuyển sang async
        {
            var cart = GetCart();

            // Lấy thông tin sản phẩm cho các item trong giỏ
            var productIds = cart.Select(item => item.ProductId).ToList();
            var products = await _context.Products
                                        .Where(p => productIds.Contains(p.Id))
                                        .ToListAsync();

            foreach (var item in cart)
            {
                // Gán thông tin sản phẩm từ danh sách đã lấy
                item.Product = products.FirstOrDefault(p => p.Id == item.ProductId);
            }

            // Lọc ra những item không tìm thấy sản phẩm (có thể đã bị xóa khỏi DB)
            var validCart = cart.Where(item => item.Product != null).ToList();
            if (validCart.Count != cart.Count)
            {
                // Nếu có sự khác biệt, lưu lại giỏ hàng hợp lệ
                SaveCart(validCart);
            }

            return View(validCart);
        }

        // Action thêm sản phẩm (đã cập nhật cho AJAX)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!" });
        }

        // === ACTION MỚI: Tăng số lượng ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int productId)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);
            var product = await _context.Products.FindAsync(productId);

            if (cartItem == null || product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tìm thấy" });
            }

            // (Tùy chọn: Bạn có thể thêm kiểm tra tồn kho ở đây)
            // if (cartItem.Quantity + 1 > product.Stock) { ... return error ... }

            cartItem.Quantity++;
            SaveCart(cart);

            decimal newSubtotal = product.Price * cartItem.Quantity;
            decimal newGrandTotal = await CalculateGrandTotal(cart);

            return Json(new
            {
                success = true,
                newQuantity = cartItem.Quantity,
                newSubtotal = newSubtotal.ToString("N0") + "đ", // Trả về chuỗi đã định dạng
                newGrandTotal = newGrandTotal.ToString("N0") + "đ",
                cartCount = cart.Count,
                itemRemoved = false
            });
        }

        // === ACTION MỚI: Giảm số lượng ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int productId)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tìm thấy" });
            }

            bool itemRemoved = false;
            cartItem.Quantity--;

            if (cartItem.Quantity <= 0)
            {
                cart.Remove(cartItem);
                itemRemoved = true;
            }

            SaveCart(cart);

            decimal newSubtotal = 0;
            if (!itemRemoved)
            {
                // Chỉ cần lấy giá khi item chưa bị xóa
                var product = await _context.Products.FindAsync(productId);
                if (product != null) newSubtotal = product.Price * cartItem.Quantity;
            }

            decimal newGrandTotal = await CalculateGrandTotal(cart);

            return Json(new
            {
                success = true,
                newQuantity = itemRemoved ? 0 : cartItem.Quantity,
                newSubtotal = newSubtotal.ToString("N0") + "đ",
                newGrandTotal = newGrandTotal.ToString("N0") + "đ",
                cartCount = cart.Count,
                itemRemoved = itemRemoved
            });
        }

        // === CẬP NHẬT: Xóa sản phẩm (cho AJAX) ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                SaveCart(cart);
            }

            decimal newGrandTotal = await CalculateGrandTotal(cart);

            // Trả về JSON để JavaScript cập nhật giao diện
            return Json(new
            {
                success = true,
                newGrandTotal = newGrandTotal.ToString("N0") + "đ",
                cartCount = cart.Count,
                itemRemoved = true,
                removedProductId = productId // Gửi kèm ID để JS biết xóa item nào
            });
        }
    }
}