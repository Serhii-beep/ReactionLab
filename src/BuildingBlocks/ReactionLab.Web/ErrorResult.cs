using Microsoft.AspNetCore.Http;
using ReactionLab.SharedKernel;

namespace ReactionLab.Web;

internal sealed class ErrorResult(Error error) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext) =>
        Results.Problem(ApiProblems.Create(error, httpContext)).ExecuteAsync(httpContext);
}
