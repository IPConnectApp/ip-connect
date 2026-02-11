using ip_connect.Dtos.Photo;
using ip_connect.DTOs;
using ip_connect.Exceptions;
using ip_connect.Services.Albums;
using ip_connect.Services.FriendshipService;
using ip_connect.Services.Photos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ip_connect.Controllers
{
    [Authorize]
    [Route("api/albums")]
    [ApiController]
    public class AlbumController : ControllerBase
    {
        private readonly IAlbumService _albumService;
        private readonly IPhotoService _photoService;
        private readonly IFriendshipService _friendshipService;
        private readonly ILogger<AlbumController> _logger;

        public AlbumController(
            IAlbumService albumService,
            IPhotoService photoService,
            IFriendshipService friendshipService,
            ILogger<AlbumController> logger)
        {
            _albumService = albumService;
            _photoService = photoService;
            _friendshipService = friendshipService;
            _logger = logger;
        }

        private string GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated");


        // ========== ALBUM ENDPOINTS ==========


        // GET: api/album/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAlbums(string userId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId != userId)
            {
                var areFriends = await _friendshipService.AreFriendsAsync(currentUserId, userId);
                if (!areFriends) return Forbid();
            }

            var albums = await _albumService.GetUserAlbumsAsync(userId);
            return Ok(albums);
        }

        // GET: api/album/{albumId}
        [HttpGet("{albumId}")]
        public async Task<IActionResult> GetAlbum(int albumId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var album = await _albumService.GetAlbumByIdAsync(albumId, currentUserId);
                return Ok(album);
            }
            catch (ForbiddenException)
            {
                return StatusCode(403, new { error = "Access denied" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
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

        // ========== PHOTO ENDPOINTS ==========

        // GET: api/album/{albumId}/photos
        [HttpGet("{albumId}/photos")]
        public async Task<IActionResult> GetPhotos(int albumId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var photos = await _photoService.GetPhotosByAlbumIdAsync(albumId, userId);
                return Ok(photos);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // POST: api/album/{albumId}/photos
        [HttpPost("{albumId}/photos")]
        public async Task<IActionResult> UploadPhotos(int albumId, [FromForm] List<IFormFile> files)
        {
            try
            {
                if (files == null || !files.Any())
                    return BadRequest(new { error = "No files provided" });

                var userId = GetCurrentUserId();
                var uploadedPhotos = new List<PhotoDto>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        try
                        {
                            var photoDto = await _photoService.UploadPhotoAsync(albumId, file, userId);
                            uploadedPhotos.Add(photoDto);
                        }
                        catch (BadRequestException ex)
                        {
                            _logger.LogWarning($"Failed to upload {file.FileName}: {ex.Message}");
                        }
                    }
                }

                if (!uploadedPhotos.Any())
                    return BadRequest(new { error = "No files were successfully uploaded" });

                return Ok(new
                {
                    message = $"{uploadedPhotos.Count} photo(s) uploaded successfully",
                    photos = uploadedPhotos
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // DELETE: api/album/photo/{photoId}
        [HttpDelete("photo/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _photoService.DeletePhotoAsync(photoId, userId);
                return Ok(new { message = "Photo deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // PUT: api/albums/photos/reorder
        [HttpPut("photos/reorder")]
        public async Task<IActionResult> ReorderPhotos([FromBody] List<PhotoReorderDto> reorderList)
        {
            try
            {
                if (reorderList == null || !reorderList.Any())
                    return BadRequest(new { error = "Reorder list cannot be empty" });

                var userId = GetCurrentUserId();
                await _photoService.ReorderPhotosAsync(reorderList, userId);

                return Ok(new { message = "Photos reordered successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}