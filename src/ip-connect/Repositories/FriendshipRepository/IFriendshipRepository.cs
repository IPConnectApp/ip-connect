using ip_connect.Models;
using ip_connect.Models.Enums;

namespace ip_connect.Repositories.FriendshipRepository
{
    public interface IFriendshipRepository : IRepository<Friendship>
    {
        // Check if friendship exists between two users (any status)
        Task<Friendship?> GetFriendshipAsync(string userId, string friendId);
        
        // Get all accepted friends for a user
        Task<List<Friendship>> GetFriendsAsync(string userId);
        
        // Get pending friend requests received by user
        Task<List<Friendship>> GetPendingRequestsAsync(string userId);
        
        // Get pending friend requests sent by user
        Task<List<Friendship>> GetSentRequestsAsync(string userId);
        
        // Check if two users are friends (Accepted status)
        Task<bool> AreFriendsAsync(string userId, string friendId);
        
        // Get friendship by ID with related user data
        Task<Friendship?> GetFriendshipWithUsersAsync(int friendshipId);
        
        // Update friendship status
        Task<bool> UpdateStatusAsync(int friendshipId, FriendshipStatus status);
    }
}