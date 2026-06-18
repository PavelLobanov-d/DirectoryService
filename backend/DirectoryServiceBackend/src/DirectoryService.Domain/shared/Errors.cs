using System;
using System.Collections;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.shared;

public class Errors : IEnumerable<Error>
{
    private readonly List<Error> _errors = [];
    public Errors(IEnumerable<Error> errors)
    {
        _errors = [.. errors];
    }
    public Errors(ValidationResult validationResult)
    {
        foreach (var failure in validationResult.Errors)
        {
            _errors.Add(failure);
        }
    }
    public static implicit operator Errors(List<Error> errors) => new(errors);
    public static implicit operator Errors(Error[] errors) => new(errors);
    public static implicit operator Errors(Error error) => new([error]);


    public IEnumerator<Error> GetEnumerator()
    {
        return _errors.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
