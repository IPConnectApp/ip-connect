using ip_connect.Dtos.Photo;
using ip_connect.Models;

namespace ip_connect.Services.Photos
{
    public interface IPhotoService
    {
        Task<List<PhotoDto>> GetPhotosByAlbumIdAsync(int albumId, string currentUserId);
        Task<PhotoDto> UploadPhotoAsync(int albumId, IFormFile file, string currentUserId);
        Task DeletePhotoAsync(int photoId, string currentUserId);
        Task ReorderPhotosAsync(List<PhotoReorderDto> reorderList, string userId);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<Photo> GetPhotoByIdAsync(int id, string userId);
    }
}
