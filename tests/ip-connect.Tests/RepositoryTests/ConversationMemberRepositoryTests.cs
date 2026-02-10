using FluentAssertions;
using ip_connect.Models;
using ip_connect.Repositories.ConversationMemberRepository;
using ip_connect.Tests.Helpers;

namespace ip_connect.Tests.RepositoryTests
{
    public class ConversationMemberRepositoryTests
    {
        [Fact]
        public async Task GetMembershipAsync_MemberExists_ReturnsMembership()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };

            context.Users.Add(user);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            var member = new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow,
                LastReadAt = DateTime.UtcNow
            };

            context.ConversationMembers.Add(member);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetMembershipAsync(conversation.Id, user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.ConversationId.Should().Be(conversation.Id);
            result.UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetMembershipAsync_MemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);

            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetMembershipAsync(conversation.Id, "nonexistent");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateLastReadAtAsync_MemberExists_UpdatesTimestamp()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };

            context.Users.Add(user);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            var oldTimestamp = DateTime.UtcNow.AddHours(-1);
            var member = new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow,
                LastReadAt = oldTimestamp
            };

            context.ConversationMembers.Add(member);
            await context.SaveChangesAsync();

            // Act
            await repository.UpdateLastReadAtAsync(conversation.Id, user.Id);

            // Assert
            var updated = await repository.GetMembershipAsync(conversation.Id, user.Id);
            updated.Should().NotBeNull();
            updated!.LastReadAt.Should().BeAfter(oldTimestamp);
            updated.LastReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateLastReadAtAsync_MemberDoesNotExist_DoesNotThrow()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);

            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await repository.UpdateLastReadAtAsync(conversation.Id, "nonexistent");

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GetConversationMembersAsync_HasMembers_ReturnsAllMembers()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "user3" };
            var conversation = new Conversation { IsGroup = true, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(user1, user2, user3);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            var member1 = new ConversationMember { ConversationId = conversation.Id, UserId = user1.Id, JoinedAt = DateTime.UtcNow };
            var member2 = new ConversationMember { ConversationId = conversation.Id, UserId = user2.Id, JoinedAt = DateTime.UtcNow };
            var member3 = new ConversationMember { ConversationId = conversation.Id, UserId = user3.Id, JoinedAt = DateTime.UtcNow };

            context.ConversationMembers.AddRange(member1, member2, member3);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetConversationMembersAsync(conversation.Id);

            // Assert
            result.Should().HaveCount(3);
            result.Select(m => m.UserId).Should().Contain(new[] { "user1", "user2", "user3" });
        }

        [Fact]
        public async Task GetConversationMembersAsync_NoMembers_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);

            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetConversationMembersAsync(conversation.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationMembersAsync_IncludesUserInfo()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new ConversationMemberRepository(context);
            
            var user = new ApplicationUser { Id = "user1", UserName = "TestUser", ProfilePictureUrl = "/images/avatar.jpg" };
            var conversation = new Conversation { IsGroup = false, CreatedAt = DateTime.UtcNow };
            
            context.Users.Add(user);
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();
            
            var member = new ConversationMember { ConversationId = conversation.Id, UserId = user.Id, JoinedAt = DateTime.UtcNow };
            context.ConversationMembers.Add(member);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetConversationMembersAsync(conversation.Id);
            
            // Assert
            result.Should().HaveCount(1);
            result[0].User.Should().NotBeNull();
            result[0].User.UserName.Should().Be("TestUser");
            result[0].User.ProfilePictureUrl.Should().Be("/images/avatar.jpg");
        }
    }
}