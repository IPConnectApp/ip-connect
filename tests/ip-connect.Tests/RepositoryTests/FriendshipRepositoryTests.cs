using FluentAssertions;
using ip_connect.Models;
using ip_connect.Models.Enums;
using ip_connect.Repositories.FriendshipRepository;
using ip_connect.Tests.Helpers;

namespace ip_connect.Tests.RepositoryTests
{
    public class FriendshipRepositoryTests
    {
        [Fact]
        public async Task GetFriendshipAsync_FriendshipExists_ReturnsFriendship()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var friendship = new Friendship
            {
                UserId = user1.Id,
                FriendId = user2.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow
            };
            context.Friendships.Add(friendship);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetFriendshipAsync(user1.Id, user2.Id);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(user1.Id);
            result.FriendId.Should().Be(user2.Id);
        }

        [Fact]
        public async Task GetFriendshipAsync_FriendshipExistsReversed_ReturnsFriendship()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var friendship = new Friendship
            {
                UserId = user1.Id,
                FriendId = user2.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow
            };
            context.Friendships.Add(friendship);
            await context.SaveChangesAsync();

            // Act - Search in reverse order (bidirectional)
            var result = await repository.GetFriendshipAsync(user2.Id, user1.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(friendship.Id);
        }

        [Fact]
        public async Task GetFriendshipAsync_FriendshipDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            // Act
            var result = await repository.GetFriendshipAsync("user1", "user2");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetFriendsAsync_HasAcceptedFriends_ReturnsOnlyAccepted()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var friend1 = new ApplicationUser { Id = "friend1", UserName = "friend1" };
            var friend2 = new ApplicationUser { Id = "friend2", UserName = "friend2" };
            var pending = new ApplicationUser { Id = "pending", UserName = "pending" };

            context.Users.AddRange(user1, friend1, friend2, pending);
            await context.SaveChangesAsync();

            // Accepted friendships
            var acceptedFriendship1 = new Friendship
            {
                UserId = user1.Id,
                FriendId = friend1.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            };

            var acceptedFriendship2 = new Friendship
            {
                UserId = friend2.Id,
                FriendId = user1.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            };

            // Pending friendship (should not be included)
            var pendingFriendship = new Friendship
            {
                UserId = user1.Id,
                FriendId = pending.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            context.Friendships.AddRange(acceptedFriendship1, acceptedFriendship2, pendingFriendship);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetFriendsAsync(user1.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(f => f.Status == FriendshipStatus.Accepted);
        }

        [Fact]
        public async Task GetFriendsAsync_NoFriends_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetFriendsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFriendsAsync_IncludesBothDirections()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            var friend1 = new ApplicationUser { Id = "friend1", UserName = "friend1" };
            var friend2 = new ApplicationUser { Id = "friend2", UserName = "friend2" };

            context.Users.AddRange(user, friend1, friend2);
            await context.SaveChangesAsync();

            // User -> Friend1
            context.Friendships.Add(new Friendship
            {
                UserId = user.Id,
                FriendId = friend1.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            });

            // Friend2 -> User
            context.Friendships.Add(new Friendship
            {
                UserId = friend2.Id,
                FriendId = user.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetFriendsAsync(user.Id);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPendingRequestsAsync_HasPendingRequests_ReturnsOnlyReceived()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            var sender1 = new ApplicationUser { Id = "sender1", UserName = "sender1" };
            var sender2 = new ApplicationUser { Id = "sender2", UserName = "sender2" };
            var recipient = new ApplicationUser { Id = "recipient", UserName = "recipient" };

            context.Users.AddRange(user, sender1, sender2, recipient);
            await context.SaveChangesAsync();

            // Requests received by user (should be included)
            context.Friendships.Add(new Friendship
            {
                UserId = sender1.Id,
                FriendId = user.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            context.Friendships.Add(new Friendship
            {
                UserId = sender2.Id,
                FriendId = user.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            // Request sent by user (should NOT be included)
            context.Friendships.Add(new Friendship
            {
                UserId = user.Id,
                FriendId = recipient.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetPendingRequestsAsync(user.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(f => f.FriendId == user.Id);
        }

        [Fact]
        public async Task GetPendingRequestsAsync_NoPendingRequests_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetPendingRequestsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSentRequestsAsync_HasSentRequests_ReturnsOnlySent()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            var recipient1 = new ApplicationUser { Id = "recipient1", UserName = "recipient1" };
            var recipient2 = new ApplicationUser { Id = "recipient2", UserName = "recipient2" };
            var sender = new ApplicationUser { Id = "sender", UserName = "sender" };

            context.Users.AddRange(user, recipient1, recipient2, sender);
            await context.SaveChangesAsync();

            // Requests sent by user (should be included)
            context.Friendships.Add(new Friendship
            {
                UserId = user.Id,
                FriendId = recipient1.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            context.Friendships.Add(new Friendship
            {
                UserId = user.Id,
                FriendId = recipient2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            // Request received by user (should NOT be included)
            context.Friendships.Add(new Friendship
            {
                UserId = sender.Id,
                FriendId = user.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetSentRequestsAsync(user.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(f => f.UserId == user.Id);
        }

        [Fact]
        public async Task GetSentRequestsAsync_NoSentRequests_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetSentRequestsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AreFriendsAsync_Accepted_ReturnsTrue()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            context.Friendships.Add(new Friendship
            {
                UserId = user1.Id,
                FriendId = user2.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.AreFriendsAsync(user1.Id, user2.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreFriendsAsync_Pending_ReturnsFalse()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            context.Friendships.Add(new Friendship
            {
                UserId = user1.Id,
                FriendId = user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.AreFriendsAsync(user1.Id, user2.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AreFriendsAsync_NoFriendship_ReturnsFalse()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            // Act
            var result = await repository.AreFriendsAsync("user1", "user2");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetFriendshipWithUsersAsync_FriendshipExists_IncludesUserAndFriend()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "Alice", ProfilePictureUrl = "/alice.jpg" };
            var friend = new ApplicationUser { Id = "user2", UserName = "Bob", ProfilePictureUrl = "/bob.jpg" };
            context.Users.AddRange(user, friend);
            await context.SaveChangesAsync();

            var friendship = new Friendship
            {
                UserId = user.Id,
                FriendId = friend.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            };
            context.Friendships.Add(friendship);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetFriendshipWithUsersAsync(friendship.Id);

            // Assert
            result.Should().NotBeNull();
            result!.User.Should().NotBeNull();
            result.User.UserName.Should().Be("Alice");
            result.Friend.Should().NotBeNull();
            result.Friend.UserName.Should().Be("Bob");
        }

        [Fact]
        public async Task GetFriendshipWithUsersAsync_FriendshipDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            // Act
            var result = await repository.GetFriendshipWithUsersAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateStatusAsync_UpdatesToAccepted_SetsAcceptedAt()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var friendship = new Friendship
            {
                UserId = user1.Id,
                FriendId = user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            context.Friendships.Add(friendship);
            await context.SaveChangesAsync();

            // Act
            await repository.UpdateStatusAsync(friendship.Id, FriendshipStatus.Accepted);

            // Assert
            var updated = await context.Friendships.FindAsync(friendship.Id);
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(FriendshipStatus.Accepted);
            updated.AcceptedAt.Should().NotBeNull();
            updated.AcceptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    
        [Fact]
        public async Task UpdateStatusAsync_FriendshipDoesNotExist_DoesNotThrow()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new FriendshipRepository(context);
            
            // Act
            Func<Task> act = async () => await repository.UpdateStatusAsync(999, FriendshipStatus.Accepted);
            
            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
