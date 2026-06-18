using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.Statistics;

public record GetStatisticsByObjectIdDto(Guid objectId, int? level);
