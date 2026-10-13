using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;

namespace DirectoryService.Domain.shared;

public record Envelope
{
    public object? Result { get; }
    public bool? Created { get; }
    public Errors? Errors { get; }
    public bool isError =>
        Errors != null && Errors.Any();
    public DateTime TimeGenerated { get; }

    [JsonConstructor]
    private Envelope(object? result, Errors? errors, bool? created = false)
    {
        Result = result;
        Created = created;
        Errors = errors;
        TimeGenerated = DateTime.Now;
    }
    public static Envelope Ok(object? result = null, bool created = false) =>
        new(result, null, created);
    public static Envelope Error(Errors errors) =>
        new(null, errors);

}

public record Envelope<T>
{
    public T? Result { get; }
    public bool? Created { get; }
    public Errors? Errors { get; }
    public bool isError =>
        Errors != null && Errors.Any();
    public DateTime TimeGenerated { get; }

    [JsonConstructor]
    private Envelope (T? result, Errors? errors, bool? created = false)
    {
        Result = result;
        Created = created;
        Errors = errors;
        TimeGenerated = DateTime.Now;
    }
    public static Envelope<T> Ok(T? result = default, bool created = false) =>
        new(result, null, created);
    public static Envelope<T> Error(Errors errors) =>
        new(default, errors);
}
