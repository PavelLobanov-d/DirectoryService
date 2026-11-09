using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.FindChildById;

public class PositionMatrixFindChildByIdHandler : ICommandHandler<PositionMatrix?, PositionMatrixFindChildByIdCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;

    public PositionMatrixFindChildByIdHandler(IPositionMatrixRepository positionMatrixRepository)
    {
        _positionMatrixRepository = positionMatrixRepository;
    }

    public async Task<Result<PositionMatrix?, Errors>> ExecuteAsync(
        PositionMatrixFindChildByIdCommand command,
        CancellationToken cancellationToken = default)
    {
        PositionMatrix? childFound = null;
        var resultChilds = await _positionMatrixRepository.GetByParentIdAsync(command.PositionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsSuccess)
        {
            foreach (PositionMatrix child in resultChilds.Value)
            {
                if (child.Id == command.FindPositionId)
                {
                    childFound = child;
                    break;
                }
                else
                {
                    var resultFind = await ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
                    if (resultFind.IsSuccess && resultFind.Value != null)
                    {
                        childFound = resultFind.Value;
                        break;
                    }
                }
            }
        }
        else
        {
            return resultChilds.Error.ToErrors();
        }

        return childFound;
    }
}
