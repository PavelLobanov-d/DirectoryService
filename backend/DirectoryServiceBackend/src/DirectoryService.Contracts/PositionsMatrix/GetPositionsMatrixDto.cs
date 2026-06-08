using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.PositionsMatrix;

public record GetPositionsMatrixDto(string Search, int Page, int PageSize);
