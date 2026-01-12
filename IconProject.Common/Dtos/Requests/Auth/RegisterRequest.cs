using System.ComponentModel.DataAnnotations;

namespace IconProject.Common.Dtos.Requests.Auth;

public sealed record RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    [StringLength(100)]
    public required string Password { get; init; }

    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }
}
