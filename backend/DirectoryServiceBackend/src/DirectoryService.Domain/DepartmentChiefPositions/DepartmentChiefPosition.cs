using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.DepartmentChiefPositions;

public sealed class DepartmentChiefPosition
{
    private DepartmentChiefPosition() { }
    public DepartmentChiefPosition(
        Department department,
        PositionMatrix positionMatrix)
    {
        Department = department;
        DepartmentId = department.Id;
        PositionMatrix = positionMatrix;
        PositionMatrixId = positionMatrix.Id;
    }
    public DepartmentChiefPosition(
        DepartmentId departmentId,
        PositionMatrixId positionMatrixId)
    {
        DepartmentId = departmentId;
        PositionMatrixId = positionMatrixId;
    }
    public DepartmentChiefPosition(
        DepartmentId departmentId,
        PositionMatrix positionMatrix)
    {
        PositionMatrix = positionMatrix;
        DepartmentId = departmentId;
        PositionMatrixId = PositionMatrix.Id;
    }
    public DepartmentId DepartmentId { get; private set; } = null!;
    public PositionMatrixId PositionMatrixId { get; private set; } = null!;
    public PositionMatrix PositionMatrix { get; private set; } = null!;
    public Department Department { get; private set; } = null!;

    public static DepartmentChiefPosition Create(
        DepartmentId departmentId,
        PositionMatrix positionMatrix)
    {
        return new DepartmentChiefPosition(departmentId, positionMatrix);
    }
}
