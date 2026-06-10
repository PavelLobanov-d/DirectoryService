using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Contracts.Locations;

/// <summary>
/// запрос на поиск локаций
/// </summary>
/// <param name="Search">строка параметров запроса</param>
/// <param name="Page">страница</param>
/// <param name="PageSize">размер страницы в строках</param>
public record GetLocationsDto(string Search, int Page, int PageSize);