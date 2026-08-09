namespace ReactionLab.Domain.Common;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unexpected = 5
}
