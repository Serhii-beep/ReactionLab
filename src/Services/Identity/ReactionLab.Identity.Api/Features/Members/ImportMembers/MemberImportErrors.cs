using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.ImportMembers;

internal static class MemberImportErrors
{
    public static readonly Error SchoolsOnly = Error.Validation(
        "MemberImport.SchoolsOnly",
        "Accounts are imported at a school; a district creates its accounts one at a time.");

    public static readonly Error CsvRequired = Error.Validation(
        "MemberImport.CsvRequired",
        "A CSV is required.",
        CsvField);

    public static readonly Error CsvTooLarge = Error.Validation(
        "MemberImport.CsvTooLarge",
        $"A CSV must not exceed {MemberCsv.MaximumLength} characters.",
        CsvField)
        .WithArgs(("max", MemberCsv.MaximumLength));

    public static readonly Error CsvMalformed = Error.Validation(
        "MemberImport.CsvMalformed",
        "A quote is out of place: a field holding quotes or separators is enclosed in quotes, its own quotes doubled.",
        CsvField);

    public static readonly Error HeaderInvalid = Error.Validation(
        "MemberImport.HeaderInvalid",
        "The first line names the columns: username, display_name and role, and optionally email, each once.",
        CsvField);

    public static readonly Error RowsRequired = Error.Validation(
        "MemberImport.RowsRequired",
        "The CSV has no accounts after its header.",
        CsvField);

    public static readonly Error TooManyRows = Error.Validation(
        "MemberImport.TooManyRows",
        $"A CSV holds at most {MemberCsv.MaximumRows} accounts.",
        CsvField)
        .WithArgs(("max", MemberCsv.MaximumRows));

    public static readonly Error RowMalformed = Error.Validation(
        "MemberImport.RowMalformed",
        "This row has a different number of fields than the header.");

    public static readonly Error ColumnUnnamed = Error.Validation(
        "MemberImport.ColumnUnnamed",
        "This row has a value in a column the first line doesn't name.");

    public static readonly Error UsernameRepeated = Error.Validation(
        "MemberImport.UsernameRepeated",
        "Another row of the CSV has this username.");

    public static readonly Error EmailRepeated = Error.Validation(
        "MemberImport.EmailRepeated",
        "Another row of the CSV has this email address.");

    private const string CsvField = "Csv";
}
