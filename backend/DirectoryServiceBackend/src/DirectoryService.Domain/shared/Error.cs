using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DirectoryService.Domain.shared;
public record Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type {  get; }
    public string? InvalidField { get; }
    public Guid? InvalidGuid { get; }
    [JsonConstructor]
   public Error(string code, string message, ErrorType type, string? invalidField = null, Guid? invalidGuid = null)
    {
        Code = code;
        Message = message;
        Type = type;
        InvalidField = invalidField;
        InvalidGuid = invalidGuid;
    }
    public static Error None = new(string.Empty, string.Empty, ErrorType.NONE);
    public static Error Validation(string? code, string message, string? invalidField = null) =>
        new(code ?? "value.is.invalid" , message, ErrorType.VALIDATION, invalidField);
    public static Error NotFound(string? code, string message, Guid? invalidGuid = null) =>
        new(code ?? "record.not.found", message, ErrorType.NOTFOUND, null, invalidGuid);
    public static Error Failure(string? code, string message) =>
        new(code ?? "server.error", message, ErrorType.FAILURE);
    public static Error Conflict(string? code, string message) =>
        new(code ?? "value.is.conflict", message, ErrorType.CONFLICT);
    public static Error Authentication(string? code, string message) =>
        new(code ?? "authentication.error", message, ErrorType.AUTHENTICATION);
    public static Error Authorization(string? code, string message) =>
        new(code ?? "authorization.error", message, ErrorType.AUTHORIZATION);

    public enum ErrorType
    {
        /// <summary>
        /// нет ошибки
        /// </summary>
        NONE,
        /// <summary>
        /// ошибка валидации
        /// </summary>
        VALIDATION,
        /// <summary>
        /// ошибка сервера
        /// </summary>
        FAILURE,
        /// <summary>
        /// ошибка не найдено
        /// </summary>
        NOTFOUND,
        /// <summary>
        /// ошибка конфликт
        /// </summary>
        CONFLICT,
        /// <summary>
        /// ошибка аутентификации
        /// </summary>
        AUTHENTICATION,
        /// <summary>
        /// ошибка авторизации
        /// </summary>
        AUTHORIZATION,
    }

    public static implicit operator Error(ValidationFailure error)
    {
        return Failure(error.ErrorCode, error.ErrorMessage);
    }

    public Errors ToErrors() => this;
}
