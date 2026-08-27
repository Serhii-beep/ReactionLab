namespace ReactionLab.API.Http;

internal static class LocaleHeaders
{
    public static RouteGroupBuilder WithLocaleHeaders(this RouteGroupBuilder group) =>
        group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            httpContext.Response.OnStarting(() =>
            {
                httpContext.Response.Headers.ContentLanguage = httpContext.ResolveLocale().Code;
                httpContext.Response.Headers.Append("Vary", "Accept-Language");
                return Task.CompletedTask;
            });

            return await next(context);
        });
}
