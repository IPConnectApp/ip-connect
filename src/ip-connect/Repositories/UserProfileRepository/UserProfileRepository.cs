using ip_connect.Data;
using ip_connect.Models;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.UserProfileRepository
{
    public class UserProfileRepository : Repository<UserProfile>, IUserProfileRepository
    {
        public UserProfileRepository(ApplicationDbContext context) : base(context)
        {

        }

        public async Task<UserProfile?> GetByUserIdAsync(string userId)
        {

            return await _context.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
