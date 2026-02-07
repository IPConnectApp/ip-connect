using ip_connect.Dtos.Photo;

namespace ip_connect.Services.Photos
{
    public interface IPhotoService
    {
        Task<List<PhotoDto>> GetPhotosByAlbumIdAsync(int albumId, string currentUserId);
        Task<PhotoDto> UploadPhotoAsync(int albumId, IFormFile file, string currentUserId);
        Task DeletePhotoAsync(int photoId, string currentUserId);
    }
}
