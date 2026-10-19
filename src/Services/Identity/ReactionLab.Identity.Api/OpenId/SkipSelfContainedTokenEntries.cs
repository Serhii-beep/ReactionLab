using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class SkipSelfContainedTokenEntries : IOpenIddictServerHandler<GenerateTokenContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<GenerateTokenContext>()
            .UseSingletonHandler<SkipSelfContainedTokenEntries>()
            .SetOrder(OpenIddictServerHandlers.Protection.CreateTokenEntry.Descriptor.Order - 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(GenerateTokenContext context)
    {
        if (context.TokenType is TokenTypeIdentifiers.AccessToken or TokenTypeIdentifiers.IdentityToken)
        {
            context.CreateTokenEntry = false;
        }

        return ValueTask.CompletedTask;
    }
}
