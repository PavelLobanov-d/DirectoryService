using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Positions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.DepartmentPositionMatrixDir
{
    internal class DepartmentPositionMatrix
    {
        private DepartmentPositionMatrix(DepartmentPositionMatrixId id, DepartmentId departmentId, PositionMatrixId positionMatrixId)
        {
            this.id = id;
            DepartmentId = departmentId;
            PositionMatrixId = positionMatrixId;
        }
        public DepartmentPositionMatrixId id { get; private set; } = null!;
        public DepartmentId DepartmentId { get; private set; } = null!;
        public PositionMatrixId PositionMatrixId { get; private set; } = null!;

        public DepartmentPositionMatrix(DepartmentId departmentId, PositionMatrixId positionMatrixId)
        {
            if (departmentId.Value == Guid.Empty)
            {
                throw new ArgumentException("Не задан Id департамента", nameof(departmentId));
            }
            if (positionMatrixId.Value == Guid.Empty)
            {
                throw new ArgumentException("Не задан Id матричной должности", nameof(positionMatrixId));
            }
            id = new DepartmentPositionMatrixId(Guid.CreateVersion7());
            DepartmentId = departmentId;
            PositionMatrixId = positionMatrixId;
        }
    }
}
