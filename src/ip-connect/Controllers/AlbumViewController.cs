using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ip_connect.Controllers
{
    [Authorize]
    public class AlbumViewController : Controller
    {
        // GET: /album/{albumId}
        [Route("album/{albumId}")]
        public IActionResult Details(int albumId)
        {
            ViewData["AlbumId"] = albumId;
            return View("~/Views/Album/Details.cshtml");
        }
    }
}