using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace qlthucung.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            // gửi cho người nhận
            await Clients.Group(receiverId).SendAsync("ReceiveMessage", senderId, message);

            // gửi ngược cho chính sender (để đồng bộ)
            await Clients.Group(senderId).SendAsync("MessageSent", senderId, receiverId, message);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);

                // thông báo cho admin là có user mới
                await Clients.Group("ADMIN").SendAsync("NewUserConnected", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var userId = Context.GetHttpContext().Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
