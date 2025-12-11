using ip_connect.Models;

namespace ip_connect.Services.Messages
{
    public interface IMessageService
    {
        Task<Message> SendMessageAsync(string senderName, string text);

        Task<List<Message>> GetAllMessagesAsync();

        Task<List<Message>> GetRecentMessagesAsync(int count = 50);
    }
}
