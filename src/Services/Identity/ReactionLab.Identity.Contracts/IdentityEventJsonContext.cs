using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactionLab.Identity.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(UserRegistered))]
[JsonSerializable(typeof(RegistrationRepeated))]
[JsonSerializable(typeof(EmailConfirmationRequested))]
[JsonSerializable(typeof(PasswordResetRequested))]
[JsonSerializable(typeof(PasswordChanged))]
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(UserErasureRequested))]
public sealed partial class IdentityEventJsonContext : JsonSerializerContext;
