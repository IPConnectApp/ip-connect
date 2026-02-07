using ip_connect.Dtos.Photo;
using ip_connect.Exceptions;
using ip_connect.Services.Photos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ip_connect.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : ControllerBase
    {
        private readonly IPhotoService _photoService;

        public PhotoController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        // 1. GET: api/photo/album/{albumId}
        [HttpGet("album/{albumId}")]
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

        // 2. POST: api/photo/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhotos([FromForm] List<IFormFile> files, [FromForm] int albumId)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files received");

            var userId = GetCurrentUserId();
            var uploadedPhotos = new List<PhotoDto>();

            try
            {
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var photoDto = await _photoService.UploadPhotoAsync(albumId, file, userId);
                        uploadedPhotos.Add(photoDto);
                    }
                }

                return Ok(uploadedPhotos);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        // 3. DELETE: api/photo/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _photoService.DeletePhotoAsync(id, userId);
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

        // PUT: api/photo/reorder
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderPhotos([FromBody] List<PhotoReorderDto> reorderList)
        {
            var userId = GetCurrentUserId();

            try
            {
                // Verify user owns all photos being reordered
                foreach (var item in reorderList)
                {
                    var photo = await _photoService.GetPhotoByIdAsync(item.Id, userId);
                    if (photo.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                // Update display order
                await _photoService.ReorderPhotosAsync(reorderList, userId);

                return Ok(new { message = "Photos reordered successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to reorder photos" });
            }
        }
    }
}
