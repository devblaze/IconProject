namespace IconProject.Dtos.Auth;

/// <summary>
/// Response DTO containing authentication tokens and user information.
/// </summary>
public sealed record AuthResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// The authenticated user's information.
    /// </summary>
    public required UserInfo User { get; init; }
}

/// <summary>
/// Basic user information included in auth responses.
/// </summary>
public sealed record UserInfo
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
