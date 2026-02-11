using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ip_connect.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; }
        public int AlbumId { get; set; }
        public string UserId { get; set; }
        public int DisplayOrder { get; set; } // ADD THIS IF MISSING
        public DateTime UploadedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(AlbumId))] // Казваме на EF, че AlbumId е ключът за Album
        public Album Album { get; set; } = null!;
        public ApplicationUser User { get; set; }
    }
}