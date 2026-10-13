using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DirectoryService.Domain.shared;

public sealed class ErrorsResult : IResult
{
    private readonly Errors _errors;
    public ErrorsResult(Errors errors)
    {
        _errors = errors;
    }
    public ErrorsResult(Error error)
    {
        _errors = error.ToErrors();
    }
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if(!_errors.Any())
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return httpContext.Response.WriteAsJsonAsync(Envelope.Error(_errors));
        }

        var distinctErrorTypes = _errors
            .Select(x => x.Type)
            .Distinct()
            .ToList();
        int statusCode = distinctErrorTypes.Count > 1
            ? StatusCodes.Status500InternalServerError
            : GetStatusCodeForErrorType(distinctErrorTypes[0]);

        var envelope = Envelope.Error(_errors);
        httpContext.Response.StatusCode = statusCode;

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
    private static int GetStatusCodeForErrorType(Error.ErrorType errorType) =>    
        errorType switch
        {
            Error.ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            Error.ErrorType.AUTHORIZATION => StatusCodes.Status401Unauthorized,
            Error.ErrorType.NOTFOUND => StatusCodes.Status404NotFound,
            Error.ErrorType.CONFLICT => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    
}
