using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation.Results;

namespace DirectoryService.Domain.shared;
public record Error
{
    private string Code { get; }
    private string Message { get; }

    public ErrorType Type {  get; }
    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }
    public static Error None = new(string.Empty, string.Empty, ErrorType.NONE);
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.VALIDATION);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NOTFOUND);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.FAILURE);
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.CONFLICT);
    public static Error Authentication(string code, string message) =>
        new(code, message, ErrorType.AUTHENTICATION);
    public static Error Authorization(string code, string message) =>
        new(code, message, ErrorType.AUTHORIZATION);

    public enum ErrorType
    {
        NONE,
        VALIDATION,
        FAILURE,
        NOTFOUND,
        CONFLICT,
        AUTHENTICATION,
        AUTHORIZATION,
    }

    public static implicit operator Error(ValidationFailure error)
    {
        return Failure(error.ErrorCode, error.ErrorMessage);
    }

    public Errors ToErrors() => this;
}
