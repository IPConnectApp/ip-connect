using FluentAssertions;
using ip_connect.Hubs;
using ip_connect.Models;
using ip_connect.Repositories.ConversationMemberRepository;
using ip_connect.Repositories.ConversationRepository;
using ip_connect.Services.ConversationService;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class ConversationServiceTests
    {
        private Mock<IHubContext<ChatHub>> CreateMockHubContext()
        {
            var mockHubContext = new Mock<IHubContext<ChatHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            return mockHubContext;
        }

        [Fact]
        public async Task GetOrCreatePrivateConversationAsync_ConversationExists_ReturnsExisting()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            var existing = new Conversation
            {
                Id = 1,
                IsGroup = false,
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow
            };

            mockConvRepo.Setup(r => r.GetPrivateConversationAsync("user1", "user2"))
                .ReturnsAsync(existing);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetOrCreatePrivateConversationAsync("user1", "user2");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.IsGroup.Should().BeFalse();
            mockConvRepo.Verify(r => r.CreateAsync(It.IsAny<Conversation>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreatePrivateConversationAsync_NoConversation_CreatesNewConversation()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            mockConvRepo.Setup(r => r.GetPrivateConversationAsync("user1", "user2"))
                .ReturnsAsync((Conversation?)null);

            mockConvRepo.Setup(r => r.CreateAsync(It.IsAny<Conversation>()))
                .ReturnsAsync((Conversation c) => { c.Id = 5; return c; });

            mockMemberRepo.Setup(r => r.CreateAsync(It.IsAny<ConversationMember>()))
                .ReturnsAsync((ConversationMember cm) => cm);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetOrCreatePrivateConversationAsync("user1", "user2");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(5);
            mockConvRepo.Verify(r => r.CreateAsync(It.Is<Conversation>(c =>
                c.IsGroup == false && c.CreatedBy == "user1")), Times.Once);
        }

        [Fact]
        public async Task GetOrCreatePrivateConversationAsync_NoConversation_AddsBothMembers()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            var createdMembers = new List<ConversationMember>();

            mockConvRepo.Setup(r => r.GetPrivateConversationAsync("user1", "user2"))
                .ReturnsAsync((Conversation?)null);

            mockConvRepo.Setup(r => r.CreateAsync(It.IsAny<Conversation>()))
                .ReturnsAsync((Conversation c) => { c.Id = 10; return c; });

            mockMemberRepo.Setup(r => r.CreateAsync(It.IsAny<ConversationMember>()))
                .Callback<ConversationMember>(cm => createdMembers.Add(cm))
                .ReturnsAsync((ConversationMember cm) => cm);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            await service.GetOrCreatePrivateConversationAsync("user1", "user2");

            // Assert
            createdMembers.Should().HaveCount(2);
            createdMembers.Should().Contain(m => m.UserId == "user1" && m.ConversationId == 10);
            createdMembers.Should().Contain(m => m.UserId == "user2" && m.ConversationId == 10);
            mockMemberRepo.Verify(r => r.CreateAsync(It.IsAny<ConversationMember>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetOrCreatePrivateConversationAsync_NoConversation_SendsSignalRNotification()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockConvRepo.Setup(r => r.GetPrivateConversationAsync("user1", "user2"))
                .ReturnsAsync((Conversation?)null);

            mockConvRepo.Setup(r => r.CreateAsync(It.IsAny<Conversation>()))
                .ReturnsAsync((Conversation c) => { c.Id = 7; return c; });

            mockMemberRepo.Setup(r => r.CreateAsync(It.IsAny<ConversationMember>()))
                .ReturnsAsync((ConversationMember cm) => cm);

            mockClients.Setup(c => c.Group("user-user2")).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            await service.GetOrCreatePrivateConversationAsync("user1", "user2");

            // Assert
            mockClientProxy.Verify(p => p.SendCoreAsync(
                "NewConversationCreated",
                It.Is<object[]>(o => o.Length == 1),
                default), Times.Once);
        }

        [Fact]
        public async Task GetUserConversationsAsync_HasConversations_ReturnsDtosWithCorrectData()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice", ProfilePictureUrl = "/pic1.jpg" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };

            var conversations = new List<Conversation>
            {
                new Conversation
                {
                    Id = 1,
                    IsGroup = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Members = new List<ConversationMember>
                    {
                        new ConversationMember { UserId = "user1", User = user1, LastReadAt = DateTime.UtcNow.AddHours(-1) },
                        new ConversationMember { UserId = "user2", User = user2 }
                    },
                    Messages = new List<Message>
                    {
                        new Message { Id = 1, Text = "Hello", Timestamp = DateTime.UtcNow.AddHours(-2) },
                        new Message { Id = 2, Text = "Hi there", Timestamp = DateTime.UtcNow }
                    }
                }
            };

            mockConvRepo.Setup(r => r.GetUserConversationsAsync("user1"))
                .ReturnsAsync(conversations);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetUserConversationsAsync("user1");

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
            result[0].OtherUserName.Should().Be("bob");
            result[0].OtherUserProfilePicture.Should().Be("/pic2.jpg");
            result[0].LastMessageText.Should().Be("Hi there");
        }

        [Fact]
        public async Task GetUserConversationsAsync_HasConversations_CalculatesUnreadCountCorrectly()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob" };

            var lastReadTime = DateTime.UtcNow.AddHours(-1);

            var conversations = new List<Conversation>
            {
                new Conversation
                {
                    Id = 1,
                    IsGroup = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Members = new List<ConversationMember>
                    {
                        new ConversationMember { UserId = "user1", User = user1, LastReadAt = lastReadTime },
                        new ConversationMember { UserId = "user2", User = user2 }
                    },
                    Messages = new List<Message>
                    {
                        new Message { Id = 1, Text = "Old msg", Timestamp = lastReadTime.AddMinutes(-10) },
                        new Message { Id = 2, Text = "New msg 1", Timestamp = lastReadTime.AddMinutes(5) },
                        new Message { Id = 3, Text = "New msg 2", Timestamp = DateTime.UtcNow }
                    }
                }
            };

            mockConvRepo.Setup(r => r.GetUserConversationsAsync("user1"))
                .ReturnsAsync(conversations);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetUserConversationsAsync("user1");

            // Assert
            result[0].UnreadCount.Should().Be(2);
        }

        [Fact]
        public async Task GetUserConversationsAsync_NoConversations_ReturnsEmptyList()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            mockConvRepo.Setup(r => r.GetUserConversationsAsync("user1"))
                .ReturnsAsync(new List<Conversation>());

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetUserConversationsAsync("user1");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationByIdAsync_ConversationExists_ReturnsDto()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            var conversation = new Conversation
            {
                Id = 3,
                IsGroup = false,
                CreatedAt = DateTime.UtcNow
            };

            mockConvRepo.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(conversation);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetConversationByIdAsync(3);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(3);
            result.IsGroup.Should().BeFalse();
        }

        [Fact]
        public async Task GetConversationByIdAsync_ConversationDoesNotExist_ReturnsNull()
        {
            // Arrange
            var mockConvRepo = new Mock<IConversationRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            mockConvRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Conversation?)null);

            var service = new ConversationService(mockConvRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetConversationByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }
    }
}
