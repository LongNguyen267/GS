using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
namespace GameStore.Models.Services
{
    
        public class EmailSender : IEmailSender
        {
            public Task SendEmailAsync(string toEmail, string subject, string body)
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("your-email@gmail.com", "your-password"), // THAY ĐỔI: Thay bằng email và mật khẩu của bạn
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@gmail.com"), // THAY ĐỔI: Thay bằng email của bạn
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                return smtpClient.SendMailAsync(mailMessage);
            }
        }
    
}
