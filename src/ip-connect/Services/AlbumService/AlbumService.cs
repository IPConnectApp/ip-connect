using ip_connect.DTOs;
using ip_connect.Exceptions;
using ip_connect.Models;
using ip_connect.Repositories.AlbumRepository;
using ip_connect.Repositories.FriendshipRepository;

namespace ip_connect.Services.Albums
{
    public class AlbumService : IAlbumService
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IFriendshipRepository _friendshipRepository;

        public AlbumService(IAlbumRepository albumRepository, IFriendshipRepository friendshipRepository)
        {
            _albumRepository = albumRepository;
            _friendshipRepository = friendshipRepository;
        }

        // Get all albums for a user
        public async Task<List<AlbumDto>> GetUserAlbumsAsync(string userId)
        {
            var albums = await _albumRepository.GetUserAlbumsAsync(userId);

            return albums.Select(a => new AlbumDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                Description = a.Description,
                CreatedAt = a.CreatedAt,

                PhotoCount = a.Photos?.Count ?? 0,
                CoverPhotoUrl = a.Photos?
                    .OrderBy(p => p.UploadedAt)
                    .Select(p => p.PhotoUrl)
                    .FirstOrDefault()
            }).ToList();
        }

        // Get a single album by ID
        public async Task<AlbumDto> GetAlbumByIdAsync(int albumId, string currentUserId)
        {
            var album = await _albumRepository.GetAlbumByIdAsync(albumId);

            if (album == null)
                throw new NotFoundException("Album not found");

            // Access control...
            if (album.UserId != currentUserId)
            {
                var areFriends = await _friendshipRepository.AreFriendsAsync(currentUserId, album.UserId);
                if (!areFriends)
                    throw new ForbiddenException("You don't have access to this album");
            }

            return new AlbumDto
            {
                Id = album.Id,
                UserId = album.UserId,
                Name = album.Name,
                Description = album.Description,
                CreatedAt = album.CreatedAt,

                PhotoCount = album.Photos?.Count ?? 0,
                CoverPhotoUrl = album.Photos?
                    .OrderBy(p => p.UploadedAt)
                    .Select(p => p.PhotoUrl)
                    .FirstOrDefault()
            };
        }

        // Create a new album
        public async Task<AlbumDto> CreateAlbumAsync(string userId, CreateAlbumDto createDto)
        {
            var existingAlbums = await _albumRepository.GetUserAlbumsAsync(userId);
            if (existingAlbums.Any(a => a.Name == createDto.Name))
            {
                throw new BadRequestException("You already have an album with this name");
            }

            var album = new Album
            {
                UserId = userId,
                Name = createDto.Name,
                Description = createDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            var createdAlbum = await _albumRepository.CreateAsync(album);

            return new AlbumDto
            {
                Id = createdAlbum.Id,
                UserId = createdAlbum.UserId,
                Name = createdAlbum.Name,
                Description = createdAlbum.Description,
                CreatedAt = createdAlbum.CreatedAt,
                PhotoCount = 0,
                CoverPhotoUrl = null
            };
        }

        // Update an existing album
        public async Task<AlbumDto> UpdateAlbumAsync(int albumId, string userId, UpdateAlbumDto updateDto)
        {
            var album = await _albumRepository.GetByIdAsync(albumId);
            if (album == null)
            {
                throw new NotFoundException("Album not found");
            }

            if (album.UserId != userId)
            {
                throw new ForbiddenException("You don't have permission to edit this album");
            }

            album.Name = updateDto.Name;
            album.Description = updateDto.Description;

            var updatedAlbum = await _albumRepository.UpdateAsync(album);

            return new AlbumDto
            {
                Id = updatedAlbum.Id,
                UserId = updatedAlbum.UserId,
                Name = updatedAlbum.Name,
                Description = updatedAlbum.Description,
                CreatedAt = updatedAlbum.CreatedAt,
                PhotoCount = updatedAlbum.Photos?.Count ?? 0,
                CoverPhotoUrl = updatedAlbum.Photos?
                    .OrderBy(p => p.UploadedAt)
                    .Select(p => p.PhotoUrl)
                    .FirstOrDefault()
            };
        }

        // Delete an album
        public async Task DeleteAlbumAsync(int albumId, string userId)
        {
            var album = await _albumRepository.GetByIdAsync(albumId);
            if (album == null)
            {
                throw new NotFoundException("Album not found");
            }

            if (album.UserId != userId)
            {
                throw new ForbiddenException("You don't have permission to delete this album");
            }

            await _albumRepository.DeleteAsync(albumId);
        }

    }
}
