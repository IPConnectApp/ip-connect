using System.ComponentModel.DataAnnotations;

namespace ip_connect.DTOs
{
    public class UpdateAlbumDto
    {
        [Required(ErrorMessage = "Album name is required")]
        [StringLength(100, ErrorMessage = "Album name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}