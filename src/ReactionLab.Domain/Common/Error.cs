namespace ReactionLab.Domain.Common;

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static readonly Error NullValue = new(
        "General.NullValue",
        "A null value was provided where one was not permitted.",
        ErrorType.Validation);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorType.Unexpected);
}
