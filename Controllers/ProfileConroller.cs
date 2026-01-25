using ip_connect.Models;
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

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Photos";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            return View();
        }

        // /profile/{username}/friends
        public async Task<IActionResult> Friends(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound();

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Friends";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            return View();
        }

        // /profile/{username}/chats - Only owner can access
        public async Task<IActionResult> Chats(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound();

            // Check if viewing own profile
            if (User.Identity?.Name != username)
            {
                return RedirectToAction("Photos", new { username });
            }

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

            // Check if viewing own profile
            if (User.Identity?.Name != username)
            {
                return RedirectToAction("Photos", new { username });
            }

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Settings";
            ViewData["ProfilePictureUrl"] = user.ProfilePictureUrl ?? "/images/default-avatar.jpg";
            return View();
        }
    }
}