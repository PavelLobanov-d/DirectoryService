using System.Text.Json;
using DirectoryService.Shared;

namespace DirectoryService.Domain.shared.Exceptions;

public class ConflictException : Exception
{
    //public ConflictException(IEnumerable<string> messages) : base(string.Join(", ", messages)) { }
    public ConflictException(Error error) : base(JsonSerializer.Serialize(error.ToErrors())) { }
    public ConflictException(Errors errors) : base(JsonSerializer.Serialize(errors)) { }

    public ConflictException() : base()
    {
    }

    public ConflictException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}