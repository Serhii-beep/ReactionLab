namespace ReactionLab.Domain.Common;

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, object?>? Args = null,
    string? Field = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static readonly Error NullValue = new(
        "General.NullValue",
        "A null value was provided where one was not permitted.",
        ErrorType.Validation);

    public static Error Validation(string code, string description, string? field = null) =>
        new(code, description, ErrorType.Validation, Field: field);

    public static Error NotFound(string code, string description, string? field = null) =>
        new(code, description, ErrorType.NotFound, Field: field);

    public static Error Conflict(string code, string description, string? field = null) =>
        new(code, description, ErrorType.Conflict, Field: field);

    public static Error Forbidden(string code, string description, string? field = null) =>
        new(code, description, ErrorType.Forbidden, Field: field);

    public static Error Unexpected(string code, string description, string? field = null) =>
        new(code, description, ErrorType.Unexpected, Field: field);

    public Error WithArgs(params (string Key, object? Value)[] args) =>
        this with { Args = args.ToDictionary(arg => arg.Key, arg => arg.Value, StringComparer.Ordinal) };

    public bool Equals(Error? other) =>
        other is not null
        && Code == other.Code
        && Description == other.Description
        && Type == other.Type
        && Field == other.Field;

    public override int GetHashCode() => HashCode.Combine(Code, Description, Type, Field);
}
