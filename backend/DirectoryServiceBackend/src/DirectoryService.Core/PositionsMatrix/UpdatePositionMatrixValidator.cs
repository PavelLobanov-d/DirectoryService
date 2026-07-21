using DirectoryService.Contracts.PositionsMatrix;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.PositionsMatrix;

public class UpdatePositionMatrixValidator : AbstractValidator<UpdatePositionMatrixDto>
{
    public UpdatePositionMatrixValidator()
    {
        RuleFor(x => x.NewName)
            .MaximumLength(200).WithMessage("Название должности слишком длинное");
        RuleFor(x => x.NewSlug)
            .MaximumLength(100).WithMessage("Код должности слишком длинный");
    }
}
