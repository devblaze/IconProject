using System.Net;
using System.Text.Json;
using IconProject.Dtos;
using Microsoft.EntityFrameworkCore;

namespace IconProject.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning consistent error responses.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var path = context.Request.Path;
        var (statusCode, errorResponse) = MapException(exception, path);

        // Log the exception
        LogException(exception, statusCode, path);

        // Include stack trace in development
        if (_environment.IsDevelopment() && statusCode == (int)HttpStatusCode.InternalServerError)
        {
            errorResponse = errorResponse with { Details = exception.ToString() };
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private (int StatusCode, ErrorResponse Response) MapException(Exception exception, string path)
    {
        return exception switch
        {
            // Validation exceptions
            ArgumentException argEx => (
                (int)HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    Code = "Validation.ArgumentError",
                    Message = argEx.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Path = path
                }),

            // Not found exceptions
            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                ErrorResponse.NotFound("The requested resource was not found.", path)),

            // Entity Framework exceptions
            DbUpdateConcurrencyException => (
                (int)HttpStatusCode.Conflict,
                new ErrorResponse
                {
                    Code = "Database.ConcurrencyConflict",
                    Message = "The record was modified by another user. Please refresh and try again.",
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Path = path
                }),

            DbUpdateException dbEx => HandleDbUpdateException(dbEx, path),

            // Unauthorized access
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                ErrorResponse.Unauthorized(path: path)),

            // Operation canceled (typically client disconnected)
            OperationCanceledException => (
                499, // Client Closed Request
                new ErrorResponse
                {
                    Code = "Request.Cancelled",
                    Message = "The request was cancelled.",
                    StatusCode = 499,
                    Path = path
                }),

            // Invalid operation
            InvalidOperationException invalidOpEx => (
                (int)HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    Code = "Operation.Invalid",
                    Message = invalidOpEx.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Path = path
                }),

            // Default - Internal server error
            _ => (
                (int)HttpStatusCode.InternalServerError,
                ErrorResponse.InternalServerError(path: path))
        };
    }

    private (int StatusCode, ErrorResponse Response) HandleDbUpdateException(DbUpdateException exception, string path)
    {
        var innerMessage = exception.InnerException?.Message ?? exception.Message;

        // Check for unique constraint violation
        if (innerMessage.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("IX_", StringComparison.OrdinalIgnoreCase))
        {
            return (
                (int)HttpStatusCode.Conflict,
                new ErrorResponse
                {
                    Code = "Database.UniqueConstraintViolation",
                    Message = "A record with the same unique value already exists.",
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Path = path
                });
        }

        // Check for foreign key violation
        if (innerMessage.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("FK_", StringComparison.OrdinalIgnoreCase))
        {
            return (
                (int)HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    Code = "Database.ForeignKeyViolation",
                    Message = "The operation violates a foreign key constraint. Ensure referenced records exist.",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Path = path
                });
        }

        return (
            (int)HttpStatusCode.InternalServerError,
            new ErrorResponse
            {
                Code = "Database.UpdateError",
                Message = "An error occurred while updating the database.",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Path = path
            });
    }

    private void LogException(Exception exception, int statusCode, string path)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            exception,
            "Exception occurred while processing request {Path}. Status: {StatusCode}",
            path,
            statusCode);
    }
}

/// <summary>
/// Extension methods for registering the GlobalExceptionMiddleware.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handling middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
