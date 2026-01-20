using ip_connect.DTOs.Chat;
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

        public async Task<MessageDto> SendMessageAsync(int conversationId, string senderId, string senderUsername, string text)
        {
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            message = await _messageRepository.CreateAsync(message);

            // Create DTO for response and SignalR
            var messageDto = new MessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                SenderUsername = senderUsername,
                Text = message.Text,
                Timestamp = message.Timestamp
            };

            //Broadcast message via SignalR to all users in this conversation
            await _hubContext.Clients
                .Group($"conversation-{conversationId}")
                .SendAsync("ReceiveMessage", messageDto);

            // Ensures the conversation appears in their list immediately
            await _hubContext.Clients
                .Group($"user-{conversationId}")
                .SendAsync("NewConversation", new { conversationId });

            return messageDto;
        }

        public async Task<List<MessageDto>> GetConversationMessagesAsync(int conversationId)
        {
            var messages = await _messageRepository.GetConversationMessagesAsync(conversationId);

            // Map to DTOs
            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderUsername = m.Sender.UserName!,
                Text = m.Text,
                Timestamp = m.Timestamp
            }).ToList();
        }

        public async Task MarkConversationAsReadAsync(int conversationId, string userId)
        {
            await _conversationMemberRepository.UpdateLastReadAtAsync(conversationId, userId);
        }
    }
}
