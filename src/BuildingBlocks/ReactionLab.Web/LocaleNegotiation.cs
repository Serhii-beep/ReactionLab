using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using ReactionLab.SharedKernel;

namespace ReactionLab.Web;

public static class LocaleNegotiation
{
    public static SupportedLocale ResolveLocale(this HttpContext httpContext)
    {
        if (!StringWithQualityHeaderValue.TryParseList(httpContext.Request.Headers.AcceptLanguage, out var accepted))
        {
            return SupportedLocale.Default;
        }

        var ordered = accepted
            .Where(language => language.Quality != 0)
            .OrderByDescending(language => language.Quality ?? 1.0);

        foreach (var language in ordered)
        {
            if (SupportedLocale.Match(language.Value.Value) is { } supported)
            {
                return supported;
            }
        }

        return SupportedLocale.Default;
    }
}
