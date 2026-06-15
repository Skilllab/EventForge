using System.ComponentModel.DataAnnotations;

using EventBookingService.Domain.Exceptions;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Presentation.Middleware;

/// <summary>
/// Класс для отработки перехваченных <see cref="Exception"/>
/// </summary>
/// <param name="next">Делегат, обрабатывающий HTTP-запрос </param>
/// <param name="logger">Логгер</param>
public class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{

    /// <summary>
    /// Метод перехвата исключений
    /// </summary>
    /// <param name="httpContext">HTTP контекст</param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception exception)
    {

        logger.LogError(
            exception,
            "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var statusCode = MapStatusCode(exception);
        var error = exception switch
        {
            NotFoundException nfe => new ProblemDetails
            {
                Type = nfe.EntityName,
                Instance = nfe.EntityId,
                Status = statusCode,
                Detail = nfe.Message
            },
            ValidationCustomException ver => new ProblemDetails
            {
                Type = ver.EntityName,
                Instance = ver.EntityId,
                Status = statusCode,
                Detail = ver.Message
            },
            NoAvailableSeatsException nae => new ProblemDetails
            {
                Type = nae.EntityName,
                Instance = nae.EntityId,
                Status = statusCode,
                Detail = nae.Message
            },
            BookingPastEventException bpe => new ProblemDetails
            {
                Type = bpe.EntityName,
                Instance = bpe.EntityId,
                Status = statusCode,
                Detail = bpe.Message
            },
            BookingLimitExceededException ble => new ProblemDetails
            {
                Type = ble.EntityName,
                Instance = ble.EntityId,
                Status = statusCode,
                Detail = ble.Message
            },
            InsufficientPermissionsException ipe => new ProblemDetails
            {
                Type = ipe.EntityName,
                Instance = ipe.EntityId,
                Status = statusCode,
                Detail = ipe.Message
            },
            _ => new ProblemDetails()
            {
                Status = statusCode,
                Detail = exception.Message
            }
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(error);
    }

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationCustomException or ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InsufficientPermissionsException => StatusCodes.Status403Forbidden,
            BookingPastEventException => StatusCodes.Status422UnprocessableEntity,
            NoAvailableSeatsException or BookingLimitExceededException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
}
