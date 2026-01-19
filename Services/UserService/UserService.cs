using ip_connect.DTOs.Chat;
using ip_connect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<List<UserSearchDto>> SearchUsersAsync(string query, string currentUserId)
        {
            return await _userManager.Users
                .Where(u => u.Id != currentUserId && u.UserName!.Contains(query))
                .Select(u => new UserSearchDto
                {
                    Id = u.Id,
                    Username = u.UserName!,
                    ProfilePictureUrl = u.ProfilePictureUrl
                })
                .Take(10)
                .ToListAsync();
        }
    }
}