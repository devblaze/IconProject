using System.ComponentModel.DataAnnotations;

namespace IconProject.Dtos.Auth;

/// <summary>
/// Request DTO for user login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required]
    [MinLength(6)]
    public required string Password { get; init; }
}
