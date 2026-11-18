using System.Net;
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
