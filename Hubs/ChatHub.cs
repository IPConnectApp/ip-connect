using ip_connect.Services.Messages;
using Microsoft.AspNetCore.SignalR;

namespace ip_connect.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;

        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task SendMessage(string senderName, string messageText)
        {
            // Save message using service
            var message = await _messageService.SendMessageAsync(senderName, messageText);

            // Broadcast to all connected clients
            await Clients.All.SendAsync("ReceiveMessage", message.SenderName, message.Text, message.Timestamp);
        }
    }
}
