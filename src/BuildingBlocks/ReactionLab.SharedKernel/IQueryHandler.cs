namespace ReactionLab.SharedKernel;

public interface IQueryHandler<in TQUery, TResponse>
{
    ValueTask<Result<TResponse>> HandleAsync(TQUery query, CancellationToken cancellationToken);
}
