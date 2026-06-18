using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation.Validators;
using DirectoryService.Contracts.Locations;
using FluentValidation;

namespace DirectoryService.Core.Locations;

public class CreateLocationValidator : AbstractValidator<CreateLocationDto>
{
    public CreateLocationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Название локации не может быть пустым")
            .MaximumLength(200).WithMessage("Название локации слишком длинное");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Адрес локации не может быть пустым")
            .MaximumLength(500).WithMessage("Адрес локации слишком длинный");
    }
}