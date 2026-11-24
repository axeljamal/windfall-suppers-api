using Microsoft.AspNetCore.Diagnostics;
using WindfallSuppers.Api.Domain;

namespace WindfallSuppers.Api;

/// <summary>
/// The one place that turns broken business rules into HTTP responses
/// (RFC 9457 problem details), so endpoints stay short and consistent.
/// </summary>
public class ProblemDetailsExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            DomainException { Error: DomainError.NotFound } => (StatusCodes.Status404NotFound, "Not found"),
            DomainException { Error: DomainError.Conflict } => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorised"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (0, ""),
        };
        if (status == 0) return false; // let the default handler log it and return 500

        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
