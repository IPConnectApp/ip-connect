using System.Security.Claims;
using ip_connect.Models;
using ip_connect.Services.FriendshipService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

//This controller will be changed
namespace ip_connect.Controllers
{
    [Authorize] // Must be logged in to access profile
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFriendshipService _friendshipService;

        public ProfileController(UserManager<ApplicationUser> userManager, IFriendshipService friendshipService)
        {
            _userManager = userManager;
            _friendshipService = friendshipService;
        }

        // /profile → Redirects to logged-in user's profile
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            return RedirectToAction("Chats", new { username });
        }

        // /profile/{username}/photos
        public async Task<IActionResult> Photos(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var isOwnProfile = currentUserId == user.Id;
            var areFriends = false;

            if (!isOwnProfile)
            {
                areFriends = await _friendshipService.AreFriendsAsync(currentUserId, user.Id);
            }

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Photos";
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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var isOwnProfile = currentUserId == user.Id;
            var areFriends = false;

            if (!isOwnProfile)
            {
                areFriends = await _friendshipService.AreFriendsAsync(currentUserId, user.Id);
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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only the owner can view settings
            if (currentUserId != user.Id)
                return Forbid();

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Chats";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            return View();
        }

        // /profile/{username}/settings - Only owner can access
        public async Task<IActionResult> Settings(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only the owner can view settings
            if (currentUserId != user.Id)
                return Forbid();

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Settings";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            return View();
        }
    }
}