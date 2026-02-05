using ip_connect.DTOs.Chat;

namespace ip_connect.Services.UserService
{
    public interface IUserService
    {
        Task<List<UserSearchDto>> SearchUsersAsync(string query, string currentUserId);
    }
}