using ip_connect.Dtos.UserProfile;
using ip_connect.Models;
using ip_connect.Repositories.UserProfileRepository;
using Microsoft.AspNetCore.Identity;

namespace ip_connect.Services.UserProfileService
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserProfileService(
            IUserProfileRepository repository,
            UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        public async Task<UserProfileDto> GetOrCreateProfileAsync(string userId, string defaultDisplayName)
        {
            // 1. Взимаме данните за профила от новата таблица
            var profileEntity = await _repository.GetByUserIdAsync(userId);

            // 2. Взимаме юзъра от AspNetUsers, за да достъпим снимката!
            var userEntity = await _userManager.FindByIdAsync(userId);
            if (userEntity == null) throw new Exception("User not found");

            // 3. Ако няма профил - създаваме
            if (profileEntity == null)
            {
                profileEntity = new UserProfile
                {
                    UserId = userId,
                    DisplayName = defaultDisplayName,
                    CreatedAt = DateTime.UtcNow,
                    IsOnline = true
                };
                await _repository.CreateAsync(profileEntity);
            }

            // 4. Мапваме, като комбинираме данните (Профил + Снимка от User)
            var dto = MapToDto(profileEntity);

            // ВАЖНО: Тук слагаме снимката от AspNetUsers таблицата
            dto.ProfilePictureUrl = userEntity.ProfilePictureUrl;

            return dto;
        }

        public async Task UpdateProfileAsync(UserProfileDto dto)
        {
            var entity = await _repository.GetByUserIdAsync(dto.UserId);
            if (entity == null) return;

            // Обновяваме само полетата от UserProfiles таблицата
            entity.DisplayName = dto.DisplayName;
            entity.Bio = dto.Bio;
            entity.BirthDate = dto.BirthDate;
            entity.Gender = dto.Gender;

            await _repository.UpdateAsync(entity);

            // Забележка: Тук НЕ пипаме снимката, защото искаш отделен метод
        }

        // --- НОВИЯТ МЕТОД ЗА СНИМКАТА ---
        public async Task<bool> UpdateProfilePictureAsync(string userId, string newPictureUrl)
        {
            // Тук работим само с AspNetUsers таблицата чрез UserManager
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.ProfilePictureUrl = newPictureUrl;

            // Запазваме промяната в AspNetUsers
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        private static UserProfileDto MapToDto(UserProfile entity)
        {
            return new UserProfileDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                DisplayName = entity.DisplayName,
                Bio = entity.Bio,
                BirthDate = entity.BirthDate,
                Gender = entity.Gender,
                IsOnline = entity.IsOnline,
                LastSeen = entity.LastSeen,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
