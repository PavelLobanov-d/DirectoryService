using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class DepartmentMoveConcurrencyTests : IntegrationTestBase
{
    public DepartmentMoveConcurrencyTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ConcurrentMove_SameNodeToDifferentParents_KeepsTreeConsistent()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid locationId = await CreateLocationAsync("Concurrent Office", "Concurrent street 1");

        Guid parentX = await CreateDepartmentAsync(
            "Concurrent Parent X",
            "concurrentparentx",
            chiefId,
            locationsId: locationId);
        Guid parentY = await CreateDepartmentAsync(
            "Concurrent Parent Y",
            "concurrentparenty",
            chiefId,
            locationsId: locationId);
        Guid moved = await CreateDepartmentAsync(
            "Concurrent Moved",
            "concurrentmoved",
            chiefId,
            locationsId: locationId);
        Guid movedChild = await CreateDepartmentAsync(
            "Concurrent Moved Child",
            "concurrentmovedchild",
            childChiefId,
            moved,
            locationId);

        Task<HttpResponseMessage> moveToX = Client.PutAsJsonAsync(
            $"/departments/{moved}/parent",
            new { parentId = parentX });
        Task<HttpResponseMessage> moveToY = Client.PutAsJsonAsync(
            $"/departments/{moved}/parent",
            new { parentId = parentY });

        HttpResponseMessage[] responses = await Task.WhenAll(moveToX, moveToY);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        HttpResponseMessage movedResponse = await Client.GetAsync($"/departments/{moved}");
        ApiEnvelope<GetDepartmentResponse> movedEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(movedResponse);

        Assert.Equal(HttpStatusCode.OK, movedResponse.StatusCode);
        Assert.NotNull(movedEnvelope.Result);
        Assert.Contains(movedEnvelope.Result.ParentId, new Guid?[] { parentX, parentY });
        Assert.Equal(1, movedEnvelope.Result.Path.Count(c => c == '.'));

        HttpResponseMessage childResponse = await Client.GetAsync($"/departments/{movedChild}");
        ApiEnvelope<GetDepartmentResponse> childEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(childResponse);

        Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
        Assert.NotNull(childEnvelope.Result);
        Assert.Equal(
            $"{movedEnvelope.Result.Path}.concurrentmovedchild",
            childEnvelope.Result.Path);
        Assert.Equal(2, childEnvelope.Result.Path.Count(c => c == '.'));
    }

    [Fact]
    public async Task ConcurrentMove_AToBAndBToA_OneWinsAndOtherGetsCycleConflict()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid locationId = await CreateLocationAsync("Concurrent Office", "Concurrent street 1");

        Guid departmentA = await CreateDepartmentAsync(
            "Concurrent Department A",
            "concurrentdepta",
            chiefId,
            locationsId: locationId);
        Guid departmentB = await CreateDepartmentAsync(
            "Concurrent Department B",
            "concurrentdeptb",
            chiefId,
            locationsId: locationId);

        Task<HttpResponseMessage> moveAToB = Client.PutAsJsonAsync(
            $"/departments/{departmentA}/parent",
            new { parentId = departmentB });
        Task<HttpResponseMessage> moveBToA = Client.PutAsJsonAsync(
            $"/departments/{departmentB}/parent",
            new { parentId = departmentA });

        HttpResponseMessage[] responses = await Task.WhenAll(moveAToB, moveBToA);

        int successCount = responses.Count(response => response.StatusCode == HttpStatusCode.OK);
        HttpResponseMessage conflictResponse = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);

        ApiEnvelope<ChangeDepartmentParentResponse> conflictEnvelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(conflictResponse);

        Assert.True(conflictEnvelope.IsError);
        Assert.NotNull(conflictEnvelope.Errors);
        Assert.Contains(
            conflictEnvelope.Errors,
            error => error.Code == "department.move.cycle");

        HttpResponseMessage aResponse = await Client.GetAsync($"/departments/{departmentA}");
        ApiEnvelope<GetDepartmentResponse> aEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(aResponse);

        HttpResponseMessage bResponse = await Client.GetAsync($"/departments/{departmentB}");
        ApiEnvelope<GetDepartmentResponse> bEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(bResponse);

        Assert.NotNull(aEnvelope.Result);
        Assert.NotNull(bEnvelope.Result);

        bool aUnderB = aEnvelope.Result.ParentId == departmentB
            && aEnvelope.Result.Path.StartsWith("concurrentdeptb.", StringComparison.Ordinal);
        bool bUnderA = bEnvelope.Result.ParentId == departmentA
            && bEnvelope.Result.Path.StartsWith("concurrentdepta.", StringComparison.Ordinal);

        Assert.True(aUnderB ^ bUnderA);
    }
}

