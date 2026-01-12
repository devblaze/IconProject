using IconProject.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IconProject.Extensions;

/// <summary>
/// Extension methods for converting Result types to ActionResult responses.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result&lt;T&gt; to an ActionResult.
    /// Returns 200 OK with the value on success, or an appropriate error response on failure.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToErrorActionResult<T>(result.Error, path);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to an ActionResult with a custom success status code.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, int successStatusCode, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(result.Value) { StatusCode = successStatusCode };
        }

        return ToErrorActionResult<T>(result.Error, path);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to a Created ActionResult (201).
    /// </summary>
    public static ActionResult<T> ToCreatedResult<T>(this Result<T> result, string location, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new CreatedResult(location, result.Value);
        }

        return ToErrorActionResult<T>(result.Error, path);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to a CreatedAtAction ActionResult (201).
    /// </summary>
    public static ActionResult<T> ToCreatedAtActionResult<T>(
        this Result<T> result,
        string actionName,
        string controllerName,
        object routeValues,
        string? path = null)
    {
        if (result.IsSuccess)
        {
            return new CreatedAtActionResult(actionName, controllerName, routeValues, result.Value);
        }

        return ToErrorActionResult<T>(result.Error, path);
    }

    /// <summary>
    /// Converts a Result (non-generic) to an ActionResult.
    /// Returns 204 No Content on success, or an appropriate error response on failure.
    /// </summary>
    public static IActionResult ToActionResult(this Result result, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToErrorActionResult(result.Error, path);
    }

    /// <summary>
    /// Converts a Result (non-generic) to an ActionResult with a custom success status code.
    /// </summary>
    public static IActionResult ToActionResult(this Result result, int successStatusCode, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new StatusCodeResult(successStatusCode);
        }

        return ToErrorActionResult(result.Error, path);
    }

    private static ActionResult<T> ToErrorActionResult<T>(Error error, string? path)
    {
        var statusCode = GetStatusCodeFromError(error);
        var errorResponse = ErrorResponse.FromError(error, statusCode, path);

        return new ObjectResult(errorResponse) { StatusCode = statusCode };
    }

    private static IActionResult ToErrorActionResult(Error error, string? path)
    {
        var statusCode = GetStatusCodeFromError(error);
        var errorResponse = ErrorResponse.FromError(error, statusCode, path);

        return new ObjectResult(errorResponse) { StatusCode = statusCode };
    }

    /// <summary>
    /// Maps error codes to HTTP status codes.
    /// </summary>
    private static int GetStatusCodeFromError(Error error)
    {
        // Map based on error code prefix
        return error.Code.Split('.')[0] switch
        {
            "Validation" => StatusCodes.Status400BadRequest,
            "Unauthorized" or "Auth" => StatusCodes.Status401Unauthorized,
            "Forbidden" => StatusCodes.Status403Forbidden,
            var code when code.EndsWith("NotFound") => StatusCodes.Status404NotFound,
            "Conflict" => StatusCodes.Status409Conflict,
            _ => error.Code switch
            {
                // Specific error codes
                var c when c.Contains("NotFound") => StatusCodes.Status404NotFound,
                var c when c.Contains("AlreadyExists") => StatusCodes.Status409Conflict,
                var c when c.Contains("InvalidCredentials") => StatusCodes.Status401Unauthorized,
                var c when c.Contains("NotOwned") => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            }
        };
    }
}

/// <summary>
/// Extension methods for async Result operations.
/// </summary>
public static class ResultAsyncExtensions
{
    /// <summary>
    /// Converts an async Result&lt;T&gt; to an ActionResult.
    /// </summary>
    public static async Task<ActionResult<T>> ToActionResultAsync<T>(this Task<Result<T>> resultTask, string? path = null)
    {
        var result = await resultTask;
        return result.ToActionResult(path);
    }

    /// <summary>
    /// Converts an async Result to an ActionResult.
    /// </summary>
    public static async Task<IActionResult> ToActionResultAsync(this Task<Result> resultTask, string? path = null)
    {
        var result = await resultTask;
        return result.ToActionResult(path);
    }

    /// <summary>
    /// Converts an async Result&lt;T&gt; to a Created ActionResult.
    /// </summary>
    public static async Task<ActionResult<T>> ToCreatedResultAsync<T>(
        this Task<Result<T>> resultTask,
        string location,
        string? path = null)
    {
        var result = await resultTask;
        return result.ToCreatedResult(location, path);
    }
}
