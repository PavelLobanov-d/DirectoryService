using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DirectoryService.Domain.shared;

public static class GeneralErrors
{
    public static Error ValueIsInvalid(string? fieldName = null)
    {
        return Error.Validation("value.is.invalid", $"{fieldName ?? "значение"} недействительно", fieldName);
    }
    public static Error NotFound(Guid? id, string? record = null)
    {
        string label = id == null ? string.Empty : $" по Id '{id}'";
        return Error.NotFound("record.not.found", $"Запись {record ?? string.Empty} не найдена{label}", id);
    }
    public static Error ValueIsRequired(string? fieldName = null)
    {
        string label = fieldName == null ? string.Empty : $"{fieldName} ";
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
    public static Error Conflict(string? code,  string message)
    {
        return Error.Conflict(code ?? "conflict", message);
    }
    public static Error OtherError(string message)
    {
        return Error.Failure("other.error", message);
    }
}
