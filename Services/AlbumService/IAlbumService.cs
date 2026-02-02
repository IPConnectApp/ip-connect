using ip_connect.DTOs;

namespace ip_connect.Services.Albums
{
    public interface IAlbumService
    {
        // Get all albums for a user
        Task<List<AlbumDto>> GetUserAlbumsAsync(string userId);

        // Get a single album by ID
        Task<AlbumDto?> GetAlbumByIdAsync(int albumId);

        // Create a new album
        Task<AlbumDto> CreateAlbumAsync(string userId, CreateAlbumDto createDto);

        // Update an existing album
        Task<AlbumDto> UpdateAlbumAsync(int albumId, string userId, UpdateAlbumDto updateDto);

        // Delete an album
        Task DeleteAlbumAsync(int albumId, string userId);

        Task<AlbumDto> GetAlbumByIdAsync(int albumId, string currentUserId);
    }
}