using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ip_connect.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public UserProfile? Profile { get; set; }
    }
}