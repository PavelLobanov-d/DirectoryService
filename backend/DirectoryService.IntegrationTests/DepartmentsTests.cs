using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class DepartmentsTests : IntegrationTestBase
{
    public DepartmentsTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateReadFilterAndDeleteDepartment_Works()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid locationId = await CreateLocationAsync("Head Office", "Head street 1");
        Guid departmentId = await CreateDepartmentAsync(
            "Dept Alpha",
            "deptalpha",
            chiefId,
            locationsId: locationId);

        await CreateDepartmentAsync(
            "Dept Beta",
            "deptbeta",
            chiefId,
            locationsId: locationId);

        HttpResponseMessage getByIdResponse = await Client.GetAsync($"/departments/{departmentId}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        ApiEnvelope<GetDepartmentResponse> departmentEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(getByIdResponse);

        Assert.False(departmentEnvelope.IsError);
        Assert.NotNull(departmentEnvelope.Result);
        Assert.Equal(departmentId, departmentEnvelope.Result.Id);
        Assert.Equal("Dept Alpha", departmentEnvelope.Result.Name);
        Assert.Contains(locationId, departmentEnvelope.Result.LocationsIds ?? []);

        HttpResponseMessage listResponse = await Client.GetAsync("/departments?search=name%3Ddept%20alpha");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        ApiEnvelope<GetDepartmentsResponse> listEnvelope =
            await ReadEnvelopeAsync<GetDepartmentsResponse>(listResponse);

        Assert.False(listEnvelope.IsError);
        Assert.NotNull(listEnvelope.Result);
        Assert.Equal(1L, listEnvelope.Result.TotalCount);
        Assert.Single(listEnvelope.Result.Items);

        HttpResponseMessage commonSearchResponse = await Client.GetAsync("/departments?search=name%3Ddept");
        ApiEnvelope<GetDepartmentsResponse> commonSearchEnvelope =
            await ReadEnvelopeAsync<GetDepartmentsResponse>(commonSearchResponse);

        Assert.Equal(HttpStatusCode.OK, commonSearchResponse.StatusCode);
        Assert.NotNull(commonSearchEnvelope.Result);
        Assert.Equal(2L, commonSearchEnvelope.Result.TotalCount);
        Assert.Equal(2, commonSearchEnvelope.Result.Items.Count);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/departments/{departmentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        ApiEnvelope<bool?> deleteEnvelope = await ReadEnvelopeAsync<bool?>(deleteResponse);
        Assert.False(deleteEnvelope.IsError);
        Assert.True(deleteEnvelope.Result == true);

        Assert.Equal(1, await CountRowsAsync("departments", "id", departmentId));
        Assert.True(await IsSoftDeletedAsync("departments", "id", departmentId));
        Assert.Equal(0, await CountRowsAsync("department_locations", "department_id", departmentId));
        Assert.Equal(0, await CountRowsAsync("department_chiefpositions", "department_id", departmentId));

        HttpResponseMessage listAfterDeleteResponse =
            await Client.GetAsync("/departments?search=name%3Ddept%20alpha");
        ApiEnvelope<GetDepartmentsResponse> listAfterDeleteEnvelope =
            await ReadEnvelopeAsync<GetDepartmentsResponse>(listAfterDeleteResponse);

        Assert.Equal(HttpStatusCode.OK, listAfterDeleteResponse.StatusCode);
        Assert.NotNull(listAfterDeleteEnvelope.Result);
        Assert.Equal(0L, listAfterDeleteEnvelope.Result.TotalCount);

        HttpResponseMessage getDeletedResponse = await Client.GetAsync($"/departments/{departmentId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteDepartment_MissingId_ReturnsNotFound()
    {
        HttpResponseMessage response = await Client.DeleteAsync($"/departments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiEnvelope<bool?> envelope = await ReadEnvelopeAsync<bool?>(response);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "record.not.found");
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_ReturnsConflict()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid locationId = await CreateLocationAsync("Conflict Office", "Conflict street 1");

        await CreateDepartmentAsync("Dept Conflict", "deptconflict", chiefId, locationsId: locationId);

        HttpResponseMessage response = await Client.PostAsJsonAsync("/departments", new
        {
            name = "Dept Conflict",
            slug = "deptconflict",
            parentDepartmentId = (Guid?)null,
            chiefPositionMatrixId = chiefId,
            locationsId = new[] { locationId },
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ApiEnvelope<Guid?> envelope = await ReadEnvelopeAsync<Guid?>(response);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "record.already.exist");
    }
}



