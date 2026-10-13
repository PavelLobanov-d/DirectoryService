using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace DirectoryService.Domain.shared;

public sealed class EndPointResult<TValue> : Microsoft.AspNetCore.Http.IResult, IEndpointMetadataProvider
{
    private readonly Microsoft.AspNetCore.Http.IResult _result;

    public EndPointResult(Result<TValue, Errors> result)
    {
        _result = result.IsSuccess 
            ? new SuccessResult<TValue>(result.Value)
            : new ErrorsResult(result.Error);
    }
    public EndPointResult(Result<TValue, Error> result)
    {
        _result = result.IsSuccess
            ? new SuccessResult<TValue>(result.Value)
            : new ErrorsResult(result.Error);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(200, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(210, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(500, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(400, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(401, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(403, typeof(Envelope<TValue>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(409, typeof(Envelope<TValue>), ["application/json"]));
    }
    public Task ExecuteAsync(HttpContext httpContext) => _result.ExecuteAsync(httpContext);

    public static implicit operator EndPointResult<TValue>(Result<TValue, Error> result) => new(result);
    public static implicit operator EndPointResult<TValue>(Result<TValue, Errors> result) => new(result);
}
