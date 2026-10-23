using DirectoryService.Domain.shared;
using DirectoryService.Shared;
using DirectoryService.Core.Validation;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace DirectoryService.Core.Validation
{
    public static class ValidationExtensions
    {
        public static Error ToError(this ValidationResult validationResult)
        {
            Errors errors = ToErrors(validationResult).ToList();

            List<string> errorMessages = errors.Select(e => e.Message).ToList();
            string resultMessage = string.Join(", ", errorMessages);

            return Error.Validation(null, resultMessage);
        }

        public static Errors ToErrors(this ValidationResult validationResult)
        {
            List<ValidationFailure> validationErrors = validationResult.Errors;

            // Вытягиваем объекты Error напрямую из CustomState
            List<Error> errors = validationErrors
                .Select(failure => failure.CustomState as Error)
                .Where(error => error != null)
                .ToList();

            return errors;
        }

    }
}
