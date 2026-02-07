using ip_connect.Data;
using ip_connect.Dtos.Photo;
using ip_connect.Exceptions;
using ip_connect.Models;
using ip_connect.Services.Files;
using ip_connect.Services.FriendshipService;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Services.Photos
{
    public class PhotoService : IPhotoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IFriendshipService _friendshipService;

        public PhotoService(ApplicationDbContext context, IFileService fileService, IFriendshipService friendshipService)
        {
            _context = context;
            _fileService = fileService;
            _friendshipService = friendshipService;
        }

        public async Task<List<PhotoDto>> GetPhotosByAlbumIdAsync(int albumId, string currentUserId)
        {
            var album = await _context.Albums.FindAsync(albumId);
            if (album == null) throw new NotFoundException("Album not found");

            // Проверка за права (ако не е твой албума, трябва да сте приятели)
            if (album.UserId != currentUserId)
            {
                var areFriends = await _friendshipService.AreFriendsAsync(album.UserId, currentUserId);
                if (!areFriends) throw new ForbiddenException("You are not friends with this user.");
            }

            var photos = await _context.Photos
                .Where(p => p.AlbumId == albumId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PhotoDto
                {
                    Id = p.Id,
                    Url = p.Url,
                    ThumbnailUrl = p.ThumbnailUrl ?? p.Url, // Fallback
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return photos;
        }

        public async Task<PhotoDto> UploadPhotoAsync(int albumId, IFormFile file, string currentUserId)
        {
            var album = await _context.Albums.FindAsync(albumId);
            if (album == null) throw new NotFoundException("Album not found");

            // САМО СОБСТВЕНИКА може да качва
            if (album.UserId != currentUserId)
            {
                throw new ForbiddenException("You can only upload photos to your own albums.");
            }

            // 1. Качване на файла физически
            var fileUrl = await _fileService.SaveFileAsync(file, "photos");

            // 2. Запис в базата
            var photo = new Photo
            {
                AlbumId = albumId,
                Url = fileUrl,
                ThumbnailUrl = fileUrl, // За сега е същото
                CreatedAt = DateTime.UtcNow
            };

            _context.Photos.Add(photo);

            // Увеличаване на броя снимки в албума (ако пазиш броя в Album entity-то, ако не - пропусни)
            // album.PhotoCount++; 

            await _context.SaveChangesAsync();

            return new PhotoDto
            {
                Id = photo.Id,
                Url = photo.Url,
                ThumbnailUrl = photo.ThumbnailUrl,
                CreatedAt = photo.CreatedAt
            };
        }

        public async Task DeletePhotoAsync(int photoId, string currentUserId)
        {
            var photo = await _context.Photos
                .Include(p => p.Album)
                .FirstOrDefaultAsync(p => p.Id == photoId);

            if (photo == null) throw new NotFoundException("Photo not found");

            // САМО СОБСТВЕНИКА може да трие
            if (photo.Album.UserId != currentUserId)
            {
                throw new ForbiddenException("You do not own this photo.");
            }

            // 1. Изтриване на файла физически
            _fileService.DeleteFile(photo.Url);

            // 2. Изтриване от базата
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
        }
    }
}
