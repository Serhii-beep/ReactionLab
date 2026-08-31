using ReactionLab.Domain.Common;

namespace ReactionLab.API.Http;

internal static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : new ErrorResult(result.Error);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : new ErrorResult(result.Error);

    public static IResult ToCreatedResult<TValue>(this Result<TValue> result, Func<TValue, string> location) =>
        result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : new ErrorResult(result.Error);

    public static IResult ToRawJsonResult(this Result<string> result) =>
        result.IsSuccess
            ? Results.Text(result.Value, "application/json")
            : new ErrorResult(result.Error);
}
