using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class DepartmentMoveTests : IntegrationTestBase
{
    public DepartmentMoveTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task MoveDepartment_WithDescendants_UpdatesSubtree()
    {
        (Guid rootA, Guid child, Guid grand, Guid rootB) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = rootB });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Equal(rootB, envelope.Result.ParentId);
        Assert.Equal("moverootb.movechild", envelope.Result.Path);
        Assert.Equal(1, envelope.Result.Depth);
        Assert.NotEqual(default, envelope.Result.UpdatedAt);

        HttpResponseMessage grandResponse = await Client.GetAsync($"/departments/{grand}");
        ApiEnvelope<GetDepartmentResponse> grandEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(grandResponse);

        Assert.Equal(HttpStatusCode.OK, grandResponse.StatusCode);
        Assert.NotNull(grandEnvelope.Result);
        Assert.Equal("moverootb.movechild.movegrand", grandEnvelope.Result.Path);
    }

    [Fact]
    public async Task MoveDepartment_ToRoot_UpdatesSubtree()
    {
        (Guid _, Guid child, Guid grand, Guid __) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = (Guid?)null });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Null(envelope.Result.ParentId);
        Assert.Equal("movechild", envelope.Result.Path);
        Assert.Equal(0, envelope.Result.Depth);

        HttpResponseMessage grandResponse = await Client.GetAsync($"/departments/{grand}");
        ApiEnvelope<GetDepartmentResponse> grandEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(grandResponse);

        Assert.Equal(HttpStatusCode.OK, grandResponse.StatusCode);
        Assert.NotNull(grandEnvelope.Result);
        Assert.Equal("movechild.movegrand", grandEnvelope.Result.Path);
    }

    [Fact]
    public async Task MoveDepartment_IntoOwnSubtree_ReturnsCycleConflict()
    {
        (Guid rootA, Guid _, Guid grand, Guid __) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{rootA}/parent",
            new { parentId = grand });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "department.move.cycle");
    }

    [Fact]
    public async Task MoveDepartment_UnderItself_ReturnsBadRequest()
    {
        (Guid _, Guid child, Guid __, Guid ___) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = child });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "department.move.parent_is_self");
    }

    [Fact]
    public async Task MoveDepartment_UnderDeletedParent_ReturnsConflict()
    {
        (Guid _, Guid child, Guid __, Guid ___) = await CreateMoveTreeAsync();

        Guid chiefId = await CreatePositionAsync("Deleted Director", "deleteddirector");
        Guid locationId = await CreateLocationAsync("Move Deleted Office", "Move deleted street 1");
        Guid deletedParentId = await CreateDepartmentAsync(
            "Move Deleted Root",
            "movedeletedroot",
            chiefId,
            locationsId: locationId);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/departments/{deletedParentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = deletedParentId });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "department.move.parent_deleted");
    }

    [Fact]
    public async Task MoveDepartment_MissingParent_ReturnsNotFound()
    {
        (Guid _, Guid child, Guid __, Guid ___) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = Guid.NewGuid() });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "department.parent.not_found");
    }

    [Fact]
    public async Task MoveDepartment_MissingDepartment_ReturnsNotFound()
    {
        (Guid rootA, Guid _, Guid __, Guid ___) = await CreateMoveTreeAsync();

        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/departments/{Guid.NewGuid()}/parent",
            new { parentId = rootA });
        ApiEnvelope<ChangeDepartmentParentResponse> envelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "department.not_found");
    }

    [Fact]
    public async Task MoveDepartment_ToSameParent_IsNoOp()
    {
        (Guid _, Guid child, Guid __, Guid rootB) = await CreateMoveTreeAsync();

        HttpResponseMessage firstResponse = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = rootB });
        ApiEnvelope<ChangeDepartmentParentResponse> firstEnvelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(firstResponse);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.NotNull(firstEnvelope.Result);

        HttpResponseMessage secondResponse = await Client.PutAsJsonAsync(
            $"/departments/{child}/parent",
            new { parentId = rootB });
        ApiEnvelope<ChangeDepartmentParentResponse> secondEnvelope =
            await ReadEnvelopeAsync<ChangeDepartmentParentResponse>(secondResponse);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(secondEnvelope.Result);
        Assert.Equal(firstEnvelope.Result.Path, secondEnvelope.Result.Path);
        Assert.Equal(firstEnvelope.Result.UpdatedAt, secondEnvelope.Result.UpdatedAt);
    }

    private async Task<(Guid RootA, Guid Child, Guid Grand, Guid RootB)> CreateMoveTreeAsync()
    {
        Guid rootChiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", rootChiefId);
        Guid grandChiefId = await CreatePositionAsync("Lead", "lead", childChiefId);
        Guid locationId = await CreateLocationAsync("Move Office", "Move street 1");

        Guid rootA = await CreateDepartmentAsync(
            "Move Root A",
            "moveroota",
            rootChiefId,
            locationsId: locationId);
        Guid child = await CreateDepartmentAsync(
            "Move Child",
            "movechild",
            childChiefId,
            rootA,
            locationId);
        Guid grand = await CreateDepartmentAsync(
            "Move Grand",
            "movegrand",
            grandChiefId,
            child,
            locationId);
        Guid rootB = await CreateDepartmentAsync(
            "Move Root B",
            "moverootb",
            rootChiefId,
            locationsId: locationId);

        return (rootA, child, grand, rootB);
    }
}
