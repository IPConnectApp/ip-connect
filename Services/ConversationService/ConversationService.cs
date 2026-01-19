using ip_connect.DTOs.Chat;
using ip_connect.Models;
using ip_connect.Repositories.ConversationMemberRepository;
using ip_connect.Repositories.ConversationRepository;

namespace ip_connect.Services.ConversationService
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IConversationMemberRepository _conversationMemberRepository;

        public ConversationService(IConversationRepository conversationRepository,
                                   IConversationMemberRepository conversationMemberRepository)
        {
            _conversationRepository = conversationRepository;
            _conversationMemberRepository = conversationMemberRepository;
        }

        public async Task<ConversationDto> GetOrCreatePrivateConversationAsync(string user1Id, string user2Id)
        {
            var existingConversation = await _conversationRepository
                .GetPrivateConversationAsync(user1Id, user2Id);

            Conversation conversation;

            if (existingConversation != null)
            {
                conversation = existingConversation;
            }
            else
            {

                //Create new conversation
                conversation = new Conversation
                {
                    IsGroup = false,
                    CreatedBy = user1Id,
                    CreatedAt = DateTime.UtcNow
                };

                conversation = await _conversationRepository.CreateAsync(conversation);

                //Add both users as members
                await _conversationMemberRepository.CreateAsync(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = user1Id,
                    JoinedAt = DateTime.UtcNow
                });

                await _conversationMemberRepository.CreateAsync(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = user2Id,
                    JoinedAt = DateTime.UtcNow
                });
            }

            //Map to DTO
            return new ConversationDto
            {
                Id = conversation.Id,
                IsGroup = conversation.IsGroup,
                CreatedAt = conversation.CreatedAt
            };
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId)
        {
            var conversations = await _conversationRepository.GetUserConversationsAsync(userId);

            // Map to DTOs
            return conversations.Select(c =>
            {
                var lastMessage = c.Messages.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                var userMembership = c.Members.FirstOrDefault(m => m.UserId == userId);

                // Count unread messages (messages after LastReadAt)
                var unreadCount = 0;
                if (userMembership?.LastReadAt != null)
                {
                    unreadCount = c.Messages.Count(m => m.Timestamp > userMembership.LastReadAt);
                }
                else
                {
                    unreadCount = c.Messages.Count();
                }

                return new ConversationDto
                {
                    Id = c.Id,
                    IsGroup = c.IsGroup,
                    CreatedAt = c.CreatedAt,
                    OtherUserName = c.IsGroup ? null : c.Members.FirstOrDefault(m => m.UserId != userId)?.User.UserName,
                    OtherUserProfilePicture = c.IsGroup ? null : c.Members.FirstOrDefault(m => m.UserId != userId)?.User.ProfilePictureUrl,
                    LastMessageText = lastMessage?.Text,
                    LastMessageTime = lastMessage?.Timestamp,
                    UnreadCount = unreadCount
                };
            }).OrderByDescending(c => c.LastMessageTime)
            .ToList();
        }

        public async Task<ConversationDto?> GetConversationByIdAsync(int id)
        {
            var conversation = await _conversationRepository.GetByIdAsync(id);

            if (conversation == null)
                return null;

            return new ConversationDto
            {
                Id = conversation.Id,
                IsGroup = conversation.IsGroup,
                CreatedAt = conversation.CreatedAt
            };
        }
    }
}