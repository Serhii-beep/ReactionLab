using OpenIddict.Client;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Client.OpenIddictClientEvents;
using static OpenIddict.Client.OpenIddictClientHandlerFilters;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed class SkipIssuerCheckOnErrorResponse : IOpenIddictClientHandler<ProcessAuthenticationContext>
{
    public static OpenIddictClientHandlerDescriptor Descriptor { get; } =
        OpenIddictClientHandlerDescriptor.CreateBuilder<ProcessAuthenticationContext>()
            .AddFilter<RequireRedirectionRequest>()
            .UseSingletonHandler<SkipIssuerCheckOnErrorResponse>()
            .SetOrder(OpenIddictClientHandlers.ValidateIssuerParameter.Descriptor.Order - 1)
            .SetType(OpenIddictClientHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        if (!string.IsNullOrEmpty((string?)context.Request[Parameters.Error])
            && string.IsNullOrEmpty((string?)context.Request[Parameters.Iss]))
        {
            context.DisableIssuerParameterValidation = true;
        }

        return ValueTask.CompletedTask;
    }
}
