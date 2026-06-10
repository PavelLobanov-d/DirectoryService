using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.Statistics;

public record GetStatisticsByParentIdDto(Guid parentId, int? level);

