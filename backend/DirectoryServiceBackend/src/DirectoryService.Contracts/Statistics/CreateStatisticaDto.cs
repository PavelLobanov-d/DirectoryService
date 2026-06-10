using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.Statistics;

public record CreateStatisticaDto(
        Guid objectId,
        string objectTypeName,
        int level,
        int action,
        string description,
        Guid? parentId,
        string? parentTypeName);