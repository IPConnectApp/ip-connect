using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//This controller will be changed
namespace ip_connect.Controllers
{
    [Authorize] // Must be logged in to access profile
    public class ProfileController : Controller
    {
        // /profile → Redirects to logged-in user's profile
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            return RedirectToAction("Chats", new { username });
        }

        // /profile/{username}/photos
        public IActionResult Photos(string username)
        {
            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Photos";
            return View();
        }

        // /profile/{username}/friends
        public IActionResult Friends(string username)
        {
            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Friends";
            return View();
        }

        // /profile/{username}/chats - Only owner can access
        public IActionResult Chats(string username)
        {
            // Check if viewing own profile
            if (User.Identity?.Name != username)
            {
                return RedirectToAction("Photos", new { username });
            }

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Chats";
            return View();
        }

        // /profile/{username}/settings - Only owner can access
        public IActionResult Settings(string username)
        {
            // Check if viewing own profile
            if (User.Identity?.Name != username)
            {
                return RedirectToAction("Photos", new { username });
            }

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Settings";
            return View();
        }
    }
}