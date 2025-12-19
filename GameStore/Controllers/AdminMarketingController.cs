using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GameStore.Models.Services; // <-- QUAN TRỌNG: Namespace chứa file EmailSender của bro

namespace GameStore.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được
    public class AdminMarketingController : Controller
    {
        private readonly GameStoreDBContext _context;
        private readonly IEmailSender _emailSender; // Gọi Interface gửi mail

        // Constructor: Nhận DBContext và EmailSender từ hệ thống (Dependency Injection)
        public AdminMarketingController(GameStoreDBContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 1. Hiển thị giao diện soạn mail
        public IActionResult Index()
        {
            return View();
        }

        // 2. Xử lý gửi mail hàng loạt
        [HttpPost]
        public async Task<IActionResult> SendMarketingEmail(string subject, string content)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Tiêu đề và Nội dung!";
                return RedirectToAction("Index");
            }

            // Lấy danh sách email khách hàng (Chỉ lấy những người có email)
            var emails = await _context.Users
                                       .Where(u => !string.IsNullOrEmpty(u.Email))
                                       .Select(u => u.Email)
                                       .ToListAsync();

            if (emails.Count == 0)
            {
                TempData["ErrorMessage"] = "Hệ thống chưa có khách hàng nào để gửi mail!";
                return RedirectToAction("Index");
            }

            int count = 0;
            try
            {
                // Vòng lặp gửi mail cho từng người
                foreach (var email in emails)
                {
                    // Trang trí nội dung HTML cho mail đẹp hơn
                    string htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e3e6f0; border-radius: 8px; background-color: #ffffff;'>
                            <div style='text-align: center; border-bottom: 3px solid #1cc88a; padding-bottom: 15px; margin-bottom: 20px;'>
                                <h2 style='color: #1cc88a; margin: 0; text-transform: uppercase;'>GameStore News</h2>
                            </div>
                            
                            <div style='color: #333; font-size: 16px; line-height: 1.6;'>
                                {content}
                            </div>

                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; text-align: center; font-size: 13px; color: #858796;'>
                                <p><strong>Chill Game Store</strong> - Thế giới game bản quyền.</p>
                                <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            </div>
                        </div>";

                    // Gọi hàm gửi mail (Sử dụng tài khoản Gmail ChillGameStoreBITV)
                    await _emailSender.SendEmailAsync(email, subject, htmlBody);
                    count++;
                }

                TempData["SuccessMessage"] = $"Đã gửi thành công chiến dịch cho {count} khách hàng!";
            }
            catch (Exception ex)
            {
                // Bắt lỗi nếu Gmail chặn hoặc mạng lag
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi mail: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}