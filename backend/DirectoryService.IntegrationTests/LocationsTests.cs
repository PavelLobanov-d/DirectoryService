using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class LocationsTests : IntegrationTestBase
{
    public LocationsTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateLocation_ReturnsCreatedAndPersists()
    {
        Guid locationId = await CreateLocationAsync("Alpha Office", "Alpha street 1");

        Assert.Equal(1, await CountRowsAsync("locations", "id", locationId));

        HttpResponseMessage response = await Client.GetAsync($"/locations/{locationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope<GetLocationResponse> envelope = await ReadEnvelopeAsync<GetLocationResponse>(response);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(locationId, envelope.Result.Id);
        Assert.Equal("Alpha Office", envelope.Result.Name);
        Assert.Equal("Alpha street 1", envelope.Result.Address);
    }

    [Fact]
    public async Task GetLocation_MissingId_ReturnsNotFound()
    {
        Guid missingId = Guid.NewGuid();

        HttpResponseMessage response = await Client.GetAsync($"/locations/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiEnvelope<GetLocationResponse> envelope = await ReadEnvelopeAsync<GetLocationResponse>(response);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "record.not.found");
    }

    [Fact]
    public async Task DeleteLocation_MissingId_ReturnsNotFound()
    {
        HttpResponseMessage response = await Client.DeleteAsync($"/locations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiEnvelope<bool?> envelope = await ReadEnvelopeAsync<bool?>(response);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "record.not.found");
    }

    [Fact]
    public async Task CreateLocation_DuplicateName_ReturnsConflict()
    {
        await CreateLocationAsync("Duplicate Office", "Duplicate street 1");

        HttpResponseMessage response = await Client.PostAsJsonAsync("/locations", new
        {
            name = "Duplicate Office",
            address = "Duplicate street 2",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ApiEnvelope<Guid?> envelope = await ReadEnvelopeAsync<Guid?>(response);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "record.already.exist");
    }

    [Fact]
    public async Task DeleteLocation_RemovesLocationAndDepartmentLinks()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");
        Guid locationId = await CreateLocationAsync("Linked Office", "Linked street 1");
        Guid departmentId = await CreateDepartmentAsync(
            "Linked Department",
            "linkeddept",
            chiefId,
            locationsId: locationId);

        HttpResponseMessage response = await Client.DeleteAsync($"/locations/{locationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope<bool?> envelope = await ReadEnvelopeAsync<bool?>(response);
        Assert.False(envelope.IsError);
        Assert.True(envelope.Result == true);

        Assert.Equal(1, await CountRowsAsync("locations", "id", locationId));
        Assert.True(await IsSoftDeletedAsync("locations", "id", locationId));
        Assert.Equal(0, await CountRowsAsync("department_locations", "location_id", locationId));
        Assert.Equal(1, await CountRowsAsync("departments", "id", departmentId));

        HttpResponseMessage listResponse = await Client.GetAsync("/locations?search=Linked%20Office");
        ApiEnvelope<PagedResult<LocationListItemDto>> listEnvelope =
            await ReadEnvelopeAsync<PagedResult<LocationListItemDto>>(listResponse);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(listEnvelope.Result);
        Assert.Equal(0L, listEnvelope.Result.TotalCount);

        Guid recreatedLocationId = await CreateLocationAsync("Linked Office", "Linked street 1");
        Assert.NotEqual(locationId, recreatedLocationId);
    }

    [Fact]
    public async Task ListLocations_FiltersSortsAndPaginates()
    {
        Guid chiefId = await CreatePositionAsync("Director", "director");

        Guid firstLocationId = await CreateLocationAsync("Alpha Office", "Alpha street 1");
        Guid secondLocationId = await CreateLocationAsync("Beta Office", "Beta street 2");
        Guid thirdLocationId = await CreateLocationAsync("Gamma Office", "Gamma street 3");

        await CreateDepartmentAsync("Dept One", "deptone", chiefId, locationsId: firstLocationId);
        await CreateDepartmentAsync("Dept Two", "depttwo", chiefId, locationsId: firstLocationId);
        await CreateDepartmentAsync("Dept Three", "deptthree", chiefId, locationsId: secondLocationId);

        HttpResponseMessage response = await Client.GetAsync(
            "/locations?minDepartmentCount=1&sortBy=departmentCount&sortDir=desc&page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope<PagedResult<LocationListItemDto>> envelope =
            await ReadEnvelopeAsync<PagedResult<LocationListItemDto>>(response);

        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2L, envelope.Result.TotalCount);
        Assert.Equal(2, envelope.Result.Items.Count);
        Assert.Equal(firstLocationId, envelope.Result.Items[0].Id);
        Assert.Equal(2, envelope.Result.Items[0].DepartmentCount);
        Assert.Equal(secondLocationId, envelope.Result.Items[1].Id);
        Assert.Equal(1, envelope.Result.Items[1].DepartmentCount);

        HttpResponseMessage searchResponse = await Client.GetAsync("/locations?search=ALPHA");
        ApiEnvelope<PagedResult<LocationListItemDto>> searchEnvelope =
            await ReadEnvelopeAsync<PagedResult<LocationListItemDto>>(searchResponse);

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.NotNull(searchEnvelope.Result);
        Assert.Equal(1L, searchEnvelope.Result.TotalCount);
        Assert.Single(searchEnvelope.Result.Items);
        Assert.Equal(firstLocationId, searchEnvelope.Result.Items[0].Id);

        HttpResponseMessage secondPageResponse = await Client.GetAsync(
            "/locations?page=2&pageSize=2&sortBy=name&sortDir=asc");
        ApiEnvelope<PagedResult<LocationListItemDto>> secondPageEnvelope =
            await ReadEnvelopeAsync<PagedResult<LocationListItemDto>>(secondPageResponse);

        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);
        Assert.NotNull(secondPageEnvelope.Result);
        Assert.Equal(3L, secondPageEnvelope.Result.TotalCount);
        Assert.Single(secondPageEnvelope.Result.Items);
        Assert.Equal(thirdLocationId, secondPageEnvelope.Result.Items[0].Id);

        HttpResponseMessage invalidResponse = await Client.GetAsync("/locations?sortBy=rawSql");
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        ApiEnvelope<PagedResult<LocationListItemDto>> invalidEnvelope =
            await ReadEnvelopeAsync<PagedResult<LocationListItemDto>>(invalidResponse);
        Assert.True(invalidEnvelope.IsError);
    }

    [Theory]
    [InlineData("/locations?page=0")]
    [InlineData("/locations?sortDir=sideways")]
    [InlineData("/locations?minDepartmentCount=-1")]
    public async Task ListLocations_InvalidParameters_ReturnBadRequest(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope<PagedResult<LocationListItemDto>> envelope =
            await ReadEnvelopeAsync<PagedResult<LocationListItemDto>>(response);

        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Errors);
        Assert.Contains(envelope.Errors, error => error.Code == "value.is.invalid");
    }
}



