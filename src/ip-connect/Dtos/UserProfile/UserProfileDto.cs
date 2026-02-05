using System.ComponentModel.DataAnnotations;

namespace ip_connect.Dtos.UserProfile
{
    public class UserProfileDto
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string DisplayName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public bool IsOnline { get; set; }

        public DateTime? LastSeen { get; set; }

        public DateTime CreatedAt { get; set; }

        // Може да добавим и URL на снимката тук, за да е удобно за View-то
        public string? ProfilePictureUrl { get; set; }
    }
}
