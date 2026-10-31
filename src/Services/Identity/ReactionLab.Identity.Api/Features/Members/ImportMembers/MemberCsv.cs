using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.ImportMembers;

internal static class MemberCsv
{
    public const int MaximumRows = 100;
    public const int MaximumLength = 32 * 1024;

    private const string UsernameColumn = "username";
    private const string DisplayNameColumn = "display_name";
    private const string RoleColumn = "role";
    private const string EmailColumn = "email";

    private static readonly string[] RequiredColumns = [UsernameColumn, DisplayNameColumn, RoleColumn];

    private static readonly CsvConfiguration SpreadsheetExports = new(CultureInfo.InvariantCulture)
    {
        DetectDelimiter = true,
        DetectDelimiterValues = [",", ";", "\t"],
        TrimOptions = TrimOptions.Trim,
        ExceptionMessagesContainRawData = false,
    };

    public static Result<IReadOnlyList<MemberCsvRow>> Parse(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return MemberImportErrors.CsvRequired;
        }

        if (csv.Length > MaximumLength)
        {
            return MemberImportErrors.CsvTooLarge;
        }

        var records = ReadRecords(csv);

        if (records.IsFailure)
        {
            return records.Error;
        }

        return ReadRows(records.Value);
    }

    private static Result<IReadOnlyList<string[]>> ReadRecords(string csv)
    {
        var records = new List<string[]>();
        using var reader = new StringReader(csv.TrimStart('\uFEFF'));
        using var parser = new CsvParser(reader, SpreadsheetExports);

        try
        {
            while (parser.Read())
            {
                if (parser.Record is { } fields && !fields.All(string.IsNullOrWhiteSpace))
                {
                    records.Add(fields);
                }
            }
        }
        catch (BadDataException)
        {
            return records.Count == 0
                ? MemberImportErrors.CsvMalformed
                : MemberImportErrors.CsvMalformed with { Field = $"rows[{records.Count}]" };
        }

        return records;
    }

    private static Result<IReadOnlyList<MemberCsvRow>> ReadRows(IReadOnlyList<string[]> records)
    {
        if (records.Count == 0)
        {
            return MemberImportErrors.CsvRequired;
        }

        var header = records[0].Select(column => column.Trim().ToLowerInvariant()).ToList();
        var namedColumns = header.Where(column => column.Length > 0).ToList();

        if (!RequiredColumns.All(namedColumns.Contains)
            || namedColumns.Any(column => !RequiredColumns.Contains(column) && column != EmailColumn)
            || namedColumns.Distinct().Count() != namedColumns.Count)
        {
            return MemberImportErrors.HeaderInvalid;
        }

        if (records.Count == 1)
        {
            return MemberImportErrors.RowsRequired;
        }

        if (records.Count - 1 > MaximumRows)
        {
            return MemberImportErrors.TooManyRows;
        }

        var unnamedPositions = Enumerable.Range(0, header.Count)
            .Where(position => header[position].Length == 0)
            .ToList();
        var rows = new List<MemberCsvRow>();

        for (var index = 1; index < records.Count; index++)
        {
            var fields = records[index];

            if (fields.Length != header.Count)
            {
                return MemberImportErrors.RowMalformed with { Field = $"rows[{index}]" };
            }

            if (unnamedPositions.Any(position => !string.IsNullOrWhiteSpace(fields[position])))
            {
                return MemberImportErrors.ColumnUnnamed with { Field = $"rows[{index}]" };
            }

            string? Field(string column) => header.IndexOf(column) is var position and >= 0
                ? fields[position].Trim()
                : null;

            rows.Add(new MemberCsvRow(
                index,
                Field(UsernameColumn)!,
                Field(DisplayNameColumn)!,
                Field(RoleColumn)!,
                Field(EmailColumn) is { Length: > 0 } email ? email : null));
        }

        return rows;
    }
}
