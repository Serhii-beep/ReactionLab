using Microsoft.AspNetCore.Diagnostics;
using ReactionLab.Domain.Common;

namespace ReactionLab.API.Http;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception handling {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var error = Error.Unexpected(
            "General.Unexpected",
            "An unexpected error occurred.");

        var problem = ApiProblems.Create(error, httpContext);
        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
