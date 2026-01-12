using ip_connect.DTOs.Account;
using ip_connect.Models;
using Microsoft.AspNetCore.Identity;

namespace ip_connect.Services.Account
{
    public class AccountService : IAccountService
    {
        //We use UserManager and SignInManager from ASP.NET Core Identity for user management, not custom repositories
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterDto dto)
        {
            // Check if email already exists
            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "This email is already registered"
                });
            }

            // Check if username already exists
            var existingUserByUsername = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUserByUsername != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = "This username is already taken"
                });
            }

            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow
            };

            //Create user with password (Identity hash it automatically)
            var result = await _userManager.CreateAsync(user, dto.Password);

            // If registration successful, sign in the user
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            return result;
        }

        public Task<bool> LoginAsync(LoginDto dto)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync()
        {
            throw new NotImplementedException();
        }
    }
}