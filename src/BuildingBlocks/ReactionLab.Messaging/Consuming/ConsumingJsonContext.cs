using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactionLab.Messaging.Consuming;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(EventVersion))]
internal sealed partial class ConsumingJsonContext : JsonSerializerContext;
