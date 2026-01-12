using System.Text.Json.Serialization;

namespace IconProject.Dtos;

/// <summary>
/// Represents a standardized error response returned by the API.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// Gets or sets the error code identifying the type of error.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of the error.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the request path that caused the error.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    /// <summary>
    /// Gets or sets additional details about the error (only in development).
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; init; }

    /// <summary>
    /// Gets or sets validation errors if applicable.
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Creates an ErrorResponse from a Result Error.
    /// </summary>
    public static ErrorResponse FromError(Error error, int statusCode, string? path = null)
    {
        return new ErrorResponse
        {
            Code = error.Code,
            Message = error.Description,
            StatusCode = statusCode,
            Path = path
        };
    }

    /// <summary>
    /// Creates an ErrorResponse for validation errors.
    /// </summary>
    public static ErrorResponse ValidationError(IDictionary<string, string[]> errors, string? path = null)
    {
        return new ErrorResponse
        {
            Code = "Validation.Error",
            Message = "One or more validation errors occurred.",
            StatusCode = 400,
            Path = path,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates an ErrorResponse for internal server errors.
    /// </summary>
    public static ErrorResponse InternalServerError(string? details = null, string? path = null)
    {
        return new ErrorResponse
        {
            Code = "Server.InternalError",
            Message = "An unexpected error occurred. Please try again later.",
            StatusCode = 500,
            Path = path,
            Details = details
        };
    }

    /// <summary>
    /// Creates an ErrorResponse for not found errors.
    /// </summary>
    public static ErrorResponse NotFound(string message, string? path = null)
    {
        return new ErrorResponse
        {
            Code = "Resource.NotFound",
            Message = message,
            StatusCode = 404,
            Path = path
        };
    }

    /// <summary>
    /// Creates an ErrorResponse for unauthorized errors.
    /// </summary>
    public static ErrorResponse Unauthorized(string message = "You are not authorized to access this resource.", string? path = null)
    {
        return new ErrorResponse
        {
            Code = "Auth.Unauthorized",
            Message = message,
            StatusCode = 401,
            Path = path
        };
    }

    /// <summary>
    /// Creates an ErrorResponse for forbidden errors.
    /// </summary>
    public static ErrorResponse Forbidden(string message = "You do not have permission to perform this action.", string? path = null)
    {
        return new ErrorResponse
        {
            Code = "Auth.Forbidden",
            Message = message,
            StatusCode = 403,
            Path = path
        };
    }
}
