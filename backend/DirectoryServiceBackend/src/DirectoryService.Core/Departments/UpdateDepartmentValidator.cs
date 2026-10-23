using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Shared;
using FluentValidation;

namespace DirectoryService.Core.Departments;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentValidator()
    {
        //RuleFor(x => x.NewName)
        //    .MaximumLength(200).WithMessage("Название департамента слишком длинное");
        RuleFor(x => x.NewName)
            .MustBeValueObject(DepartmentName.Create);

        RuleFor(x => x.NewSlug)
            .MustBeValueObject(Slug.Create);
    }
}
