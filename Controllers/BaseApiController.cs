using ip_connect.Common;
using Microsoft.AspNetCore.Mvc;

namespace ip_connect.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, ErrorResponse.Create("You don't have permission to perform this action"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ErrorResponse.Create(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ErrorResponse.Create(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, ErrorResponse.Create("An unexpected error occurred"));
            }
        }
    }
}