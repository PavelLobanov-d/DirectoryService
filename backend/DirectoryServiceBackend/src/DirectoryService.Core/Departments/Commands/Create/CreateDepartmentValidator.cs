using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using FluentValidation;

namespace DirectoryService.Core.Departments.Commands.Create;

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
