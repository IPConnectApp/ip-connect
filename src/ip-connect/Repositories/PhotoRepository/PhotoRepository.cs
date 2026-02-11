using ip_connect.Data;
using ip_connect.Models;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.PhotoRepository
{
    public class PhotoRepository : IPhotoRepository
    {
        private readonly ApplicationDbContext _context;

        public PhotoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Photo>> GetPhotosByAlbumIdAsync(int albumId)
        {
            return await _context.Photos
                .Where(p => p.AlbumId == albumId)
                .OrderBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<Photo?> GetByIdAsync(int photoId)
        {
            return await _context.Photos
                .Include(p => p.Album)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == photoId);
        }

        public async Task<Photo> CreateAsync(Photo photo)
        {
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();
            return photo;
        }

        public async Task<Photo> UpdateAsync(Photo photo)
        {
            _context.Photos.Update(photo);
            await _context.SaveChangesAsync();
            return photo;
        }

        public async Task DeleteAsync(int photoId)
        {
            var photo = await GetByIdAsync(photoId);
            if (photo != null)
            {
                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetNextDisplayOrderAsync(int albumId)
        {
            var maxOrder = await _context.Photos
                .Where(p => p.AlbumId == albumId)
                .MaxAsync(p => (int?)p.DisplayOrder);

            return (maxOrder ?? 0) + 1;
        }

        public async Task UpdateDisplayOrdersAsync(List<Photo> photos)
        {
            _context.Photos.UpdateRange(photos);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int photoId)
        {
            return await _context.Photos.AnyAsync(p => p.Id == photoId);
        }
    }
}
