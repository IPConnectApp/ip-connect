using ip_connect.Models;

namespace ip_connect.Repositories.PhotoRepository
{
    public interface IPhotoRepository
    {
        Task<List<Photo>> GetPhotosByAlbumIdAsync(int albumId);
        Task<Photo?> GetByIdAsync(int photoId);
        Task<Photo> CreateAsync(Photo photo);
        Task<Photo> UpdateAsync(Photo photo);
        Task DeleteAsync(int photoId);
        Task<int> GetNextDisplayOrderAsync(int albumId);
        Task UpdateDisplayOrdersAsync(List<Photo> photos);
        Task<bool> ExistsAsync(int photoId);
    }
}
