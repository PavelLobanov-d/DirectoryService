using System.Net;
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

        HttpResponseMessage getByIdResponse = await Client.GetAsync($"/departments/{departmentId}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        ApiEnvelope<GetDepartmentResponse> departmentEnvelope =
            await ReadEnvelopeAsync<GetDepartmentResponse>(getByIdResponse);

        Assert.False(departmentEnvelope.IsError);
        Assert.NotNull(departmentEnvelope.Result);
        Assert.Equal(departmentId, departmentEnvelope.Result.Id);
        Assert.Equal("Dept Alpha", departmentEnvelope.Result.Name);
        Assert.Contains(locationId, departmentEnvelope.Result.LocationsIds ?? []);

        HttpResponseMessage listResponse = await Client.GetAsync("/departments?search=dept%20alpha");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        ApiEnvelope<GetDepartmentsResponse> listEnvelope =
            await ReadEnvelopeAsync<GetDepartmentsResponse>(listResponse);

        Assert.False(listEnvelope.IsError);
        Assert.NotNull(listEnvelope.Result);
        Assert.Equal(1L, listEnvelope.Result.TotalCount);
        Assert.Single(listEnvelope.Result.Items);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/departments/{departmentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        ApiEnvelope<bool> deleteEnvelope = await ReadEnvelopeAsync<bool>(deleteResponse);
        Assert.False(deleteEnvelope.IsError);
        Assert.True(deleteEnvelope.Result);

        Assert.Equal(0, await CountRowsAsync("departments", "id", departmentId));
        Assert.Equal(0, await CountRowsAsync("department_locations", "department_id", departmentId));
        Assert.Equal(0, await CountRowsAsync("department_chiefpositions", "department_id", departmentId));
    }
}
