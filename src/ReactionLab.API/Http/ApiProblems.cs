using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReactionLab.Domain.Common;

namespace ReactionLab.API.Http;

internal static class ApiProblems
{
    private const string TypeBase = "https://reactionlab.dev/errors/";

    public static ProblemDetails Create(
        Error error,
        HttpContext httpContext,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null)
    {
        var (status, title, slug) = Describe(error.Type);

        var problem = new ProblemDetails
        {
            Type = TypeBase + slug,
            Title = title,
            Status = status,
            Detail = error.Description,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["errorCode"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (error.Args is { Count: > 0 })
        {
            problem.Extensions["params"] = error.Args;
        }

        if (fieldErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = fieldErrors;
        }

        return problem;
    }

    public static (int Status, string Title, string Slug) Describe(ErrorType type) => type switch
    {
        ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed", "validation"),
        ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found", "not-found"),
        ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict", "conflict"),
        ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden", "forbidden"),
        ErrorType.Unexpected => (StatusCodes.Status500InternalServerError, "Unexpected error", "unexpected"),
        ErrorType.None => (StatusCodes.Status500InternalServerError, "Unexpected error", "unexpected"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "unexpected")
    };
}
