using ReactionLab.Identity.Contracts;

namespace ReactionLab.Identity.Persistence.OpenId;

public static class OpenIdNames
{
    public const string GatewayClientId = "gateway";
    public const string NotificationsClientId = "notifications";
    public const string ApiScope = "api";
    public const string ContactsScope = IdentityScopes.Contacts;
    public const string SecurityLinksScope = IdentityScopes.SecurityLinks;
    public const string IdentityAudience = "identity";
    public const string CatalogAudience = "catalog";
}
