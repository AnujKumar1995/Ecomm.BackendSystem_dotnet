using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "NotificationService", Timestamp = DateTime.UtcNow });
    }
}
