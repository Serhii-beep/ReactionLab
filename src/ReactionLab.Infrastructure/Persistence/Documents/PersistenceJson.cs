using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReactionLab.Domain.Common;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal static class PersistenceJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public static T Require<T>(Result<T> result, string what) =>
        result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Stored {what} is invalid: {result.Error.Code}.");
}
