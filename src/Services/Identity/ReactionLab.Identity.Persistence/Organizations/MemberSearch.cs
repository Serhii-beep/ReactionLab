namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record MemberSearch(string? Text, string? Role, string? Status)
{
    public const int MaximumTextLength = 64;
}
