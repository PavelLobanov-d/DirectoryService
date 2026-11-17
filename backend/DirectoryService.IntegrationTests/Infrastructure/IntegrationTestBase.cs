using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using Npgsql;
using Xunit;

namespace DirectoryService.IntegrationTests.Infrastructure;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected IntegrationTestFixture Fixture { get; }

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
        Client = Fixture.Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<Guid> CreatePositionAsync(
        string name,
        string slug,
        Guid? parentPositionMatrixId = null)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/positions", new
        {
            name,
            slug,
            parentPositionMatrixId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ApiEnvelope<Guid> envelope = await ReadEnvelopeAsync<Guid>(response);
        Assert.False(envelope.IsError);
        Assert.NotEqual(Guid.Empty, envelope.Result);

        return envelope.Result;
    }

    protected async Task<Guid> CreateLocationAsync(string name, string address)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/locations", new
        {
            name,
            address,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ApiEnvelope<Guid> envelope = await ReadEnvelopeAsync<Guid>(response);
        Assert.False(envelope.IsError);
        Assert.NotEqual(Guid.Empty, envelope.Result);

        return envelope.Result;
    }

    protected async Task<Guid> CreateDepartmentAsync(
        string name,
        string slug,
        Guid chiefPositionMatrixId,
        Guid? parentDepartmentId = null,
        params Guid[] locationsId)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/departments", new
        {
            name,
            slug,
            parentDepartmentId,
            chiefPositionMatrixId,
            locationsId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ApiEnvelope<Guid> envelope = await ReadEnvelopeAsync<Guid>(response);
        Assert.False(envelope.IsError);
        Assert.NotEqual(Guid.Empty, envelope.Result);

        return envelope.Result;
    }

    protected static async Task<ApiEnvelope<T>> ReadEnvelopeAsync<T>(HttpResponseMessage response)
    {
        ApiEnvelope<T>? envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions);

        Assert.NotNull(envelope);
        return envelope;
    }

    protected async Task<long> CountRowsAsync(string table, string idColumn, Guid id)
    {
        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {table} WHERE {idColumn} = @id",
            connection);
        command.Parameters.AddWithValue("id", id);

        object? result = await command.ExecuteScalarAsync();
        return (long)result!;
    }

    protected async Task<long> CountRowsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand($"SELECT count(*) FROM {table}", connection);
        object? result = await command.ExecuteScalarAsync();
        return (long)result!;
    }
}

public sealed record ApiEnvelope<T>(
    T? Result,
    List<ApiError>? Errors,
    bool IsError);

public sealed record ApiError(
    string? Code,
    string? Message,
    int Type);

