using System.ComponentModel.DataAnnotations;
using ip_connect.Models.Enums;

namespace ip_connect.Models
{
    public class Friendship
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string FriendId { get; set; } = string.Empty;

        [Required]
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AcceptedAt { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser Friend { get; set; } = null!;
    }
}