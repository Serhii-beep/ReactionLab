using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Common.Abstractions;

public interface IQueryHandler<in TQUery, TResponse>
{
    ValueTask<Result<TResponse>> HandleAsync(TQUery query, CancellationToken cancellationToken);
}
