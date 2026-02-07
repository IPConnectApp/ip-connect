using FluentAssertions;
using ip_connect.Models;
using ip_connect.Repositories.AlbumRepository;
using ip_connect.Repositories.Messages;
using ip_connect.Tests.Helpers;

namespace ip_connect.Tests.RepositoryTests
{
    public class MessageRepositoryTests
    {
        [Fact]
        public async Task GetConversationMessagesAsync_HasMessages_ReturnsOrderedMessages()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new MessageRepository(context);

            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            var sender = new ApplicationUser { Id = "user1", UserName = "sender" };

            context.Conversations.Add(conversation);
            context.Users.Add(sender);
            await context.SaveChangesAsync();

            var message1 = new Message
            {
                ConversationId = conversation.Id,
                SenderId = sender.Id,
                Text = "First message",
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            };

            var message2 = new Message
            {
                ConversationId = conversation.Id,
                SenderId = sender.Id,
                Text = "Second message",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            };

            var message3 = new Message
            {
                ConversationId = conversation.Id,
                SenderId = sender.Id,
                Text = "Third message",
                Timestamp = DateTime.UtcNow
            };

            context.Messages.AddRange(message1, message2, message3);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetConversationMessagesAsync(conversation.Id);

            // Assert
            result.Should().HaveCount(3);
            result[0].Text.Should().Be("First message");
            result[1].Text.Should().Be("Second message");
            result[2].Text.Should().Be("Third message");
            result.Should().BeInAscendingOrder(m => m.Timestamp);
        }

        [Fact]
        public async Task GetConversationMessagesAsync_NoMessages_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new MessageRepository(context);

            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetConversationMessagesAsync(conversation.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationMessagesAsync_IncludesSenderInfo()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new MessageRepository(context);
            
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            var sender = new ApplicationUser { Id = "user1", UserName = "TestUser" };
            
            context.Conversations.Add(conversation);
            context.Users.Add(sender);
            await context.SaveChangesAsync();
            
            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = sender.Id,
                Text = "Test message",
                Timestamp = DateTime.UtcNow
            };
            
            context.Messages.Add(message);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetConversationMessagesAsync(conversation.Id);
            
            // Assert
            result.Should().HaveCount(1);
            result[0].Sender.Should().NotBeNull();
            result[0].Sender.UserName.Should().Be("TestUser");
        }
    
        [Fact]
        public async Task GetUserAlbumsAsync_OnlyReturnsUserAlbums()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();
            
            var user1Album = new Album { UserId = user1.Id, Name = "User 1 Album", CreatedAt = DateTime.UtcNow };
            var user2Album = new Album { UserId = user2.Id, Name = "User 2 Album", CreatedAt = DateTime.UtcNow };
            
            context.Albums.AddRange(user1Album, user2Album);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetUserAlbumsAsync(user1.Id);
            
            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("User 1 Album");
        }
    }
}