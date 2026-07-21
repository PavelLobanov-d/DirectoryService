using DirectoryService.Contracts.PositionsMatrix;
using FluentValidation;

namespace DirectoryService.Core.PositionsMatrix;

public class CreatePositionMatrixValidator : AbstractValidator<CreatePositionMatrixDto>
{
    public CreatePositionMatrixValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Название должности не может быть пустым")
            .MaximumLength(200).WithMessage("Название должности слишком длинное");
        RuleFor(x => x.Slug).NotEmpty().WithMessage("Код должности не может быть пустым")
            .MaximumLength(100).WithMessage("Код должности слишком длинный");
    }
}
