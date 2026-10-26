using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Core.Validation;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.Locations;

public class UpdateLocationValidator : AbstractValidator<UpdateLocationDto>
{
    public UpdateLocationValidator()
    {
        //RuleFor(x => x.NewName)
        //    .MaximumLength(200).WithMessage("Название локации слишком длинное");
        //RuleFor(x => x.NewAddress)
        //    .MaximumLength(500).WithMessage("Адрес локации слишком длинный");
        RuleFor(x => x.NewName)
            .MustBeValueObject(LocationName.Create);

        RuleFor(x => x.NewAddress)
            .MustBeValueObject(Address.Create);

    }
}
