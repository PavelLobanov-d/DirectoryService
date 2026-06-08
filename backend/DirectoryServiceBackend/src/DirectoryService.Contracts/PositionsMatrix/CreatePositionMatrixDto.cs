using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.PositionsMatrix;

public record CreatePositionMatrixDto(string Name, string Slug, Guid? ParentPositionMatrixId);