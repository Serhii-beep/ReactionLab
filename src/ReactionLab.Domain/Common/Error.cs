namespace ReactionLab.Domain.Common;

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, object?>? Args = null)
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

    public Error WithArgs(params (string Key, object? Value)[] args) =>
        this with { Args = args.ToDictionary(arg => arg.Key, arg => arg.Value, StringComparer.Ordinal) };

    public bool Equals(Error? other) =>
        other is not null
        && Code == other.Code
        && Description == other.Description
        && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Code, Description, Type);
}
