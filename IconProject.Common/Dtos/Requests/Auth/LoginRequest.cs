using System.ComponentModel.DataAnnotations;

namespace IconProject.Common.Dtos.Requests.Auth;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }
}
