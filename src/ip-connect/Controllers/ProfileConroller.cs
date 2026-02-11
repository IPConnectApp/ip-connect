using ip_connect.Dtos.UserProfile;
using ip_connect.Models;
using ip_connect.Services.Albums;
using ip_connect.Services.BlobStorage;
using ip_connect.Services.FriendshipService;
using ip_connect.Services.UserProfileService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

//This controller will be changed
namespace ip_connect.Controllers
{
    [Authorize] // Must be logged in to access profile
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _profileService;
        private readonly IFriendshipService _friendshipService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IAlbumService _albumService;

        public ProfileController(UserManager<ApplicationUser> userManager, IFriendshipService friendshipService, IUserProfileService profileService, IBlobStorageService blobStorageService,
            IAlbumService albumService)
        {
            _userManager = userManager;
            _friendshipService = friendshipService;
            _profileService = profileService;
            _blobStorageService = blobStorageService;
            _albumService = albumService;
        }
        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // /profile → Redirects to logged-in user's profile
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            return RedirectToAction("Chats", new { username });
        }

        // /profile/{username}/albums
        public async Task<IActionResult> Albums(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null) return NotFound();

            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var isOwnProfile = currentUserId == user.Id;
            var areFriends = false;

            if (!isOwnProfile)
            {
                var friendshipStatus = await _friendshipService.GetFriendshipStatusAsync(currentUserId, user.Id);
                areFriends = friendshipStatus.AreFriends;
                ViewData["HasPendingRequest"] = friendshipStatus.HasPendingRequest;
            }

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Albums";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            ViewData["IsOwnProfile"] = isOwnProfile;
            ViewData["AreFriends"] = areFriends;
            ViewData["ProfileUserId"] = user.Id;

            return View();
        }

        // /profile/{username}/friends
        public async Task<IActionResult> Friends(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var isOwnProfile = currentUserId == user.Id;
            var areFriends = false;

            if (!isOwnProfile)
            {
                var friendshipStatus = await _friendshipService.GetFriendshipStatusAsync(currentUserId, user.Id);
                areFriends = friendshipStatus.AreFriends;
                ViewData["HasPendingRequest"] = friendshipStatus.HasPendingRequest;
            }

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Friends";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            ViewData["IsOwnProfile"] = isOwnProfile;
            ViewData["AreFriends"] = areFriends;
            ViewData["ProfileUserId"] = user.Id;

            return View();
        }

        // /profile/{username}/chats - Only owner can access
        public async Task<IActionResult> Chats(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();

            // Only the owner can view settings
            if (currentUserId != user.Id)
                return Forbid();

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Chats";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            return View();
        }

        // /profile/{username}/settings - Only owner can access
        // GET: /profile/{username}/settings
        public async Task<IActionResult> Settings(string username)
        {
            Console.WriteLine("🟢 Settings page loaded");
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound();


            // Взимаме DTO от сървиса
            var displayName = user.UserName ?? username;
            var profileDto = await _profileService.GetOrCreateProfileAsync(user.Id, displayName);

            var currentUserId = GetCurrentUserId();

            // Only the owner can view settings
            if (currentUserId != user.Id)
                return Forbid();


            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Settings";
            ViewData["ProfilePictureUrl"] = profileDto.ProfilePictureUrl ?? "/images/default-avatar.jpg";

            return View(profileDto);
        }


        // POST: Update both avatar AND profile info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(UserProfileDto model, IFormFile? avatarFile)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound();

            // Update profile info
            if (ModelState.IsValid)
            {
                await _profileService.UpdateProfileAsync(model);
            }

            // Update avatar if provided
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Invalid file type.";
                    return RedirectToAction("Settings", new { username });
                }

                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File too large. Max 5MB.";
                    return RedirectToAction("Settings", new { username });
                }

                try
                {
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl) &&
                        !user.ProfilePictureUrl.Contains("/images/default-avatar.jpg"))
                    {
                        await _blobStorageService.DeleteFileAsync(user.ProfilePictureUrl);
                    }

                    using (var stream = avatarFile.OpenReadStream())
                    {
                        var imageUrl = await _blobStorageService.UploadProfilePictureAsync(stream, avatarFile.FileName);
                        await _profileService.UpdateProfilePictureAsync(user.Id, imageUrl);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error uploading image: {ex.Message}";
                    return RedirectToAction("Settings", new { username });
                }
            }

            TempData["SuccessMessage"] = "Settings updated successfully!";
            return RedirectToAction("Settings", new { username });
        }


        // /profile/album/{albumId} - Album details view
        [HttpGet("/profile/{username}/albums/{albumId}")]
        public async Task<IActionResult> Album(string username, int albumId)
        {
            var currentUserId = GetCurrentUserId();

            try
            {
                var album = await _albumService.GetAlbumByIdAsync(albumId, currentUserId);

                // Get the profile user info for the sidebar
                var profileUser = await _userManager.FindByNameAsync(username);
                if (profileUser == null) return NotFound();

                // Check if viewing own profile
                var isOwnProfile = album.UserId == currentUserId;

                // Check friendship if not own profile
                var areFriends = false;
                if (!isOwnProfile)
                {
                    var friendshipStatus = await _friendshipService.GetFriendshipStatusAsync(currentUserId, profileUser.Id);
                    areFriends = friendshipStatus.AreFriends;
                    ViewData["HasPendingRequest"] = friendshipStatus.HasPendingRequest;
                }

                ViewData["AlbumId"] = albumId;
                ViewData["AlbumName"] = album.Name;
                ViewData["AlbumDescription"] = album.Description;
                ViewData["IsOwnProfile"] = isOwnProfile;
                ViewData["AreFriends"] = areFriends;
                ViewData["Username"] = username;
                ViewData["CurrentTab"] = "Albums";
                ViewData["ProfilePictureUrl"] = profileUser.ProfilePictureUrl ?? "/images/default-avatar.jpg";
                ViewData["ProfileUserId"] = profileUser.Id;

                return View("Album");
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}