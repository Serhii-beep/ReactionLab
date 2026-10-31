namespace ReactionLab.Identity.Api.Features.Members.ImportMembers;

internal sealed record MemberCsvRow(int RowNumber, string Username, string DisplayName, string Role, string? Email);
