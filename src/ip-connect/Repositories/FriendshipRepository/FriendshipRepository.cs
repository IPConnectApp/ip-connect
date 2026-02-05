using ip_connect.Data;
using ip_connect.Models;
using ip_connect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.FriendshipRepository
{
    public class FriendshipRepository : Repository<Friendship>, IFriendshipRepository
    {
        public FriendshipRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Friendship?> GetFriendshipAsync(string userId, string friendId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(f => 
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId));
        }

        public async Task<List<Friendship>> GetFriendsAsync(string userId)
        {
            return await _dbSet
                .Include(f => f.User)
                .Include(f => f.Friend)
                .Where(f => 
                    (f.UserId == userId || f.FriendId == userId) && 
                    f.Status == FriendshipStatus.Accepted)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetPendingRequestsAsync(string userId)
        {
            return await _dbSet
                .Include(f => f.User)
                .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetSentRequestsAsync(string userId)
        {
            return await _dbSet
                .Include(f => f.Friend)
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<bool> AreFriendsAsync(string userId, string friendId)
        {
            return await _dbSet
                .AnyAsync(f => 
                    ((f.UserId == userId && f.FriendId == friendId) ||
                     (f.UserId == friendId && f.FriendId == userId)) &&
                    f.Status == FriendshipStatus.Accepted);
        }

        public async Task<Friendship?> GetFriendshipWithUsersAsync(int friendshipId)
        {
            return await _dbSet
                .Include(f => f.User)
                .Include(f => f.Friend)
                .FirstOrDefaultAsync(f => f.Id == friendshipId);
        }

        public async Task<bool> UpdateStatusAsync(int friendshipId, FriendshipStatus status)
        {
            var friendship = await GetByIdAsync(friendshipId);
            if (friendship == null)
                return false;

            friendship.Status = status;
            
            if (status == FriendshipStatus.Accepted)
                friendship.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}