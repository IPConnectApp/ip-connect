namespace ip_connect.DTOs.Friendship
{
    public class FriendshipStatusDto
    {
        public bool AreFriends { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool IsSentByMe { get; set; }
        public int? FriendshipId { get; set; }
    }
}
