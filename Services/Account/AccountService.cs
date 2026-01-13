using ip_connect.DTOs.Account;
using ip_connect.Models;
using Microsoft.AspNetCore.Identity;

namespace ip_connect.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterDto registerDto)
        {
            // Check if email already exists
            var existingUserByEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUserByEmail != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "This email is already registered"
                });
            }

            // Check if username already exists
            var existingUserByUsername = await _userManager.FindByNameAsync(registerDto.UserName);
            if (existingUserByUsername != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = "This username is already taken"
                });
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow
            };

            // Create user with password (Identity will hash it automatically)
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            return result;
        }

        public async Task<(bool Success, string ErrorMessage)> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Check if user exists by email
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return (false, "No account found with this email address");
                }

                // Attempt to sign in with password
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    loginDto.Password,
                    loginDto.RememberMe,
                    lockoutOnFailure: false
                );

                // Check specific failure reasons
                if (result.Succeeded)
                {
                    return (true, string.Empty);
                }

                // Default error for wrong password
                return (false, "Invalid email or password");
            }
            catch (Exception ex)
            {
                return (false, "An unexpected error occurred during login. Please try again");
            }
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}