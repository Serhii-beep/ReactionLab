namespace ReactionLab.Domain.Localization;

public interface ITranslatableContent<TSelf>
    where TSelf : ITranslatableContent<TSelf>
{
    TSelf WithFallback(TSelf fallback);
}
