using System.Net;
using FeedbackSorter.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackSorter.Presentation.Middleware;

public class DomainValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainValidationExceptionMiddleware> _logger;

    public DomainValidationExceptionMiddleware(RequestDelegate next, ILogger<DomainValidationExceptionMiddleware> logger)
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
        catch (DomainValidationException ex)
        {
            _logger.LogError(ex, "A domain validation exception occurred: {Message}", ex.Message);
            httpContext.Response.ContentType = "application/problem+json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var problemDetails = new ProblemDetails
            {
                Status = httpContext.Response.StatusCode,
                Title = "Validation Error",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            httpContext.Response.ContentType = "application/problem+json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = httpContext.Response.StatusCode,
                Title = "An unexpected error occurred.",
                Detail = "An internal server error has occurred.",
                Type = "https://tools.ietf.org/html/rfc7807"
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
