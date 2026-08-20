using ReactionLab.Domain.Common;

namespace ReactionLab.API.Http;

internal sealed class ErrorResult(Error error) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext) =>
        Results.Problem(ApiProblems.Create(error, httpContext)).ExecuteAsync(httpContext);
}
