using ip_connect.DTOs;
using ip_connect.Services.Albums;
using ip_connect.Services.FriendshipService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ip_connect.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumController : ControllerBase
    {
        private readonly IAlbumService _albumService;
        private readonly IFriendshipService _friendshipService;

        public AlbumController(IAlbumService albumService, IFriendshipService friendshipService)
        {
            _albumService = albumService;
            _friendshipService = friendshipService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        // GET: api/album/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAlbums(string userId)
        {
            var currentUserId = GetCurrentUserId();

            // Privacy check - same as FriendshipController
            if (currentUserId != userId)
            {
                var areFriends = await _friendshipService.AreFriendsAsync(currentUserId, userId);
                if (!areFriends)
                {
                    return Forbid();
                }
            }

            var albums = await _albumService.GetUserAlbumsAsync(userId);
            return Ok(albums);
        }

        // GET: api/album/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlbum(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
            {
                return NotFound(new { message = "Album not found" });
            }
            return Ok(album);
        }

        // POST: api/album
        [HttpPost]
        public async Task<IActionResult> CreateAlbum([FromBody] CreateAlbumDto createDto)
        {
            var userId = GetCurrentUserId();
            var album = await _albumService.CreateAlbumAsync(userId, createDto);
            return Ok(album);
        }

        // PUT: api/album/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlbum(int id, [FromBody] UpdateAlbumDto updateDto)
        {
            var userId = GetCurrentUserId();
            var album = await _albumService.UpdateAlbumAsync(id, userId, updateDto);
            return Ok(album);
        }

        // DELETE: api/album/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var userId = GetCurrentUserId();
            await _albumService.DeleteAlbumAsync(id, userId);
            return Ok(new { message = "Album deleted successfully" });
        }
    }
}
