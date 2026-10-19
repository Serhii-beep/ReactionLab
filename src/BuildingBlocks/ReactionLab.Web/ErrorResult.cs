using Microsoft.AspNetCore.Http;
using ReactionLab.SharedKernel;

namespace ReactionLab.Web;

internal sealed class ErrorResult(Error error) : IResult
{
    private const string InvalidTokenChallenge = "Bearer error=\"invalid_token\"";

    public Task ExecuteAsync(HttpContext httpContext)
    {
        if (error.Type == ErrorType.Unauthorized)
        {
            httpContext.Response.Headers.WWWAuthenticate = InvalidTokenChallenge;
        }

        return Results.Problem(ApiProblems.Create(error, httpContext)).ExecuteAsync(httpContext);
    }
}
