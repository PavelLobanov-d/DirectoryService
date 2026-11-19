using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class PositionsTests : IntegrationTestBase
{
    public PositionsTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateAndRenameChildPosition_ReturnsUpdatedPath()
    {
        Guid rootId = await CreatePositionAsync("Director", "director");
        Guid childId = await CreatePositionAsync("Manager", "manager", rootId);

        HttpResponseMessage createdResponse = await Client.GetAsync($"/positions/{childId}");
        ApiEnvelope<GetPositionMatrixResponse> createdEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(createdResponse);

        Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
        Assert.NotNull(createdEnvelope.Result);
        Assert.Equal("director.manager", createdEnvelope.Result.Path);

        HttpResponseMessage updateResponse = await Client.PutAsJsonAsync(
            $"/positions/{childId}",
            new { newName = "Senior Manager", newSlug = "seniormanager" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        HttpResponseMessage updatedResponse = await Client.GetAsync($"/positions/{childId}");
        ApiEnvelope<GetPositionMatrixResponse> updatedEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(updatedResponse);

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        Assert.NotNull(updatedEnvelope.Result);
        Assert.Equal("director.seniormanager", updatedEnvelope.Result.Path);
    }

    [Fact]
    public async Task CreatePosition_WithDigitSlug_ReturnsPath()
    {
        Guid rootId = await CreatePositionAsync("Director", "director");
        Guid childId = await CreatePositionAsync("Manager 2", "manager2", rootId);

        HttpResponseMessage response = await Client.GetAsync($"/positions/{childId}");
        ApiEnvelope<GetPositionMatrixResponse> envelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Equal("director.manager2", envelope.Result.Path);
    }

    [Fact]
    public async Task DeletePosition_SoftDeletesAndHidesFromReads()
    {
        Guid rootId = await CreatePositionAsync("Director", "director");
        Guid childId = await CreatePositionAsync("Manager", "manager", rootId);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/positions/{childId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        ApiEnvelope<bool?> deleteEnvelope = await ReadEnvelopeAsync<bool?>(deleteResponse);
        Assert.False(deleteEnvelope.IsError);
        Assert.True(deleteEnvelope.Result == true);

        Assert.Equal(1, await CountRowsAsync("positionsmatrix", "id", childId));
        Assert.True(await IsSoftDeletedAsync("positionsmatrix", "id", childId));

        HttpResponseMessage getResponse = await Client.GetAsync($"/positions/{childId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        HttpResponseMessage listResponse = await Client.GetAsync("/positions");
        ApiEnvelope<List<GetPositionMatrixResponse>> listEnvelope =
            await ReadEnvelopeAsync<List<GetPositionMatrixResponse>>(listResponse);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(listEnvelope.Result);
        Assert.DoesNotContain(listEnvelope.Result, position => position.Id == childId);
    }
}
