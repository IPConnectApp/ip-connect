using ip_connect.Dtos.UserProfile;
using ip_connect.Models;

namespace ip_connect.Services.UserProfileService
{
    public interface IUserProfileService
    {
        Task<UserProfileDto> GetOrCreateProfileAsync(string userId, string defaultDisplayName);

        Task UpdateProfileAsync(UserProfileDto userProfileDto);

        Task<bool> UpdateProfilePictureAsync(string userId, string newPictureUrl);
    }
}
