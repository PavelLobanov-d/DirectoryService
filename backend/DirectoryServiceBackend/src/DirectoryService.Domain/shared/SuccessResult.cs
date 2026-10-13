using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DirectoryService.Domain.shared;

public sealed class SuccessResult<TValue> : Microsoft.AspNetCore.Http.IResult
{
    private readonly TValue _value;
    public SuccessResult(TValue value)
    {
        _value = value;
    }
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        Envelope envelope;

        if (_value is CreatedObjectGuid)
        {
            CreatedObjectGuid? createdGuid = _value as CreatedObjectGuid?;
            envelope = Envelope.Ok(createdGuid != null ? createdGuid.Value : _value);
            httpContext.Response.StatusCode = (int)HttpStatusCode.Created;
        }
        else
        {
            envelope = Envelope.Ok(_value);
            httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}
