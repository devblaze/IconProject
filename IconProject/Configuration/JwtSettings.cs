namespace IconProject.Configuration;

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// The secret key used to sign JWT tokens.
    /// </summary>
    public required string SecretKey { get; init; }

    /// <summary>
    /// The issuer of the JWT token.
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// The audience for the JWT token.
    /// </summary>
    public required string Audience { get; init; }

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; init; } = 60;
}
