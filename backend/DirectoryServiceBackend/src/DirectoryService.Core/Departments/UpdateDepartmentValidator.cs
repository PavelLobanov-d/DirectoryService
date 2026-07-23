using DirectoryService.Contracts.Departments;
using FluentValidation;

namespace DirectoryService.Core.Departments;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.NewName)
            .MaximumLength(200).WithMessage("Название департамента слишком длинное");
        RuleFor(x => x.NewSlug)
            .MaximumLength(100).WithMessage("Код департамента слишком длинный");
    }
}
