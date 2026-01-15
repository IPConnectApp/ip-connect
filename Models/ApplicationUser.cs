using Microsoft.AspNetCore.Identity;

namespace ip_connect.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}