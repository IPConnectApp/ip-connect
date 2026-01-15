using Microsoft.AspNetCore.Mvc;

namespace ip_connect.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // If logged in, redirect to user's profile
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                return RedirectToAction("Photos", "Profile", new { username });
            }

            // If not logged in, redirect to login
            return RedirectToAction("Login", "Account");
        }
    }
}