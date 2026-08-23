namespace ReactionLab.Application.Common.Abstractions;

public interface ICatalogSearch
{
    IOrderedQueryable<TEntity> Matching<TEntity>(IQueryable<TEntity> source, string term)
        where TEntity : class;
}
