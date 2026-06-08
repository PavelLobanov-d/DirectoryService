using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.Departments;

public record GetDepartmentsDto(string Search, int Page, int PageSize);
