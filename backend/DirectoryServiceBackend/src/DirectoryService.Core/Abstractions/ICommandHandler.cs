using CSharpFunctionalExtensions;
using DirectoryService.Core.Departments.DetachLocation;
using DirectoryService.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryService.Core.Abstractions;

public interface ICommandHandler<TResponce, in TCommand>
    where TCommand : ICommand
{
    public Task<Result<TResponce, Errors>> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
}
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    public Task<UnitResult<Errors>> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
}
