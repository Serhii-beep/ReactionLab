using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        var problem = Describe(httpContext, exception);

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private ProblemDetails Describe(HttpContext httpContext, Exception exception)
    {
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogInformation(
                "Unreadable request on {Method} {Path}: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                badRequest.Message);

            return ApiProblems.CreateForStatus(badRequest.StatusCode, httpContext);
        }

        logger.LogError(
            exception,
            "Unhandled exception handling {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        return ApiProblems.Create(
            Error.Unexpected(
                "General.Unexpected",
                "An unexpected error occurred."),
            httpContext);
    }
}
