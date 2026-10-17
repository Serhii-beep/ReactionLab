using System.Text.Json.Serialization;

namespace ReactionLab.Web;

internal sealed record FieldError(string Code, string Message)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Params { get; init; }
}
