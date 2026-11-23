using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class PositionMoveTests : IntegrationTestBase
{
    public PositionMoveTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task MovePosition_WithDescendant_UpdatesSubtree()
    {
        (Guid parentX, Guid parentY, Guid moved, Guid child) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentY });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Equal(parentY, envelope.Result.ParentId);
        Assert.Equal("positiony.positionmoved", envelope.Result.Path);
        Assert.Equal(1, envelope.Result.Depth);

        HttpResponseMessage childResponse = await Client.GetAsync($"/positions/{child}");
        ApiEnvelope<GetPositionMatrixResponse> childEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(childResponse);

        Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
        Assert.NotNull(childEnvelope.Result);
        Assert.Equal("positiony.positionmoved.positionchild", childEnvelope.Result.Path);
        Assert.Equal(2, childEnvelope.Result.Path.Count(c => c == '.'));
    }

    [Fact]
    public async Task MovePosition_ToRoot_UpdatesSubtree()
    {
        (Guid parentX, Guid _, Guid moved, Guid child) = await CreateMoveTreeAsync();

        HttpResponseMessage firstResponse = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentX });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = (Guid?)null });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Null(envelope.Result.ParentId);
        Assert.Equal("positionmoved", envelope.Result.Path);
        Assert.Equal(0, envelope.Result.Depth);

        HttpResponseMessage childResponse = await Client.GetAsync($"/positions/{child}");
        ApiEnvelope<GetPositionMatrixResponse> childEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(childResponse);

        Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
        Assert.NotNull(childEnvelope.Result);
        Assert.Equal("positionmoved.positionchild", childEnvelope.Result.Path);
    }

    [Fact]
    public async Task MovePosition_IntoOwnSubtree_ReturnsCycleConflict()
    {
        (Guid parentX, Guid parentY, Guid moved, Guid child) = await CreateMoveTreeAsync();

        HttpResponseMessage firstResponse = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentY });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = child });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "position.move.cycle");
    }

    [Fact]
    public async Task MovePosition_UnderItself_ReturnsBadRequest()
    {
        (Guid _, Guid __, Guid moved, Guid ___) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = moved });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "position.move.parent_is_self");
    }

    [Fact]
    public async Task MovePosition_UnderDeletedParent_ReturnsConflict()
    {
        (Guid _, Guid __, Guid moved, Guid ___) = await CreateMoveTreeAsync();

        Guid deletedParentId = await CreatePositionAsync("Position Deleted", "positiondeleted");
        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/positions/{deletedParentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = deletedParentId });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "position.move.parent_deleted");
    }

    [Fact]
    public async Task MovePosition_MissingParent_ReturnsNotFound()
    {
        (Guid _, Guid __, Guid moved, Guid ___) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = Guid.NewGuid() });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "position.parent.not_found");
    }

    [Fact]
    public async Task MovePosition_MissingPosition_ReturnsNotFound()
    {
        (Guid parentX, Guid _, Guid __, Guid ___) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/positions/{Guid.NewGuid()}/parent",
            new { parentId = parentX });
        ApiEnvelope<ChangePositionMatrixParentResponse> envelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "position.not_found");
    }

    [Fact]
    public async Task MovePosition_ToSameParent_IsNoOp()
    {
        (Guid parentX, Guid _, Guid moved, Guid __) = await CreateMoveTreeAsync();

        HttpResponseMessage firstResponse = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentX });
        ApiEnvelope<ChangePositionMatrixParentResponse> firstEnvelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(firstResponse);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.NotNull(firstEnvelope.Result);

        HttpResponseMessage secondResponse = await Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentX });
        ApiEnvelope<ChangePositionMatrixParentResponse> secondEnvelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(secondResponse);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(secondEnvelope.Result);
        Assert.Equal(firstEnvelope.Result.Path, secondEnvelope.Result.Path);
        Assert.Equal(firstEnvelope.Result.UpdatedAt, secondEnvelope.Result.UpdatedAt);
    }

    [Fact]
    public async Task ConcurrentMove_SamePositionToDifferentParents_KeepsTreeConsistent()
    {
        (Guid parentX, Guid parentY, Guid moved, Guid child) = await CreateMoveTreeAsync();

        Task<HttpResponseMessage> moveToX = Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentX });
        Task<HttpResponseMessage> moveToY = Client.PutAsJsonAsync(
            $"/positions/{moved}/parent",
            new { parentId = parentY });

        HttpResponseMessage[] responses = await Task.WhenAll(moveToX, moveToY);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        HttpResponseMessage movedResponse = await Client.GetAsync($"/positions/{moved}");
        ApiEnvelope<GetPositionMatrixResponse> movedEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(movedResponse);

        Assert.NotNull(movedEnvelope.Result);
        Assert.Contains(movedEnvelope.Result.ParentId, new Guid?[] { parentX, parentY });
        Assert.Equal(1, movedEnvelope.Result.Path.Count(c => c == '.'));

        HttpResponseMessage childResponse = await Client.GetAsync($"/positions/{child}");
        ApiEnvelope<GetPositionMatrixResponse> childEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(childResponse);

        Assert.NotNull(childEnvelope.Result);
        Assert.Equal(
            $"{movedEnvelope.Result.Path}.positionchild",
            childEnvelope.Result.Path);
    }

    [Fact]
    public async Task ConcurrentMove_AToBAndBToA_OneWinsAndOtherGetsCycleConflict()
    {
        Guid departmentA = await CreatePositionAsync("Position A", "positiona");
        Guid departmentB = await CreatePositionAsync("Position B", "positionb");

        Task<HttpResponseMessage> moveAToB = Client.PutAsJsonAsync(
            $"/positions/{departmentA}/parent",
            new { parentId = departmentB });
        Task<HttpResponseMessage> moveBToA = Client.PutAsJsonAsync(
            $"/positions/{departmentB}/parent",
            new { parentId = departmentA });

        HttpResponseMessage[] responses = await Task.WhenAll(moveAToB, moveBToA);

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));

        HttpResponseMessage conflictResponse = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);

        ApiEnvelope<ChangePositionMatrixParentResponse> conflictEnvelope =
            await ReadEnvelopeAsync<ChangePositionMatrixParentResponse>(conflictResponse);

        Assert.True(conflictEnvelope.IsError);
        Assert.NotNull(conflictEnvelope.Errors);
        Assert.Contains(
            conflictEnvelope.Errors,
            error => error.Code == "position.move.cycle");

        HttpResponseMessage aResponse = await Client.GetAsync($"/positions/{departmentA}");
        ApiEnvelope<GetPositionMatrixResponse> aEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(aResponse);

        HttpResponseMessage bResponse = await Client.GetAsync($"/positions/{departmentB}");
        ApiEnvelope<GetPositionMatrixResponse> bEnvelope =
            await ReadEnvelopeAsync<GetPositionMatrixResponse>(bResponse);

        Assert.NotNull(aEnvelope.Result);
        Assert.NotNull(bEnvelope.Result);

        bool aUnderB = aEnvelope.Result.ParentId == departmentB
            && aEnvelope.Result.Path.StartsWith("positionb.", StringComparison.Ordinal);
        bool bUnderA = bEnvelope.Result.ParentId == departmentA
            && bEnvelope.Result.Path.StartsWith("positiona.", StringComparison.Ordinal);

        Assert.True(aUnderB ^ bUnderA);
    }

    private async Task<(Guid ParentX, Guid ParentY, Guid Moved, Guid Child)> CreateMoveTreeAsync()
    {
        Guid parentX = await CreatePositionAsync("Position X", "positionx");
        Guid parentY = await CreatePositionAsync("Position Y", "positiony");
        Guid moved = await CreatePositionAsync("Position Moved", "positionmoved");
        Guid child = await CreatePositionAsync("Position Child", "positionchild", moved);

        return (parentX, parentY, moved, child);
    }
}
