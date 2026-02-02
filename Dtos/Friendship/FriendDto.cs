namespace ip_connect.DTOs.Friendship
{
    public class FriendDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime FriendsSince { get; set; }
        public int FriendshipId { get; set; }
    }
}
