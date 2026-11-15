using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactionLab.Identity.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(UserRegistered))]
[JsonSerializable(typeof(RegistrationRepeated))]
[JsonSerializable(typeof(EmailConfirmationRequested))]
[JsonSerializable(typeof(PasswordResetRequested))]
[JsonSerializable(typeof(PasswordChanged))]
[JsonSerializable(typeof(SignInMethodChanged))]
[JsonSerializable(typeof(AccountLocked))]
[JsonSerializable(typeof(PlatformAdminChangeAnnounced))]
[JsonSerializable(typeof(SchoolAdminChangeAnnounced))]
[JsonSerializable(typeof(AccountSetupRequested))]
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(UserErasureRequested))]
[JsonSerializable(typeof(UserDataErased))]
public sealed partial class IdentityEventJsonContext : JsonSerializerContext;
