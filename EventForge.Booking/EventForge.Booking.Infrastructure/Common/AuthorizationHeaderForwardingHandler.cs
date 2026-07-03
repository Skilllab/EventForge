using Microsoft.AspNetCore.Http;

namespace EventForge.Booking.Infrastructure.Common;

/// <summary>
/// Прокидывает входящий Authorization header
/// в исходящий запрос к внутреннему микросервису.
/// </summary>
/// <param name="httpContextAccessor">Доступ к текущему HTTP-контексту.</param>
public class AuthorizationHeaderForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
