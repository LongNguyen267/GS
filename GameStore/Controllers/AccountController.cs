using Microsoft.AspNetCore.Mvc;
using GameStore.ViewModels;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly GameStoreDBContext _context;

        public AccountController(GameStoreDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại.");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = model.Password,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
                if (customerRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Login));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName)
                    };

                    foreach (var userRole in user.UserRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, "Login");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(claimsPrincipal);

                    if (user.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (user.UserRoles.Any(ur => ur.Role.Name == "Employee"))
                    {
                        return RedirectToAction("Index", "Employee");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // [THÊM MỚI] Action để hiển thị form quên mật khẩu
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // [THÊM MỚI] Action để xử lý yêu cầu quên mật khẩu
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Message"] = "Nếu địa chỉ email tồn tại trong hệ thống, một liên kết đặt lại mật khẩu sẽ được gửi đến bạn.";
                return View();
            }

            // THAY ĐỔI: Kiểm tra và xóa token cũ nếu có
            var existingToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (existingToken != null)
            {
                _context.PasswordResetTokens.Remove(existingToken);
                await _context.SaveChangesAsync();
            }

            // Tạo token đặt lại mật khẩu mới
            var resetToken = Guid.NewGuid().ToString();
            var passwordResetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetToken,
                ExpiryDate = DateTime.Now.AddHours(2)
            };
            _context.PasswordResetTokens.Add(passwordResetToken);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Account", new { token = resetToken }, Request.Scheme);

            var emailBody = $"Chào bạn, <br/><br/>Nhấp vào liên kết sau để đặt lại mật khẩu của bạn: <a href='{resetLink}'>Đặt lại mật khẩu</a><br/><br/>Liên kết này sẽ hết hạn sau 2 giờ.";
            await SendEmailAsync(email, "Đặt lại mật khẩu", emailBody);

            TempData["Message"] = "Một liên kết đặt lại mật khẩu đã được gửi đến email của bạn.";
            return View();
        }

        // [THÊM MỚI] Action để hiển thị form đặt lại mật khẩu
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var passwordResetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token && t.ExpiryDate > DateTime.Now);
            if (passwordResetToken == null)
            {
                return View("Error", new ErrorViewModel { RequestId = "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." });
            }
            return View(new ResetPasswordViewModel { Token = token });
        }

        // [THÊM MỚI] Action để xử lý đặt lại mật khẩu
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var passwordResetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == model.Token && t.ExpiryDate > DateTime.Now);
            if (passwordResetToken == null)
            {
                ModelState.AddModelError("", "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            var user = await _context.Users.FindAsync(passwordResetToken.UserId);
            if (user == null)
            {
                return NotFound();
            }

            user.Password = model.NewPassword;
            _context.PasswordResetTokens.Remove(passwordResetToken);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        private Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Cần cấu hình tài khoản email để gửi
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("ChillGameStoreBITV@gmail.com", "nyvp fwif palu giwa"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("ChillGameStoreBITV@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            return smtpClient.SendMailAsync(mailMessage);
        }
    }
}