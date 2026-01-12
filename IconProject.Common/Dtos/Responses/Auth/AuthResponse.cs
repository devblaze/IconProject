namespace IconProject.Common.Dtos.Responses.Auth;

public sealed record AuthResponse
{
    public required string AccessToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public required UserInfo User { get; init; }
}

public sealed record UserInfo
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
