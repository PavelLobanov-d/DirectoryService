using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DirectoryService.Domain.DepartmentPositions;

public sealed class DepartmentPosition
{
    private DepartmentPosition(
        DepartmentPositionId id, 
        DepartmentId departmentId, 
        PositionMatrixId positionMatrixId)
    {
        Id = id;
        DepartmentId = departmentId;
        PositionMatrixId = positionMatrixId;
    }
    public DepartmentPositionId Id { get; private set; }
    public DepartmentId DepartmentId { get; private set; }
    public PositionMatrixId PositionMatrixId { get; private set; }

    private readonly PositionMatrix _positionMatrix = null!;

    public PositionMatrix PositionMatrix => _positionMatrix;

    private readonly Department _department = null!;

    public Department Department => _department;

    public static DepartmentPosition Create(
        Department department, 
        PositionMatrix positionMatrix)
    {
        return new DepartmentPosition(department, positionMatrix);
    }

    public DepartmentPosition(
        Department department, 
        PositionMatrix positionMatrix)
    {
        Id = new DepartmentPositionId(Guid.CreateVersion7());
        _department = department;
        _positionMatrix = positionMatrix;
        DepartmentId = department.Id;
        PositionMatrixId = positionMatrix.Id;
    }
}
