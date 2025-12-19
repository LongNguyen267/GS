using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GameStore.Chat
{
    public class ChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (Context.User.Identity.IsAuthenticated)
            {
                // KIỂM TRA ROLE (VAI TRÒ)
                // Nếu là Admin HOẶC Employee -> Cho vào nhóm "SupportTeam"
                if (Context.User.IsInRole("Admin") || Context.User.IsInRole("Employee"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "SupportTeam");
                }
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string user, string message)
        {
            // Kiểm tra lại lần nữa khi gửi tin
            bool isStaff = Context.User.IsInRole("Admin") || Context.User.IsInRole("Employee");

            if (isStaff)
            {
                // TRƯỜNG HỢP 1: ADMIN/EMPLOYEE TRẢ LỜI
                // Tạm thời gửi cho tất cả để khách thấy phản hồi
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
            else
            {
                // TRƯỜNG HỢP 2: KHÁCH HÀNG NHẮN
                // 1. Gửi lại cho chính khách hàng (để họ thấy tin mình vừa nhắn)
                await Clients.Caller.SendAsync("ReceiveMessage", user, message);

                // 2. Chỉ gửi cho nhóm "SupportTeam" (Gồm Admin và Employee)
                // --> Khách hàng khác bên ngoài sẽ KHÔNG THẤY GÌ CẢ.
                await Clients.Group("SupportTeam").SendAsync("ReceiveMessage", user, message);
            }
        }
    }
}