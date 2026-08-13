using FluentValidation.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DirectoryService.Domain.shared;

[JsonConverter(typeof(ErrorsConverter))]
public class Errors : IEnumerable<Error>
{
    private List<Error> _errors = [];
    public Errors(List<Error> errors)
    {
        _errors = [.. errors];
    }
    public Errors(IEnumerable<Error> errors)
    {
        _errors = [.. errors];
    }
    public Errors(ValidationResult validationResult)
    {
        foreach (var failure in validationResult.Errors)
        {
            _errors.Add(failure);
        }
    }
    public static implicit operator Errors(List<Error> errors) => new(errors);
    public static implicit operator Errors(Error[] errors) => new(errors);
    public static implicit operator Errors(Error error) => new([error]);


    public IEnumerator<Error> GetEnumerator()
    {
        return _errors.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ErrorsConverter : JsonConverter<Errors>
{
    public override Errors Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Читаем JSON-массив напрямую в List<Error>
        var list = JsonSerializer.Deserialize<List<Error>>(ref reader, options);
        return new Errors(list ?? []);
    }

    public override void Write(Utf8JsonWriter writer, Errors value, JsonSerializerOptions options)
    {
        // При сериализации превращаем объект обратно в чистый JSON-массив
        JsonSerializer.Serialize(writer, value.ToList(), options);
    }
}

