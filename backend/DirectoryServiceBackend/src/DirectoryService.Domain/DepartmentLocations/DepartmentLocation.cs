using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DirectoryService.Domain.DepartmentLocations;

public sealed class DepartmentLocation
{
    private DepartmentLocation(
        DepartmentLocationId id, 
        DepartmentId departmentId, 
        LocationId locationId)
    {
        Id = id;
        DepartmentId = departmentId;
        LocationId = locationId;
    }
    public DepartmentLocationId Id { get; private set; }
    public DepartmentId DepartmentId { get; private set; }
    private readonly Department _department = null!;
    public Department Department => _department;
    public LocationId LocationId { get; private set; }
    private readonly Location _location = null!;
    public Location Location => _location;

    public static DepartmentLocation Create(
        Department department, 
        Location location)
    {
        return new DepartmentLocation(department, location);
    }

    public DepartmentLocation(
        Department department, 
        Location location)
    {
        Id = new DepartmentLocationId(Guid.CreateVersion7());
        _department = department;
        _location = location;
        DepartmentId = department.Id;
        LocationId = location.Id;
    }
}
