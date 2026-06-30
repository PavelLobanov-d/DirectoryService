using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts;

/// <summary>
/// запрос на поиск звписей
/// </summary>
/// <param name="Search">строка параметров запроса</param>
/// <param name="Page">страница</param>
/// <param name="PageSize">размер страницы в строках</param>
public record SelectDto(string? Search, int Page, int PageSize);