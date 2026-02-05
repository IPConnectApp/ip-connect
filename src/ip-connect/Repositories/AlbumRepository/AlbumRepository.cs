using ip_connect.Data;
using ip_connect.Models;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.AlbumRepository
{
    public class AlbumRepository : Repository<Album>, IAlbumRepository
    {
        public AlbumRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<Album>> GetUserAlbumsAsync(string userId)
        {
            return await _dbSet
                .Include(a => a.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsAlbumOwnerAsync(int albumId, string userId)
        {
            return await _dbSet
                .AnyAsync(a => a.Id == albumId && a.UserId == userId);
        }

        public async Task<Album?> GetAlbumByIdAsync(int albumId)
        {
            return await _context.Albums
                .FirstOrDefaultAsync(a => a.Id == albumId);
        }
    }
}