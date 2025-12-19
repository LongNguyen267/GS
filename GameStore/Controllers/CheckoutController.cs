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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            foreach (var item in cart)
            {
                item.Product = await _context.Products.FindAsync(item.ProductId);
            }

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
            // --- VALIDATION ---
            if (!ModelState.IsValid)
            {
                model.CartItems = GetCart();
                foreach (var item in model.CartItems)
                {
                    item.Product = await _context.Products.FindAsync(item.ProductId);
                }
                return View("Index", model);
            }

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItemsList = GetCart();
            if (cartItemsList == null || !cartItemsList.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            var userId = int.Parse(userIdString);

            // --- LƯU ĐƠN HÀNG VÀO DATABASE ---
            var productIds = cartItemsList.Select(item => item.ProductId).ToList();
            var productsInCart = await _context.Products
                                            .Where(p => productIds.Contains(p.Id))
                                            .ToListAsync();

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

            order.TotalAmount = totalAmount;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng
            HttpContext.Session.Remove(CartSessionKey);

            // --- XỬ LÝ VNPAY ---
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

            // --- XỬ LÝ COD ---
            return RedirectToAction(nameof(OrderConfirmation), new { orderId = order.Id });
        }

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
                // Tìm đơn hàng và cập nhật trạng thái
                var orderId = int.Parse(response.OrderId);
                var order = _context.Orders.FirstOrDefault(x => x.Id == orderId);

                if (order != null)
                {
                    order.PaymentStatus = "Paid";
                    order.PaymentTransactionId = response.TransactionId;
                    _context.SaveChanges();
                }

                TempData["Message"] = "Thanh toán thành công";

                // [ĐÃ SỬA] Chuyển hướng kèm theo orderId để trang Success có dữ liệu hiển thị
                return RedirectToAction("PaymentSuccess", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi xử lý hóa đơn: " + ex.Message;
                return RedirectToAction("PaymentFail");
            }
        }

        // [ĐÃ SỬA] Thêm tham số orderId và lấy dữ liệu từ DB
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            // Lấy thông tin đơn hàng từ DB
            var order = await _context.Orders.FindAsync(orderId);

            // Trả về View kèm theo Model là Order
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
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }
    }
}