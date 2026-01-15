using ip_connect.DTOs.Account;
using Microsoft.AspNetCore.Identity;

namespace ip_connect.Services.Account
{
    public interface IAccountService
    {
        Task<IdentityResult> RegisterAsync(RegisterDto registerDto);

        Task<(bool Success, string ErrorMessage)> LoginAsync(LoginDto loginDto);
        
        Task LogoutAsync();
    }
}