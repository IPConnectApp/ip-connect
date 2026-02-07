using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ip_connect.Models
{
    public class Photo
    {
        public int Id { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;

        // Опционално, ако в бъдеще правиш ресайз на снимки
        public string? ThumbnailUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key към Album
        public int AlbumId { get; set; }

        [ForeignKey(nameof(AlbumId))]
        public Album? Album { get; set; }
    }
}