using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Shared;
using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.Locations;

public class CreateLocationValidator : AbstractValidator<CreateLocationDto>
{
    public CreateLocationValidator()
    {
//        RuleFor(x => x.Name).NotEmpty().WithError(GeneralErrors.ValueIsInvalid("Name"))
//            .MinimumLength(Location.NAME_MIN_LENGHT).WithMessage("Название локации слишком длинное")
//            .MaximumLength(Location.NAME_MAX_LENGHT).WithMessage("Название локации слишком длинное");
        RuleFor(x => x.Name)
            .MustBeValueObject(LocationName.Create);

        RuleFor(x => x.Address)
            .MustBeValueObject(Address.Create);
    }
}