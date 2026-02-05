namespace ip_connect.DTOs.Friendship
{
    public class PendingRequestDto
    {
        public int FriendshipId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
