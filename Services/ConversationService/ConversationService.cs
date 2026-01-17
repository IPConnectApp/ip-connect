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

        public async Task<Conversation?> GetOrCreatePrivateConversationAsync(string user1Id, string user2Id)
        {
            var existingConversation = await _conversationRepository
                .GetPrivateConversationAsync(user1Id, user2Id);

            if (existingConversation != null)
                return existingConversation;

            //Create new conversation
            var conversation = new Conversation
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

            return conversation;
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            return await _conversationRepository.GetUserConversationsAsync(userId);
        }

        public async Task<Conversation?> GetConversationByIdAsync(int id)
        {
            return await _conversationRepository.GetByIdAsync(id);
        }
    }
}