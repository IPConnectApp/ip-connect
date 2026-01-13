using Microsoft.AspNetCore.Mvc;

namespace ip_connect.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Check if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.Username = User.Identity.Name;
            }
            
            return View();
        }
    }
}