using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ip_connect.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Join user's personal group when they connect
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        }

        public async Task UserTyping(int conversationId, string username)
        {
            await Clients.OthersInGroup($"conversation-{conversationId}")
                .SendAsync("UserTyping", new { conversationId, username });
        }

        public async Task UserStoppedTyping(int conversationId, string username)
        {
            await Clients.OthersInGroup($"conversation-{conversationId}")
                .SendAsync("UserStoppedTyping", new { conversationId, username });
        }
    }
}