using DirectoryService.Contracts.Departments;
using FluentValidation;

namespace DirectoryService.Core.Departments;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Название департамента не может быть пустым")
            .MaximumLength(200).WithMessage("Название департамента слишком длинное");
        RuleFor(x => x.Slug).NotEmpty().WithMessage("Код департамента не может быть пустым")
            .MaximumLength(100).WithMessage("Код департамента слишком длинный");
    }
}
