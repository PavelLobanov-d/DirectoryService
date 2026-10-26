using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.PositionsMatrix;
using FluentValidation;

namespace DirectoryService.Core.PositionsMatrix;

public class UpdatePositionMatrixValidator : AbstractValidator<UpdatePositionMatrixDto>
{
    public UpdatePositionMatrixValidator()
    {
    //    RuleFor(x => x.NewName)
    //        .MaximumLength(200).WithMessage("Название должности слишком длинное");
    //    RuleFor(x => x.NewSlug)
    //        .MaximumLength(100).WithMessage("Код должности слишком длинный");

        RuleFor(x => x.NewName)
            .MustBeValueObject(PositionName.Create);

        RuleFor(x => x.NewSlug)
            .MustBeValueObject(Slug.Create);
    }
}
