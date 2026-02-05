using ip_connect.DTOs.Chat;
using ip_connect.Exceptions;
using ip_connect.Services.ConversationService;
using ip_connect.Services.MessageService;
using ip_connect.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ip_connect.Controllers
{
    [Authorize]
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;

        public ChatController(
            IConversationService conversationService,
            IMessageService messageService,
            IUserService userService)
        {
            _conversationService = conversationService;
            _messageService = messageService;
            _userService = userService;
        }

        // GET: api/chat/search?query=john
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new BadRequestException("Search query is required");

            var currentUserId = GetCurrentUserId();
            var users = await _userService.SearchUsersAsync(query, currentUserId);

            return Ok(users);
        }

        // GET: api/chat/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetUserConversations()
        {
            var userId = GetCurrentUserId();
            var conversations = await _conversationService.GetUserConversationsAsync(userId);

            return Ok(conversations);
        }

        // POST: api/chat/start/{userId}
        [HttpPost("start/{userId}")]
        public async Task<IActionResult> StartConversation(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new BadRequestException("User ID is required");

            var currentUserId = GetCurrentUserId();

            if (currentUserId == userId)
                throw new BadRequestException("Cannot start conversation with yourself");

            var conversationDto = await _conversationService
                .GetOrCreatePrivateConversationAsync(currentUserId, userId);

            return Ok(conversationDto);
        }

        // GET: api/chat/messages/{conversationId}
        [HttpGet("messages/{conversationId}")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            if (conversationId <= 0)
                throw new BadRequestException("Invalid conversation ID");

            var messages = await _messageService.GetConversationMessagesAsync(conversationId);

            return Ok(messages);
        }

        // POST: api/chat/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException("Invalid message data");

            var senderId = GetCurrentUserId();
            var senderUsername = User.Identity?.Name ?? "Unknown";

            var message = await _messageService.SendMessageAsync(
                dto.ConversationId,
                senderId,
                senderUsername,
                dto.Text);

            return Ok(message);
        }

        // POST: api/chat/mark-read/{conversationId}
        [HttpPost("mark-read/{conversationId}")]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            if (conversationId <= 0)
                throw new BadRequestException("Invalid conversation ID");

            var userId = GetCurrentUserId();
            await _messageService.MarkConversationAsReadAsync(conversationId, userId);

            return Ok(new { success = true });
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        // GET: api/chat/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var conversations = await _conversationService.GetUserConversationsAsync(userId);

            // Sum all unread counts
            var totalUnread = conversations.Sum(c => c.UnreadCount);

            return Ok(new { count = totalUnread });
        }
    }
}