using ip_connect.Hubs;
using ip_connect.Models;
using ip_connect.Repositories.ConversationMemberRepository;
using ip_connect.Repositories.MessageRepository;
using Microsoft.AspNetCore.SignalR;

namespace ip_connect.Services.MessageService
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationMemberRepository _conversationMemberRepository;
        private readonly IHubContext<ChatHub> _hubContext;


        public MessageService(
            IMessageRepository messageRepository,
            IConversationMemberRepository conversationMemberRepository,
            IHubContext<ChatHub> hubContext)
        {
            _messageRepository = messageRepository;
            _conversationMemberRepository = conversationMemberRepository;
            _hubContext = hubContext;
        }

        public async Task<Message> SendMessageAsync(int conversationId, string senderId, string text)
        {
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            message = await _messageRepository.CreateAsync(message);

            //Broadcast message via SignalR to all users in this conversation
            await _hubContext.Clients
                .Group($"conversation-{conversationId}")
                .SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    conversationId = message.ConversationId,
                    senderId = message.SenderId,
                    text = message.Text,
                    timestamp = message.Timestamp
                });

            return message;
        }

        public async Task<List<Message>> GetConversationMessagesAsync(int conversationId)
        {
            return await _messageRepository.GetConversationMessagesAsync(conversationId);
        }

        public async Task MarkConversationAsReadAsync(int conversationId, string userId)
        {
            await _conversationMemberRepository.UpdateLastReadAtAsync(conversationId, userId);
        }
    }
}
