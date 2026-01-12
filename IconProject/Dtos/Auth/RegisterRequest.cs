using System.ComponentModel.DataAnnotations;

namespace IconProject.Dtos.Auth;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required]
    [MinLength(6)]
    [StringLength(100)]
    public required string Password { get; init; }

    /// <summary>
    /// The user's first name (optional).
    /// </summary>
    [StringLength(100)]
    public string? FirstName { get; init; }

    /// <summary>
    /// The user's last name (optional).
    /// </summary>
    [StringLength(100)]
    public string? LastName { get; init; }
}
