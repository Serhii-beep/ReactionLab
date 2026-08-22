using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReactionLab.Domain.Common;

namespace ReactionLab.API.Http;

internal static class ApiProblems
{
    private const string TypeBase = "https://reactionlab.dev/errors/";
    private const string ErrorCodeKey = "errorCode";
    private const string TraceIdKey = "traceId";

    public static ProblemDetails Create(
        Error error,
        HttpContext httpContext)
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

        problem.Extensions[ErrorCodeKey] = error.Code;
        problem.Extensions[TraceIdKey] = GetTraceId(httpContext);

        if (error.Args is { Count: > 0 })
        {
            problem.Extensions["params"] = error.Args;
        }

        if (error.Field is { Length: > 0 } field)
        {
            problem.Extensions["errors"] = new Dictionary<string, FieldError[]>(StringComparer.Ordinal)
            {
                [ToCamel(field)] = [new FieldError(error.Code, error.Description) { Params = error.Args }]
            };
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

    public static (string Code, string Slug, string Title) DescribeStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => ("Request.Malformed", "validation", "The request could not be read"),
        StatusCodes.Status401Unauthorized => ("Request.Unauthorized", "unauthorized", "Authentication required"),
        StatusCodes.Status403Forbidden => ("Request.Forbidden", "forbidden", "Forbidden"),
        StatusCodes.Status404NotFound => ("Route.NotFound", "not-found", "Resource not found"),
        StatusCodes.Status405MethodNotAllowed => ("Request.MethodNotAllowed", "method-not-allowed", "Method not allowed"),
        StatusCodes.Status406NotAcceptable => ("Request.NotAcceptable", "not-acceptable", "Not acceptable"),
        StatusCodes.Status409Conflict => ("Request.Conflict", "conflict", "Conflict"),
        StatusCodes.Status413PayloadTooLarge => ("Request.TooLarge", "payload-too-large", "Request too large"),
        StatusCodes.Status415UnsupportedMediaType => ("Request.UnsupportedMediaType", "unsupported-media-type", "Unsupported media type"),
        StatusCodes.Status429TooManyRequests => ("Request.RateLimited", "rate-limited", "Too many requests"),
        _ when status >= StatusCodes.Status500InternalServerError => ("General.Unexpected", "unexpected", "Unexpected error"),
        _ => ("Request.Invalid", "bad-request", "Bad request")
    };

    public static ProblemDetails CreateForStatus(int status, HttpContext httpContext)
    {
        var (code, slug, title) = DescribeStatus(status);

        var problem = new ProblemDetails
        {
            Type = TypeBase + slug,
            Title = title,
            Status = status,
            Detail = title,
            Instance = httpContext.Request.Path
        };

        problem.Extensions[ErrorCodeKey] = code;
        problem.Extensions[TraceIdKey] = GetTraceId(httpContext);

        return problem;
    }

    public static void BringIntoContract(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;

        if (problem.Extensions.ContainsKey(ErrorCodeKey))
        {
            return;
        }

        var filled = CreateForStatus(problem.Status ?? StatusCodes.Status500InternalServerError, context.HttpContext);

        problem.Type = filled.Type;
        problem.Title = filled.Title;
        problem.Status = filled.Status;
        problem.Detail ??= filled.Detail;
        problem.Instance ??= filled.Instance;

        foreach (var extension in filled.Extensions)
        {
            problem.Extensions[extension.Key] = extension.Value;
        }
    }

    private static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;

    private static string ToCamel(string field) =>
        char.ToLowerInvariant(field[0]) + field[1..];
}
