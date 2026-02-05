using ip_connect.Models;

namespace ip_connect.Repositories.UserProfileRepository
{
    public interface IUserProfileRepository : IRepository<UserProfile>
    {
        Task<UserProfile?> GetByUserIdAsync(string userId);
    }
}
