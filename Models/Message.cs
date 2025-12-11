namespace ip_connect.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
