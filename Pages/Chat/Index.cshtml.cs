using ip_connect.Services.Messages;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ip_connect.Pages.Chat;

public class IndexModel : PageModel
{
    private readonly IMessageService _messageService;

    public IndexModel(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public async Task OnGetAsync()
    {
        // Load recent messages when page loads
        Messages = await _messageService.GetRecentMessagesAsync(50);
    }

    public List<ip_connect.Models.Message> Messages { get; set; } = new();
}