using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using GameStore.ViewModels;
using GameStore.Services;
using System.ComponentModel.DataAnnotations;

namespace GameStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly GameStoreDBContext _context;
        private readonly IVnPayService _vnPayService;
        private const string CartSessionKey = "Cart";

        public CheckoutController(GameStoreDBContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        public class CartItem
        {
            public int ProductId { get; set; }
            public Product? Product { get; set; }
            public int Quantity { get; set; }
        }

        private List<CartItem> GetCart()
        {
            var cartString = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(cartString) ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(cartString) ?? new List<CartItem>();
        }

        // [LOGIC CHECK VOUCHER]
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string code, decimal currentTotal)
        {
            // 1. Tìm voucher
            var voucher = await _context.Notifications
                .FirstOrDefaultAsync(n => n.VoucherCode == code && n.IsActive == true);

            if (voucher == null) return Json(new { success = false, message = "Mã không hợp lệ!" });

            // [MỚI] Kiểm tra số lượng
            if (voucher.VoucherQuantity <= 0)
            {
                return Json(new { success = false, message = "Mã giảm giá này đã hết lượt sử dụng!" });
            }

            if (voucher.DiscountPercent <= 0) return Json(new { success = false, message = "Mã này không có giá trị giảm!" });

            // 2. Kiểm tra điều kiện Hãng/Loại
            var cartItems = GetCart();
            if (cartItems == null || !cartItems.Any()) return Json(new { success = false, message = "Giỏ hàng trống!" });

            var productIds = cartItems.Select(item => item.ProductId).ToList();
            var productsInDb = await _context.Products
                                             .Include(p => p.Brand).Include(p => p.Category)
                                             .Where(p => productIds.Contains(p.Id)).ToListAsync();

            decimal eligibleAmount = 0;

            foreach (var item in cartItems)
            {
                var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    bool isEligible = true;
                    if (voucher.ApplyToBrandId.HasValue && voucher.ApplyToBrandId != product.BrandId) isEligible = false;
                    if (voucher.ApplyToCategoryId.HasValue && voucher.ApplyToCategoryId != product.CategoryId) isEligible = false;

                    if (isEligible) eligibleAmount += (product.Price * item.Quantity);
                }
            }

            if (eligibleAmount == 0) return Json(new { success = false, message = "Mã không áp dụng cho sản phẩm trong giỏ!" });

            decimal discountAmount = eligibleAmount * voucher.DiscountPercent / 100;
            decimal realTotalCart = productsInDb.Sum(p => p.Price * cartItems.First(c => c.ProductId == p.Id).Quantity);
            decimal newTotal = realTotalCart - discountAmount;

            return Json(new
            {
                success = true,
                discountPercent = voucher.DiscountPercent,
                discountAmount = discountAmount,
                newTotal = newTotal,
                message = $"Áp dụng thành công! Giảm {voucher.DiscountPercent}%."
            });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            foreach (var item in cart) item.Product = await _context.Products.FindAsync(item.ProductId);

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                ShippingAddress = _context.Users.Find(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))?.Address,
                PhoneNumber = _context.Users.Find(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))?.PhoneNumber
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CartItems = GetCart();
                foreach (var item in model.CartItems) item.Product = await _context.Products.FindAsync(item.ProductId);
                return View("Index", model);
            }

            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            var cartItemsList = GetCart();
            if (cartItemsList == null || !cartItemsList.Any()) return RedirectToAction("Index", "Cart");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var productIds = cartItemsList.Select(item => item.ProductId).ToList();

            var productsInCart = await _context.Products
                                                .Include(p => p.Brand).Include(p => p.Category)
                                                .Where(p => productIds.Contains(p.Id)).ToListAsync();

            decimal totalAmount = 0;

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ShippingAddress = model.ShippingAddress,
                PaymentMethod = model.PaymentMethod,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                PhoneNumber = model.PhoneNumber
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItemsList)
            {
                var product = productsInCart.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PriceAtPurchase = product.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                    totalAmount += product.Price * item.Quantity;
                }
            }

            // [XỬ LÝ TRỪ SỐ LƯỢNG VOUCHER]
            if (!string.IsNullOrEmpty(model.VoucherCode))
            {
                var voucher = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.VoucherCode == model.VoucherCode && n.IsActive == true);

                // Kiểm tra lại lần cuối xem còn lượt không (tránh trường hợp nhiều người mua cùng lúc)
                if (voucher != null && voucher.DiscountPercent > 0 && voucher.VoucherQuantity > 0)
                {
                    decimal eligibleAmount = 0;
                    foreach (var item in cartItemsList)
                    {
                        var product = productsInCart.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            bool isEligible = true;
                            if (voucher.ApplyToBrandId.HasValue && voucher.ApplyToBrandId != product.BrandId) isEligible = false;
                            if (voucher.ApplyToCategoryId.HasValue && voucher.ApplyToCategoryId != product.CategoryId) isEligible = false;

                            if (isEligible) eligibleAmount += (product.Price * item.Quantity);
                        }
                    }

                    if (eligibleAmount > 0)
                    {
                        decimal discountAmount = eligibleAmount * voucher.DiscountPercent / 100;
                        totalAmount -= discountAmount;

                        // [MỚI] TRỪ SỐ LƯỢNG ĐI 1
                        voucher.VoucherQuantity = voucher.VoucherQuantity - 1;
                        _context.Notifications.Update(voucher); // Cập nhật lại voucher
                    }
                }
            }

            order.TotalAmount = totalAmount;
            _context.Update(order);

            // Lưu tất cả thay đổi (bao gồm cả Order và VoucherQuantity)
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);

            if (model.PaymentMethod == "VnPay")
            {
                var vnPayModel = new VnPayRequestModel
                {
                    Amount = (double)order.TotalAmount,
                    CreatedDate = DateTime.Now,
                    Description = "Thanh toan don hang " + order.Id,
                    FullName = model.FullName ?? "",
                    OrderId = order.Id
                };
                return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
            }

            return RedirectToAction(nameof(OrderConfirmation), new { orderId = order.Id });
        }

        // --- CÁC HÀM KHÁC GIỮ NGUYÊN ---
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Lỗi thanh toán VNPay: {response?.VnPayResponseCode}";
                return RedirectToAction("PaymentFail");
            }
            try
            {
                var orderId = int.Parse(response.OrderId);
                var order = _context.Orders.FirstOrDefault(x => x.Id == orderId);
                if (order != null)
                {
                    order.PaymentStatus = "Paid";
                    order.PaymentTransactionId = response.TransactionId;
                    _context.SaveChanges();
                }
                TempData["Message"] = "Thanh toán thành công";
                return RedirectToAction("PaymentSuccess", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi xử lý hóa đơn: " + ex.Message;
                return RedirectToAction("PaymentFail");
            }
        }

        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            return View(order);
        }

        public IActionResult PaymentFail()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}