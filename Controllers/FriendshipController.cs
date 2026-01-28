using ip_connect.DTOs.Friendship;
using ip_connect.Services.FriendshipService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ip_connect.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        // POST: api/friendship/send
        [HttpPost("send")]
        public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.SendFriendRequestAsync(userId, dto.FriendUserId);
            return Ok(new { success = result, message = "Friend request sent successfully" });
        }

        // POST: api/friendship/accept/{friendshipId}
        [HttpPost("accept/{friendshipId}")]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.AcceptFriendRequestAsync(userId, friendshipId);
            return Ok(new { success = result, message = "Friend request accepted" });
        }

        // POST: api/friendship/reject/{friendshipId}
        [HttpPost("reject/{friendshipId}")]
        public async Task<IActionResult> RejectFriendRequest(int friendshipId)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.RejectFriendRequestAsync(userId, friendshipId);
            return Ok(new { success = result, message = "Friend request rejected" });
        }

        // DELETE: api/friendship/remove/{friendshipId}
        [HttpDelete("remove/{friendshipId}")]
        public async Task<IActionResult> RemoveFriend(int friendshipId)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.RemoveFriendAsync(userId, friendshipId);
            return Ok(new { success = result, message = "Friend removed successfully" });
        }

        // GET: api/friendship/friends
        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetCurrentUserId();
            var friends = await _friendshipService.GetFriendsAsync(userId);
            return Ok(friends);
        }

        // GET: api/friendship/pending
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetCurrentUserId();
            var requests = await _friendshipService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }

        // GET: api/friendship/status/{otherUserId}
        [HttpGet("status/{otherUserId}")]
        public async Task<IActionResult> GetFriendshipStatus(string otherUserId)
        {
            var userId = GetCurrentUserId();
            var status = await _friendshipService.GetFriendshipStatusAsync(userId, otherUserId);
            return Ok(status);
        }

        // GET: api/friendship/are-friends/{otherUserId}
        [HttpGet("are-friends/{otherUserId}")]
        public async Task<IActionResult> AreFriends(string otherUserId)
        {
            var userId = GetCurrentUserId();
            var areFriends = await _friendshipService.AreFriendsAsync(userId, otherUserId);
            return Ok(new { areFriends });
        }

        // GET: api/friendship/friends/{userId}
        [HttpGet("friends/{userId}")]
        public async Task<IActionResult> GetUserFriends(string userId)
        {
            var currentUserId = GetCurrentUserId();

            // Check if they're friends or viewing own profile
            if (currentUserId != userId)
            {
                var areFriends = await _friendshipService.AreFriendsAsync(currentUserId, userId);
                if (!areFriends)
                {
                    return Forbid();
                }
            }

            var friends = await _friendshipService.GetFriendsAsync(userId);
            return Ok(friends);
        }
    }
}