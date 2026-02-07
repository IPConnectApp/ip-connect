using ip_connect.Services.Albums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ip_connect.Controllers
{
    [Authorize]
    public class AlbumViewController : Controller
    {
        private readonly IAlbumService _albumService;

        public AlbumViewController(IAlbumService albumService)
        {
            _albumService = albumService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        // GET: /album/{albumId}
        [HttpGet("album/{albumId}")]
        public async Task<IActionResult> Details(int albumId)
        {
            var currentUserId = GetCurrentUserId();

            Console.WriteLine($"=== ALBUM DETAILS DEBUG ===");
            Console.WriteLine($"Current User ID: {currentUserId}");

            try
            {
                var album = await _albumService.GetAlbumByIdAsync(albumId, currentUserId);

                Console.WriteLine($"Album User ID: {album.UserId}");
                Console.WriteLine($"Are they equal? {album.UserId == currentUserId}");
                Console.WriteLine($"Album UserId type: {album.UserId?.GetType()}");
                Console.WriteLine($"Current UserId type: {currentUserId?.GetType()}");

                ViewData["AlbumId"] = albumId;
                ViewData["IsOwnProfile"] = album.UserId == currentUserId;
                ViewData["AlbumName"] = album.Name;

                Console.WriteLine($"ViewData IsOwnProfile: {ViewData["IsOwnProfile"]}");

                return View("~/Views/Album/Details.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}