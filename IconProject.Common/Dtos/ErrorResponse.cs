using System.Net;

namespace IconProject.Common.Dtos;

public sealed record ErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public int StatusCode { get; init; }
    public string? Path { get; init; }
    public string? Details { get; init; }

    public static ErrorResponse NotFound(string message, string? path = null) => new()
    {
        Code = "Resource.NotFound",
        Message = message,
        StatusCode = (int)HttpStatusCode.NotFound,
        Path = path
    };

    public static ErrorResponse BadRequest(string message, string? path = null) => new()
    {
        Code = "Request.Invalid",
        Message = message,
        StatusCode = (int)HttpStatusCode.BadRequest,
        Path = path
    };

    public static ErrorResponse Unauthorized(string message = "Unauthorized access.", string? path = null) => new()
    {
        Code = "Auth.Unauthorized",
        Message = message,
        StatusCode = (int)HttpStatusCode.Unauthorized,
        Path = path
    };

    public static ErrorResponse Forbidden(string message = "Access forbidden.", string? path = null) => new()
    {
        Code = "Auth.Forbidden",
        Message = message,
        StatusCode = (int)HttpStatusCode.Forbidden,
        Path = path
    };

    public static ErrorResponse InternalServerError(
        string message = "An unexpected error occurred.",
        string? path = null) => new()
    {
        Code = "Server.InternalError",
        Message = message,
        StatusCode = (int)HttpStatusCode.InternalServerError,
        Path = path
    };

    public static ErrorResponse FromError(Error error, int statusCode, string? path = null) => new()
    {
        Code = error.Code,
        Message = error.Description,
        StatusCode = statusCode,
        Path = path
    };
}
