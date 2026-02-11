using System.ComponentModel.DataAnnotations;

namespace ip_connect.Dtos.Photo
{
    public class UploadPhotoDto
    {
        [Required(ErrorMessage = "Album ID is required")]
        public int AlbumId { get; set; }

        [Required(ErrorMessage = "At least one file is required")]
        public List<IFormFile> Files { get; set; } = new();
    }
}
