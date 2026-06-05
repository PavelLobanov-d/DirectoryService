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
    protected internal DepartmentChiefPosition(
        DepartmentId departmentId,
        PositionMatrixId positionMatrixId)
    {
        DepartmentId = departmentId;
        PositionMatrixId = positionMatrixId;
    }
    public DepartmentId DepartmentId { get; private set; }
    public PositionMatrixId PositionMatrixId { get; private set; }

    public PositionMatrix PositionMatrix { get; private set; }

    public Department Department { get; private set; }

    public static DepartmentChiefPosition Create(
        DepartmentId departmentId,
        PositionMatrix positionMatrix)
    {
        return new DepartmentChiefPosition(departmentId, positionMatrix);
    }

    public static DepartmentChiefPosition Create(
        Department department,
        PositionMatrix positionMatrix)
    {
        return new DepartmentChiefPosition(department, positionMatrix);
    }

    private DepartmentChiefPosition(
        Department department,
        PositionMatrix positionMatrix)
    {
        Department = department;
        PositionMatrix = positionMatrix;
        DepartmentId = Department.Id;
        PositionMatrixId = PositionMatrix.Id;
    }
    private DepartmentChiefPosition(
        DepartmentId departmentId,
        PositionMatrix positionMatrix)
    {
        PositionMatrix = positionMatrix;
        DepartmentId = departmentId;
        PositionMatrixId = PositionMatrix.Id;
    }
}
