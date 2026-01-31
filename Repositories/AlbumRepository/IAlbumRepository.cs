using ip_connect.Models;

namespace ip_connect.Repositories.AlbumRepository
{
    public interface IAlbumRepository : IRepository<Album>
    {
        // Get all albums for a user (ordered by newest first)
        Task<List<Album>> GetUserAlbumsAsync(string userId);

        // Check if user is the owner of the album
        Task<bool> IsAlbumOwnerAsync(int albumId, string userId);
    }
}