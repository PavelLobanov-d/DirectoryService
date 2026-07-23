using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DirectoryService.Domain.shared;

public class GeneralErrors
{
    public static Error ValueIsInvalid(string? name = null)
    {
        return Error.Validation("value.is.invalid", $"{name ?? "значение"} недействительно");
    }
    public static Error NotFound(Guid? id, string? name = null)
    {
        string label = id == null ? string.Empty : $" по Id '{id}'";
        return Error.NotFound("record.not.found", $"{name ?? "запись"} не найдена{label}");
    }
    public static Error ValueIsRequired(string? name = null)
    {
        string label = name == null ? string.Empty : $"{name} ";
        return Error.Validation("value.is.required", $"Поле {label}обязательно");
    }
    public static Error AlreadyExist()
    {
        return Error.Conflict("record.already.exist", "Запись уже существует");
    }
    public static Error Failure(string message)
    {
        return Error.Failure("server.error", message);
    }
    public static Error OtherError(string message)
    {
        return Error.Failure("other.error", message);
    }

}
