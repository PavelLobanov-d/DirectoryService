using DirectoryService.Core.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly)
            .AddScoped<ILocationsService, LocationsService>();
    }
}
