using ip_connect.DTOs.Friendship;

namespace ip_connect.Services.FriendshipService
{
    public interface IFriendshipService
    {
        // Send friend request
        Task<bool> SendFriendRequestAsync(string userId, string friendUserId);
        
        // Accept friend request
        Task<bool> AcceptFriendRequestAsync(string userId, int friendshipId);
        
        // Reject friend request
        Task<bool> RejectFriendRequestAsync(string userId, int friendshipId);
        
        // Remove friend (unfriend)
        Task<bool> RemoveFriendAsync(string userId, int friendshipId);
        
        // Get user's friends list
        Task<List<FriendDto>> GetFriendsAsync(string userId);
        
        // Get pending friend requests (received)
        Task<List<PendingRequestDto>> GetPendingRequestsAsync(string userId);
        
        // Get friendship status between two users
        Task<FriendshipStatusDto> GetFriendshipStatusAsync(string userId, string otherUserId);
        
        // Check if two users are friends
        Task<bool> AreFriendsAsync(string userId, string friendUserId);
    }
}