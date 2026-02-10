using FluentAssertions;
using ip_connect.Hubs;
using ip_connect.Models;
using ip_connect.Repositories.ConversationMemberRepository;
using ip_connect.Repositories.MessageRepository;
using ip_connect.Services.MessageService;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class MessageServiceTests
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
        public async Task SendMessageAsync_ValidData_CreatesMessage()
        {
            // Arrange
            var mockMessageRepo = new Mock<IMessageRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            mockMessageRepo.Setup(r => r.CreateAsync(It.IsAny<Message>()))
                .ReturnsAsync((Message m) => { m.Id = 10; return m; });

            mockMemberRepo.Setup(r => r.GetConversationMembersAsync(1))
                .ReturnsAsync(new List<ConversationMember>());

            var service = new MessageService(mockMessageRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.SendMessageAsync(1, "user1", "alice", "Hello world");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(10);
            result.ConversationId.Should().Be(2);
            result.SenderId.Should().Be("user1");
            result.SenderUsername.Should().Be("alice");
            result.Text.Should().Be("Hello world");
            mockMessageRepo.Verify(r => r.CreateAsync(It.Is<Message>(m =>
                m.ConversationId == 1 && m.SenderId == "user1" && m.Text == "Hello world")), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ValidData_BroadcastsToConversationGroup()
        {
            // Arrange
            var mockMessageRepo = new Mock<IMessageRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockMessageRepo.Setup(r => r.CreateAsync(It.IsAny<Message>()))
                .ReturnsAsync((Message m) => { m.Id = 5; return m; });

            mockMemberRepo.Setup(r => r.GetConversationMembersAsync(3))
                .ReturnsAsync(new List<ConversationMember>());

            mockClients.Setup(c => c.Group("conversation-3")).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var service = new MessageService(mockMessageRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            await service.SendMessageAsync(3, "user1", "alice", "Test message");

            // Assert
            mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                default), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ValidData_NotifiesAllMembers()
        {
            // Arrange
            var mockMessageRepo = new Mock<IMessageRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            var members = new List<ConversationMember>
            {
                new ConversationMember { UserId = "user1" },
                new ConversationMember { UserId = "user2" }
            };

            mockMessageRepo.Setup(r => r.CreateAsync(It.IsAny<Message>()))
                .ReturnsAsync((Message m) => { m.Id = 1; return m; });

            mockMemberRepo.Setup(r => r.GetConversationMembersAsync(1))
                .ReturnsAsync(members);

            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var service = new MessageService(mockMessageRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            await service.SendMessageAsync(1, "user1", "alice", "Hello");

            // Assert
            mockClientProxy.Verify(p => p.SendCoreAsync(
                "NewMessageNotification",
                It.Is<object[]>(o => o.Length == 1),
                default), Times.Exactly(2));
        }

        [Fact]
        public async Task GetConversationMessagesAsync_HasMessages_ReturnsDtosInOrder()
        {
            // Arrange
            var mockMessageRepo = new Mock<IMessageRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            var messages = new List<Message>
            {
                new Message
                {
                    Id = 1,
                    ConversationId = 5,
                    SenderId = "user1",
                    Sender = new ApplicationUser { UserName = "alice" },
                    Text = "First message",
                    Timestamp = DateTime.UtcNow.AddMinutes(-5)
                },
                new Message
                {
                    Id = 2,
                    ConversationId = 5,
                    SenderId = "user2",
                    Sender = new ApplicationUser { UserName = "bob" },
                    Text = "Second message",
                    Timestamp = DateTime.UtcNow
                }
            };

            mockMessageRepo.Setup(r => r.GetConversationMessagesAsync(5))
                .ReturnsAsync(messages);

            var service = new MessageService(mockMessageRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetConversationMessagesAsync(5);

            // Assert
            result.Should().HaveCount(2);
            result[0].Text.Should().Be("First message");
            result[0].SenderUsername.Should().Be("alice");
            result[1].Text.Should().Be("Second message");
            result[1].SenderUsername.Should().Be("bob");
        }

        [Fact]
        public async Task GetConversationMessagesAsync_NoMessages_ReturnsEmptyList()
        {
            // Arrange
            var mockMessageRepo = new Mock<IMessageRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            mockMessageRepo.Setup(r => r.GetConversationMessagesAsync(10))
                .ReturnsAsync(new List<Message>());

            var service = new MessageService(mockMessageRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            var result = await service.GetConversationMessagesAsync(10);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task MarkConversationAsReadAsync_ValidData_UpdatesLastReadAt()
        {
            // Arrange
            var mockMessageRepo = new Mock<IMessageRepository>();
            var mockMemberRepo = new Mock<IConversationMemberRepository>();
            var mockHub = CreateMockHubContext();

            mockMemberRepo.Setup(r => r.UpdateLastReadAtAsync(7, "user1"))
                .Returns(Task.CompletedTask);

            var service = new MessageService(mockMessageRepo.Object, mockMemberRepo.Object, mockHub.Object);

            // Act
            await service.MarkConversationAsReadAsync(7, "user1");

            // Assert
            mockMemberRepo.Verify(r => r.UpdateLastReadAtAsync(7, "user1"), Times.Once);
        }
    }
}
