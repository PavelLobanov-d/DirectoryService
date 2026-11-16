using CSharpFunctionalExtensions;
using DirectoryService.Shared;

namespace DirectoryService.Core.Abstractions;

public interface ICommandHandler<TResponse, in TCommand>
    where TCommand : ICommand
{
    public Task<Result<TResponse, Errors>> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
}
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    public Task<UnitResult<Errors>> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
}
