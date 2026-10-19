namespace ReactionLab.Identity.Api.Http;

internal static class NoStoreHeaders
{
    public static TBuilder WithNoStore<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(SetNoStoreAsync);

    private static ValueTask<object?> SetNoStoreAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";

        return next(context);
    }
}
