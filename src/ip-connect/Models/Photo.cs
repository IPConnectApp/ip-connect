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
        public Album Album { get; set; }
        public ApplicationUser User { get; set; }
    }
}