using IconProject.Common.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IconProject.Extensions;

public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToErrorActionResult<T>(result.Error, path);
    }
    
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, int successStatusCode, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(result.Value) { StatusCode = successStatusCode };
        }

        return ToErrorActionResult<T>(result.Error, path);
    }
    
    public static ActionResult<T> ToCreatedResult<T>(this Result<T> result, string location, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new CreatedResult(location, result.Value);
        }

        return ToErrorActionResult<T>(result.Error, path);
    }
    
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
    
    public static IActionResult ToActionResult(this Result result, string? path = null)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToErrorActionResult(result.Error, path);
    }
    
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
    
    private static int GetStatusCodeFromError(Error error)
    {
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
