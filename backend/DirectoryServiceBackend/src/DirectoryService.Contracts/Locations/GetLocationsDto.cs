using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.Locations;

public record GetLocationsDto(string Search, int Page, int PageSize);