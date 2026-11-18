using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class DepartmentTreeTests : IntegrationTestBase
{
    public DepartmentTreeTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Tree_ReturnsOnlyRootsWithDepthAndChildrenCount()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid locationId = await CreateLocationAsync("Tree Office", "Tree street 1");

        Guid rootId = await CreateDepartmentAsync(
            "Tree Root",
            "treeroot",
            chiefId,
            locationsId: locationId);
        Guid childId = await CreateDepartmentAsync(
            "Tree Child",
            "treechild",
            childChiefId,
            rootId,
            locationId);
        Guid secondRootId = await CreateDepartmentAsync(
            "Tree Root Two",
            "treeroottwo",
            chiefId,
            locationsId: locationId);

        HttpResponseMessage response = await Client.GetAsync("/departments/tree");
        ApiEnvelope<List<DepartmentTreeItemResponse>> envelope =
            await ReadEnvelopeAsync<List<DepartmentTreeItemResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);

        DepartmentTreeItemResponse root = Assert.Single(
            envelope.Result,
            item => item.Id == rootId);
        Assert.Equal("treeroot", root.Path);
        Assert.Equal(0, root.Depth);
        Assert.True(root.HasChildren);
        Assert.Equal(1, root.ChildrenCount);

        Assert.DoesNotContain(envelope.Result, item => item.Id == childId);
        Assert.Contains(envelope.Result, item => item.Id == secondRootId);
    }

    [Fact]
    public async Task Children_ReturnsOnlyDirectChildren()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid grandchildChiefId = await CreatePositionAsync("Lead", "lead", childChiefId);
        Guid locationId = await CreateLocationAsync("Tree Office", "Tree street 1");

        Guid rootId = await CreateDepartmentAsync(
            "Children Root",
            "childrenroot",
            chiefId,
            locationsId: locationId);
        Guid childId = await CreateDepartmentAsync(
            "Children Child",
            "childrenchild",
            childChiefId,
            rootId,
            locationId);
        Guid grandchildId = await CreateDepartmentAsync(
            "Children Grandchild",
            "childrengrandchild",
            grandchildChiefId,
            childId,
            locationId);

        HttpResponseMessage rootResponse = await Client.GetAsync($"/departments/{rootId}/children");
        ApiEnvelope<List<DepartmentTreeItemResponse>> rootEnvelope =
            await ReadEnvelopeAsync<List<DepartmentTreeItemResponse>>(rootResponse);

        Assert.Equal(HttpStatusCode.OK, rootResponse.StatusCode);
        Assert.NotNull(rootEnvelope.Result);
        DepartmentTreeItemResponse child = Assert.Single(rootEnvelope.Result);
        Assert.Equal(childId, child.Id);
        Assert.Equal(1, child.Depth);
        Assert.Equal("childrenroot.childrenchild", child.Path);
        Assert.True(child.HasChildren);
        Assert.Equal(1, child.ChildrenCount);
        Assert.DoesNotContain(rootEnvelope.Result, item => item.Id == grandchildId);

        HttpResponseMessage leafResponse = await Client.GetAsync($"/departments/{grandchildId}/children");
        ApiEnvelope<List<DepartmentTreeItemResponse>> leafEnvelope =
            await ReadEnvelopeAsync<List<DepartmentTreeItemResponse>>(leafResponse);

        Assert.Equal(HttpStatusCode.OK, leafResponse.StatusCode);
        Assert.NotNull(leafEnvelope.Result);
        Assert.Empty(leafEnvelope.Result);

        HttpResponseMessage missingResponse =
            await Client.GetAsync($"/departments/{Guid.NewGuid()}/children");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Ancestors_ReturnsRootToDirectParent()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid grandchildChiefId = await CreatePositionAsync("Lead", "lead", childChiefId);
        Guid locationId = await CreateLocationAsync("Tree Office", "Tree street 1");

        Guid rootId = await CreateDepartmentAsync(
            "Ancestors Root",
            "ancestorsroot",
            chiefId,
            locationsId: locationId);
        Guid childId = await CreateDepartmentAsync(
            "Ancestors Child",
            "ancestorschild",
            childChiefId,
            rootId,
            locationId);
        Guid grandchildId = await CreateDepartmentAsync(
            "Ancestors Grandchild",
            "ancestorsgrandchild",
            grandchildChiefId,
            childId,
            locationId);

        HttpResponseMessage response =
            await Client.GetAsync($"/departments/{grandchildId}/ancestors");
        ApiEnvelope<List<DepartmentAncestorResponse>> envelope =
            await ReadEnvelopeAsync<List<DepartmentAncestorResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2, envelope.Result.Count);
        Assert.Equal(rootId, envelope.Result[0].Id);
        Assert.Equal(0, envelope.Result[0].Depth);
        Assert.Equal(childId, envelope.Result[1].Id);
        Assert.Equal(1, envelope.Result[1].Depth);
        Assert.DoesNotContain(envelope.Result, item => item.Id == grandchildId);

        HttpResponseMessage missingResponse =
            await Client.GetAsync($"/departments/{Guid.NewGuid()}/ancestors");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsMatchesWithAncestorsAndValidatesQueryLength()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid locationId = await CreateLocationAsync("Tree Office", "Tree street 1");

        Guid rootId = await CreateDepartmentAsync(
            "Search Root",
            "searchroot",
            chiefId,
            locationsId: locationId);
        Guid childId = await CreateDepartmentAsync(
            "Search Alpha",
            "searchalpha",
            childChiefId,
            rootId,
            locationId);

        HttpResponseMessage response =
            await Client.GetAsync("/departments/tree/search?q=ALPHA");
        ApiEnvelope<List<DepartmentTreeSearchItemResponse>> envelope =
            await ReadEnvelopeAsync<List<DepartmentTreeSearchItemResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        DepartmentTreeSearchItemResponse item = Assert.Single(envelope.Result);
        Assert.Equal(childId, item.Id);
        Assert.Equal("searchroot.searchalpha", item.Path);
        Assert.Equal(1, item.Depth);
        Assert.Single(item.Ancestors);
        Assert.Equal(rootId, item.Ancestors[0].Id);

        HttpResponseMessage shortResponse =
            await Client.GetAsync("/departments/tree/search?q=a");
        Assert.Equal(HttpStatusCode.BadRequest, shortResponse.StatusCode);

        HttpResponseMessage emptyResponse =
            await Client.GetAsync("/departments/tree/search");
        Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);
    }

    [Fact]
    public async Task Rename_UpdatesStoredPathAndKeepsDepth()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid locationId = await CreateLocationAsync("Tree Office", "Tree street 1");

        Guid rootId = await CreateDepartmentAsync(
            "Rename Root",
            "renameroot",
            chiefId,
            locationsId: locationId);
        Guid childId = await CreateDepartmentAsync(
            "Rename Child",
            "renamechild",
            childChiefId,
            rootId,
            locationId);

        HttpResponseMessage updateResponse = await Client.PutAsJsonAsync(
            $"/departments/{childId}",
            new { newName = "Renamed Child", newSlug = "renamedchild" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        HttpResponseMessage response = await Client.GetAsync($"/departments/{childId}");
        ApiEnvelope<GetDepartmentResponse> envelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope.Result);
        Assert.Equal("renameroot.renamedchild", envelope.Result.Path);

        HttpResponseMessage childrenResponse = await Client.GetAsync($"/departments/{rootId}/children");
        ApiEnvelope<List<DepartmentTreeItemResponse>> childrenEnvelope =
            await ReadEnvelopeAsync<List<DepartmentTreeItemResponse>>(childrenResponse);

        Assert.NotNull(childrenEnvelope.Result);
        DepartmentTreeItemResponse child = Assert.Single(childrenEnvelope.Result);
        Assert.Equal("renameroot.renamedchild", child.Path);
        Assert.Equal(1, child.Depth);
    }

    [Fact]
    public async Task TreeEndpoints_ExcludeSoftDeletedNodes()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid childChiefId = await CreatePositionAsync("Manager", "manager", chiefId);
        Guid locationId = await CreateLocationAsync("Tree Office", "Tree street 1");

        Guid rootId = await CreateDepartmentAsync(
            "Deleted Child Root",
            "deletedchildroot",
            chiefId,
            locationsId: locationId);
        Guid childId = await CreateDepartmentAsync(
            "Deleted Child",
            "deletedchild",
            childChiefId,
            rootId,
            locationId);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/departments/{childId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        HttpResponseMessage treeResponse = await Client.GetAsync("/departments/tree");
        ApiEnvelope<List<DepartmentTreeItemResponse>> treeEnvelope =
            await ReadEnvelopeAsync<List<DepartmentTreeItemResponse>>(treeResponse);

        Assert.NotNull(treeEnvelope.Result);
        DepartmentTreeItemResponse root = Assert.Single(
            treeEnvelope.Result,
            item => item.Id == rootId);
        Assert.False(root.HasChildren);
        Assert.Equal(0, root.ChildrenCount);

        HttpResponseMessage childrenResponse = await Client.GetAsync($"/departments/{rootId}/children");
        ApiEnvelope<List<DepartmentTreeItemResponse>> childrenEnvelope =
            await ReadEnvelopeAsync<List<DepartmentTreeItemResponse>>(childrenResponse);
        Assert.NotNull(childrenEnvelope.Result);
        Assert.Empty(childrenEnvelope.Result);

        HttpResponseMessage searchResponse =
            await Client.GetAsync("/departments/tree/search?q=deletedchild");
        ApiEnvelope<List<DepartmentTreeSearchItemResponse>> searchEnvelope =
            await ReadEnvelopeAsync<List<DepartmentTreeSearchItemResponse>>(searchResponse);
        Assert.NotNull(searchEnvelope.Result);
        Assert.Empty(searchEnvelope.Result);

        HttpResponseMessage ancestorsResponse =
            await Client.GetAsync($"/departments/{childId}/ancestors");
        Assert.Equal(HttpStatusCode.NotFound, ancestorsResponse.StatusCode);
    }
}
