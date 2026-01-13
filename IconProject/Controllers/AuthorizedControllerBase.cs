using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace IconProject.Controllers;

public abstract class AuthorizedControllerBase : ControllerBase
{
    protected int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Invalid or missing user identifier claim.");
    }
}