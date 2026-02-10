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
    public class PhotoController : Controller
    {
        private readonly IPhotoService _photoService;
        private readonly ILogger<PhotoController> _logger;

        public PhotoController(IPhotoService photoService, ILogger<PhotoController> logger)
        {
            _photoService = photoService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        /// <summary>
        /// Get all photos for a specific album
        /// GET: api/photo/album/{albumId}
        /// </summary>
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting photos for album {albumId}");
                return StatusCode(500, new { error = "An error occurred while retrieving photos" });
            }
        }

        /// <summary>
        /// Upload multiple photos to an album
        /// POST: api/photo/upload
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhotos([FromForm] List<IFormFile> files, [FromForm] int albumId)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest(new { error = "No files provided" });
                }

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
                            _logger.LogWarning($"Failed to upload file {file.FileName}: {ex.Message}");
                            // Continue with other files
                        }
                    }
                }

                if (!uploadedPhotos.Any())
                {
                    return BadRequest(new { error = "No files were successfully uploaded" });
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photos");
                return StatusCode(500, new { error = "An error occurred while uploading photos" });
            }
        }

        /// <summary>
        /// Delete a photo
        /// DELETE: api/photo/{id}
        /// </summary>
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting photo {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the photo" });
            }
        }

        /// <summary>
        /// Reorder photos in an album
        /// PUT: api/photo/reorder
        /// </summary>
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderPhotos([FromBody] List<PhotoReorderDto> reorderList)
        {
            try
            {
                if (reorderList == null || !reorderList.Any())
                {
                    return BadRequest(new { error = "Reorder list cannot be empty" });
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering photos");
                return StatusCode(500, new { error = "An error occurred while reordering photos" });
            }
        }
    }
}
