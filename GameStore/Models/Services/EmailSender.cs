using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GameStore.Models.Services // Hoặc namespace GameStore.Services tùy file bro
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                // Dùng luôn thông tin xịn của bro ở đây
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