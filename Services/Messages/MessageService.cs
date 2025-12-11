using ip_connect.Models;
using ip_connect.Repositories.Messages;

namespace ip_connect.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<Message> SendMessageAsync(string senderName, string text)
        {
            var message = new Message
            {
                SenderName = senderName,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            return await _messageRepository.AddAsync(message);
        }

        public async Task<List<Message>> GetAllMessagesAsync()
        {
            return await _messageRepository.GetAllAsync();
        }

        public async Task<List<Message>> GetRecentMessagesAsync(int count = 50)
        {
            return await _messageRepository.GetRecentAsync(count);
        }
    }
}
