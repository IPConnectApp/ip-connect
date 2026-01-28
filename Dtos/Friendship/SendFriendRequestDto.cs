using System.ComponentModel.DataAnnotations;

namespace ip_connect.DTOs.Friendship
{
    public class SendFriendRequestDto
    {
        [Required(ErrorMessage = "Friend user ID is required")]
        public string FriendUserId { get; set; } = string.Empty;
    }
}