using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Shared;
using FluentValidation;

namespace DirectoryService.Core.Departments;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValueObject(DepartmentName.Create);

        RuleFor(x => x.Slug)
            .MustBeValueObject(Slug.Create);
    }
}
