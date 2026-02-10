using FluentAssertions;
using ip_connect.Models;
using ip_connect.Repositories.ConversationRepository;
using ip_connect.Tests.Helpers;

namespace ip_connect.Tests.RepositoryTests
{
    public class ConversationRepositoryTests
    {
        [Fact]
        public async Task GetPrivateConversationAsync_ConversationExists_ReturnsConversation()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(user1, user2);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            var member1 = new ConversationMember { ConversationId = conversation.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member2 = new ConversationMember { ConversationId = conversation.Id, UserId = user2.Id, JoinedAt = DateTime.UtcNow };

            context.ConversationMembers.AddRange(member1, member2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetPrivateConversationAsync(user1.Id, user2.Id);

            // Assert
            result.Should().NotBeNull();
            result!.IsGroup.Should().BeFalse();
            result.Id.Should().Be(conversation.Id);
        }

        [Fact]
        public async Task GetPrivateConversationAsync_ConversationExistsReversed_ReturnsConversation()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(user1, user2);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            var member1 = new ConversationMember { ConversationId = conversation.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member2 = new ConversationMember { ConversationId = conversation.Id, UserId = user2.Id, JoinedAt = DateTime.UtcNow };

            context.ConversationMembers.AddRange(member1, member2);
            await context.SaveChangesAsync();

            // Act - Search in reverse order
            var result = await repository.GetPrivateConversationAsync(user2.Id, user1.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(conversation.Id);
        }

        [Fact]
        public async Task GetPrivateConversationAsync_ConversationDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            // Act
            var result = await repository.GetPrivateConversationAsync("user1", "user2");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPrivateConversationAsync_GroupConversationExists_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            var groupConversation = new Conversation { IsGroup = true, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(user1, user2);
            context.Conversations.Add(groupConversation);
            await context.SaveChangesAsync();

            var member1 = new ConversationMember { ConversationId = groupConversation.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member2 = new ConversationMember { ConversationId = groupConversation.Id, UserId = user2.Id, JoinedAt = DateTime.UtcNow };

            context.ConversationMembers.AddRange(member1, member2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetPrivateConversationAsync(user1.Id, user2.Id);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserConversationsAsync_HasConversations_ReturnsConversationsWithMembers()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "user3" };

            var conv1 = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            var conv2 = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(user1, user2, user3);
            context.Conversations.AddRange(conv1, conv2);
            await context.SaveChangesAsync();

            // User1 in both conversations
            var member1 = new ConversationMember { ConversationId = conv1.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member2 = new ConversationMember { ConversationId = conv1.Id, UserId = user2.Id, JoinedAt = DateTime.UtcNow };
            var member3 = new ConversationMember { ConversationId = conv2.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member4 = new ConversationMember { ConversationId = conv2.Id, UserId = user3.Id, JoinedAt = DateTime.UtcNow };

            context.ConversationMembers.AddRange(member1, member2, member3, member4);
            await context.SaveChangesAsync();

            // Add at least one message to each conversation
            var message1 = new Message { ConversationId = conv1.Id, SenderId = user1.Id, Text = "Test", Timestamp = DateTime.UtcNow };
            var message2 = new Message { ConversationId = conv2.Id, SenderId = user1.Id, Text = "Test", Timestamp = DateTime.UtcNow };
            context.Messages.AddRange(message1, message2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserConversationsAsync(user1.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(c => c.Members.Any(m => m.UserId == user1.Id));
        }

        [Fact]
        public async Task GetUserConversationsAsync_NoConversations_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserConversationsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserConversationsAsync_IncludesMessages()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(user1, user2);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            var member1 = new ConversationMember { ConversationId = conversation.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member2 = new ConversationMember { ConversationId = conversation.Id, UserId = user2.Id, JoinedAt = DateTime.UtcNow };

            context.ConversationMembers.AddRange(member1, member2);

            var message1 = new Message { ConversationId = conversation.Id, SenderId = user1.Id, Text = "Hello", Timestamp = DateTime.UtcNow };
            var message2 = new Message { ConversationId = conversation.Id, SenderId = user2.Id, Text = "Hi", Timestamp = DateTime.UtcNow };

            context.Messages.AddRange(message1, message2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserConversationsAsync(user1.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Messages.Should().HaveCount(2);
            result[0].Messages.Should().Contain(m => m.Text == "Hello");
            result[0].Messages.Should().Contain(m => m.Text == "Hi");
        }
    }
}