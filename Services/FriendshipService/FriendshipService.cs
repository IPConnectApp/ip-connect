using ip_connect.DTOs.Friendship;
using ip_connect.DTOs.Notification;
using ip_connect.Exceptions;
using ip_connect.Models;
using ip_connect.Models.Enums;
using ip_connect.Repositories.FriendshipRepository;
using ip_connect.Services.NotificationService;
using Microsoft.AspNetCore.Identity;

namespace ip_connect.Services.FriendshipService
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _friendshipRepository = friendshipRepository;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<bool> SendFriendRequestAsync(string userId, string friendUserId)
        {
            // Validate users exist
            var user = await _userManager.FindByIdAsync(userId);
            var friendUser = await _userManager.FindByIdAsync(friendUserId);

            if (user == null || friendUser == null)
                throw new NotFoundException("User not found");

            // Can't send request to yourself
            if (userId == friendUserId)
                throw new BadRequestException("Cannot send friend request to yourself");

            // Check if friendship already exists
            var existingFriendship = await _friendshipRepository.GetFriendshipAsync(userId, friendUserId);
            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendshipStatus.Pending)
                    throw new BadRequestException("Friend request already sent");
                if (existingFriendship.Status == FriendshipStatus.Accepted)
                    throw new BadRequestException("Already friends");
            }

            // Create friendship
            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendUserId,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            var created = await _friendshipRepository.CreateAsync(friendship);

            // Create notification for the friend
            await _notificationService.CreateFriendRequestNotificationAsync(
                friendUserId,
                userId,
                created.Id);

            // Create notification DTO for real-time
            var notificationDto = new NotificationDto
            {
                Id = 0,
                Type = NotificationType.FriendRequest,
                Message = $"{user.UserName} sent you a friend request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedUserId = userId,
                RelatedUsername = user.UserName,
                RelatedUserProfilePicture = user.ProfilePictureUrl,
                RelatedEntityId = created.Id
            };

            // Send real-time notification to the friend
            await _notificationService.SendNotificationToUserAsync(friendUserId, notificationDto);

            // Update notification count for friend
            var unreadCount = await _notificationService.GetUnreadCountAsync(friendUserId);
            await _notificationService.SendNotificationCountToUserAsync(friendUserId, unreadCount);

            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(string userId, int friendshipId)
        {
            var friendship = await _friendshipRepository.GetFriendshipWithUsersAsync(friendshipId);

            if (friendship == null)
                throw new NotFoundException("Friend request not found");

            // Only the recipient can accept
            if (friendship.FriendId != userId)
                throw new ForbiddenException("You cannot accept this friend request");

            if (friendship.Status != FriendshipStatus.Pending)
                throw new BadRequestException("Friend request is not pending");

            // Update status
            var updated = await _friendshipRepository.UpdateStatusAsync(friendshipId, FriendshipStatus.Accepted);

            if (!updated)
                throw new BadRequestException("Failed to accept friend request");

            // Create notification for the sender
            await _notificationService.CreateFriendAcceptedNotificationAsync(
                friendship.UserId,
                userId,
                friendshipId);

            // Get user who accepted (for notification data)
            var acceptedBy = await _userManager.FindByIdAsync(userId);

            var notificationDto = new NotificationDto
            {
                Id = 0,
                Type = Models.Enums.NotificationType.FriendAccepted,
                Message = $"{acceptedBy.UserName} accepted your friend request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedUserId = userId,
                RelatedUsername = acceptedBy.UserName,
                RelatedUserProfilePicture = acceptedBy.ProfilePictureUrl,
                RelatedEntityId = friendshipId
            };

            // Send real-time notification to the sender
            await _notificationService.SendNotificationToUserAsync(friendship.UserId, notificationDto);

            // Update notification count for sender
            var unreadCount = await _notificationService.GetUnreadCountAsync(friendship.UserId);
            await _notificationService.SendNotificationCountToUserAsync(friendship.UserId, unreadCount);

            // Mark the friend request notification as read for the current user
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 100);
            var friendRequestNotification = notifications.FirstOrDefault(n =>
                n.Type == NotificationType.FriendRequest &&
                n.RelatedEntityId == friendshipId);

            if (friendRequestNotification != null)
            {
                await _notificationService.MarkAsReadAsync(userId, friendRequestNotification.Id);

                // Update badge count for current user in real-time
                var currentUserUnreadCount = await _notificationService.GetUnreadCountAsync(userId);
                await _notificationService.SendNotificationCountToUserAsync(userId, currentUserUnreadCount);
            }

            return true;
        }

        public async Task<bool> RejectFriendRequestAsync(string userId, int friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);

            if (friendship == null)
                throw new NotFoundException("Friend request not found");

            // Both the sender and recipient can reject/cancel
            if (friendship.UserId != userId && friendship.FriendId != userId)
                throw new ForbiddenException("You cannot reject this friend request");

            if (friendship.Status != FriendshipStatus.Pending)
                throw new BadRequestException("Friend request is not pending");

            // Delete related notifications BEFORE deleting friendship
            await _notificationService.DeleteNotificationsByEntityAsync(
                NotificationType.FriendRequest,
                friendshipId);

            // Delete the friendship entirely (cleaner than marking as rejected)
            var deleted = await _friendshipRepository.DeleteAsync(friendshipId);

            if (!deleted)
                throw new BadRequestException("Failed to reject friend request");

            return true;
        }

        public async Task<bool> RemoveFriendAsync(string userId, int friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);

            if (friendship == null)
                throw new NotFoundException("Friendship not found");

            // Only participants can remove friendship
            if (friendship.UserId != userId && friendship.FriendId != userId)
                throw new ForbiddenException("You cannot remove this friendship");

            // Determine who is the other person
            var otherUserId = friendship.UserId == userId ? friendship.FriendId : friendship.UserId;

            // Delete friendship
            var deleted = await _friendshipRepository.DeleteAsync(friendshipId);
            if (!deleted)
                throw new BadRequestException("Failed to remove friend");

            // Create notification for the other user
            await _notificationService.CreateFriendRemovedNotificationAsync(
                otherUserId,
                userId,
                friendshipId);

            // Send real-time notification
            var removedBy = await _userManager.FindByIdAsync(userId);
            if (removedBy == null)
                throw new NotFoundException("User not found");

            var notificationDto = new NotificationDto
            {
                Id = 0,
                Type = NotificationType.FriendRemoved,
                Message = $"{removedBy.UserName} removed you from friends",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedUserId = userId,
                RelatedUsername = removedBy.UserName,
                RelatedUserProfilePicture = removedBy.ProfilePictureUrl,
                RelatedEntityId = friendshipId
            };

            await _notificationService.SendNotificationToUserAsync(otherUserId, notificationDto);

            // Update notification badge count for the other user
            var unreadCount = await _notificationService.GetUnreadCountAsync(otherUserId);
            await _notificationService.SendNotificationCountToUserAsync(otherUserId, unreadCount);

            return true;
        }

        public async Task<List<FriendDto>> GetFriendsAsync(string userId)
        {
            var friendships = await _friendshipRepository.GetFriendsAsync(userId);

            var friends = friendships.Select(f =>
            {
                // Determine which user is the friend
                var friendUser = f.UserId == userId ? f.Friend : f.User;

                return new FriendDto
                {
                    UserId = friendUser.Id,
                    Username = friendUser.UserName ?? "",
                    ProfilePictureUrl = friendUser.ProfilePictureUrl,
                    FriendsSince = f.AcceptedAt ?? f.RequestedAt,
                    FriendshipId = f.Id
                };
            }).ToList();

            return friends;
        }

        public async Task<List<PendingRequestDto>> GetPendingRequestsAsync(string userId)
        {
            var requests = await _friendshipRepository.GetPendingRequestsAsync(userId);

            var pendingRequests = requests.Select(r => new PendingRequestDto
            {
                FriendshipId = r.Id,
                UserId = r.User.Id,
                Username = r.User.UserName ?? "",
                ProfilePictureUrl = r.User.ProfilePictureUrl,
                RequestedAt = r.RequestedAt
            }).ToList();

            return pendingRequests;
        }

        public async Task<FriendshipStatusDto> GetFriendshipStatusAsync(string userId, string otherUserId)
        {
            var friendship = await _friendshipRepository.GetFriendshipAsync(userId, otherUserId);

            if (friendship == null)
            {
                return new FriendshipStatusDto
                {
                    AreFriends = false,
                    HasPendingRequest = false,
                    IsSentByMe = false,
                    FriendshipId = null
                };
            }

            return new FriendshipStatusDto
            {
                AreFriends = friendship.Status == FriendshipStatus.Accepted,
                HasPendingRequest = friendship.Status == FriendshipStatus.Pending,
                IsSentByMe = friendship.UserId == userId,
                FriendshipId = friendship.Id
            };
        }

        public async Task<bool> AreFriendsAsync(string userId, string friendUserId)
        {
            return await _friendshipRepository.AreFriendsAsync(userId, friendUserId);
        }
    }
}