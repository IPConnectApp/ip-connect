using ip_connect.Dtos.Photo;
using ip_connect.Exceptions;
using ip_connect.Models;
using ip_connect.Repositories.AlbumRepository;
using ip_connect.Repositories.PhotoRepository;
using ip_connect.Services.Files;
using ip_connect.Services.FriendshipService;

namespace ip_connect.Services.Photos
{
    public class PhotoService : IPhotoService
    {
        private readonly IPhotoRepository _photoRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IFileService _fileService;
        private readonly IFriendshipService _friendshipService;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(
            IPhotoRepository photoRepository,
            IAlbumRepository albumRepository,
            IFileService fileService,
            IFriendshipService friendshipService,
            ILogger<PhotoService> logger)
        {
            _photoRepository = photoRepository;
            _albumRepository = albumRepository;
            _fileService = fileService;
            _friendshipService = friendshipService;
            _logger = logger;
        }

        public async Task<List<PhotoDto>> GetPhotosByAlbumIdAsync(int albumId, string currentUserId)
        {
            // 1. Verify album exists
            var album = await _albumRepository.GetByIdAsync(albumId);
            if (album == null)
            {
                throw new NotFoundException("Album not found");
            }

            // 2. Check access permissions
            if (album.UserId != currentUserId)
            {
                var areFriends = await _friendshipService.AreFriendsAsync(album.UserId, currentUserId);
                if (!areFriends)
                {
                    throw new ForbiddenException("You don't have access to this album");
                }
            }

            // 3. Get photos
            var photos = await _photoRepository.GetPhotosByAlbumIdAsync(albumId);

            // 4. Map to DTOs
            return photos.Select(p => new PhotoDto
            {
                Id = p.Id,
                Url = p.PhotoUrl,
                ThumbnailUrl = p.PhotoUrl, // For now, same as original
                CreatedAt = p.UploadedAt
            }).ToList();
        }

        public async Task<PhotoDto> UploadPhotoAsync(int albumId, IFormFile file, string currentUserId)
        {
            // 1. Validate file
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("Invalid file");
            }

            // 2. Verify album exists and user owns it
            var album = await _albumRepository.GetByIdAsync(albumId);
            if (album == null)
            {
                throw new NotFoundException("Album not found");
            }

            if (album.UserId != currentUserId)
            {
                throw new ForbiddenException("You can only upload photos to your own albums");
            }

            // 3. Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new BadRequestException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            // 4. Validate file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                throw new BadRequestException("File size cannot exceed 10MB");
            }

            try
            {
                // 5. Save file
                var fileUrl = await _fileService.SaveFileAsync(file, "photos");

                // 6. Get next display order
                var displayOrder = await _photoRepository.GetNextDisplayOrderAsync(albumId);

                // 7. Create photo entity
                var photo = new Photo
                {
                    AlbumId = albumId,
                    UserId = currentUserId,
                    PhotoUrl = fileUrl,
                    DisplayOrder = displayOrder,
                    UploadedAt = DateTime.UtcNow
                };

                // 8. Save to database
                var createdPhoto = await _photoRepository.CreateAsync(photo);

                _logger.LogInformation($"Photo uploaded successfully: {createdPhoto.Id} by user {currentUserId}");

                // 9. Return DTO
                return new PhotoDto
                {
                    Id = createdPhoto.Id,
                    Url = createdPhoto.PhotoUrl,
                    ThumbnailUrl = createdPhoto.PhotoUrl,
                    CreatedAt = createdPhoto.UploadedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload photo to album {albumId}");
                throw new BadRequestException($"Failed to upload photo: {ex.Message}");
            }
        }

        public async Task DeletePhotoAsync(int photoId, string currentUserId)
        {
            // 1. Get photo with album
            var photo = await _photoRepository.GetByIdAsync(photoId);
            if (photo == null)
            {
                throw new NotFoundException("Photo not found");
            }

            // 2. Verify ownership
            if (photo.Album.UserId != currentUserId)
            {
                throw new ForbiddenException("You don't have permission to delete this photo");
            }

            try
            {
                // 3. Delete physical file
                _fileService.DeleteFile(photo.PhotoUrl);

                // 4. Delete from database
                await _photoRepository.DeleteAsync(photoId);

                _logger.LogInformation($"Photo deleted successfully: {photoId} by user {currentUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete photo {photoId}");
                throw new BadRequestException($"Failed to delete photo: {ex.Message}");
            }
        }

        public async Task ReorderPhotosAsync(List<PhotoReorderDto> reorderList, string userId)
        {
            if (reorderList == null || !reorderList.Any())
            {
                throw new BadRequestException("Reorder list cannot be empty");
            }

            var photos = new List<Photo>();

            // 1. Validate all photos and permissions
            foreach (var item in reorderList)
            {
                var photo = await _photoRepository.GetByIdAsync(item.Id);
                if (photo == null)
                {
                    throw new NotFoundException($"Photo with ID {item.Id} not found");
                }

                if (photo.Album.UserId != userId)
                {
                    throw new ForbiddenException("You don't have permission to reorder these photos");
                }

                photo.DisplayOrder = item.DisplayOrder;
                photos.Add(photo);
            }

            // 2. Update all at once
            await _photoRepository.UpdateDisplayOrdersAsync(photos);

            _logger.LogInformation($"Photos reordered successfully by user {userId}");
        }

        public async Task<Photo> GetPhotoByIdAsync(int id, string userId)
        {
            var photo = await _photoRepository.GetByIdAsync(id);
            if (photo == null)
            {
                throw new NotFoundException("Photo not found");
            }

            // Check access permissions
            if (photo.Album.UserId != userId)
            {
                var areFriends = await _friendshipService.AreFriendsAsync(photo.Album.UserId, userId);
                if (!areFriends)
                {
                    throw new ForbiddenException("You don't have access to this photo");
                }
            }

            return photo;
        }
    }
}
