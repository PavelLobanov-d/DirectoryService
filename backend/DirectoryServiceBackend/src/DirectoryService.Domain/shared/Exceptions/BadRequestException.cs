using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DirectoryService.Domain.shared.Exceptions;

public class BadRequestException : Exception
{
    //public BadRequestException(IEnumerable<string> messages) : base(string.Join(", ", messages)) { }
    public BadRequestException(Error error) : base(JsonSerializer.Serialize(error.ToErrors())) { }
    public BadRequestException(Errors errors) : base(JsonSerializer.Serialize(errors)) { }

    public BadRequestException() : base()
    {
    }

    public BadRequestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
