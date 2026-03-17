using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using WebAPI.Application.Exceptions;
using WebAPI.Models.Domain;

namespace WebAPI.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleException(httpContext, ex);
            }
        }

        private async Task HandleException(HttpContext httpContext, Exception exception)
        {

            _logger.LogError(
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
                _ => StatusCodes.Status500InternalServerError
            };
    }
}
