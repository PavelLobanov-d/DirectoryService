using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DirectoryService.Shared;

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

        if (_value is CreatedObjectGuid createdGuid)
        {
            envelope = Envelope.Ok(createdGuid.Value);
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
