using ip_connect.Dtos.UserProfile;
using ip_connect.Models;
using ip_connect.Services.UserProfileService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

//This controller will be changed
namespace ip_connect.Controllers
{
    [Authorize] // Must be logged in to access profile
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _profileService;

        public ProfileController(UserManager<ApplicationUser> userManager, IUserProfileService profileService)
        {
            _userManager = userManager;
            _profileService = profileService;
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
        // GET: /profile/{username}/settings
        public async Task<IActionResult> Settings(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound();

            // Проверка дали текущият потребител е собственик на профила
            if (User.Identity?.Name != username)
            {
                return RedirectToAction("Photos", new { username });
            }

            // Взимаме DTO от сървиса
            var profileDto = await _profileService.GetOrCreateProfileAsync(user.Id, user.UserName);

            ViewData["Username"] = username;
            ViewData["CurrentTab"] = "Settings";
            ViewData["ProfilePictureUrl"] = profileDto.ProfilePictureUrl ?? "/images/default-avatar.jpg";

            return View(profileDto);
        }

        // POST: Update Info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInfo(UserProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                // Връщаме грешките
                ViewData["Username"] = User.Identity?.Name;
                ViewData["CurrentTab"] = "Settings";
                return View("Settings", model);
            }

            await _profileService.UpdateProfileAsync(model);

            TempData["SuccessMessage"] = "Profile information updated successfully!";
            return RedirectToAction("Settings");
        }

        // POST: Update Avatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatarFile)
        {
            var username = User.Identity?.Name;
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound();

            if (avatarFile != null && avatarFile.Length > 0)
            {

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + avatarFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 2. Запазваме файла на диска
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                // 3. Обновяваме URL-а в базата чрез Сървиса
                string newPhotoUrl = "/images/" + uniqueFileName;
                await _profileService.UpdateProfilePictureAsync(user.Id, newPhotoUrl);

                TempData["SuccessMessage"] = "Profile picture updated!";
            }
            else
            {
                TempData["ErrorMessage"] = "Please select a valid image file.";
            }

            return RedirectToAction("Settings", new { username });
        }
    }
}