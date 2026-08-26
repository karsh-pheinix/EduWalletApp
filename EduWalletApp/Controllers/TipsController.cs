using System.Security.Claims;
using EduWalletApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduWalletApp.Controllers;

[ApiController]
[Route("api/tips")]
[Authorize]
public class TipsController : ControllerBase
{
    private readonly ITipMatchingService matchingService;

    public TipsController(ITipMatchingService matchingService)
    {
        this.matchingService = matchingService;
    }

    [HttpGet("for-me")]
    public async Task<IActionResult> ForMe()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await matchingService.GetRelevantTipsAsync(userId));
    }
}