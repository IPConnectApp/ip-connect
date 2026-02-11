using ip_connect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ip_connect.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // If logged in, redirect to user's profile
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userManager.FindByNameAsync(username);
                var correctUsername = user?.UserName ?? username;
                
                return RedirectToAction("Albums", "Profile", new { username = correctUsername });
            }

            // If not logged in, redirect to login
            return RedirectToAction("Login", "Account");
        }
    }
}