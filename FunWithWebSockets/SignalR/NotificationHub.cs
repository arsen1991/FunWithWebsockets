using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;

namespace FunWithWebSockets.SignalR
{
    [SignalRHub]
    public class NotificationHub : Hub
    {
        public Task SendMessage(string message)
        {
            return Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
