using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Common.Abstractions;

public interface ICommandHandler<in TCommand, TResponse>
{
    ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
{
    ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
