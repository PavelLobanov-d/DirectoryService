using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments;
using DirectoryService.Core.Departments.ChangeChiefPositionMatrix;
using DirectoryService.Core.Departments.ChangeParent;
using DirectoryService.Core.Departments.Create;
using DirectoryService.Core.Departments.Delete;
using DirectoryService.Core.Departments.DetachLocation;
using DirectoryService.Core.Departments.DetachPosition;
using DirectoryService.Core.Departments.Get;
using DirectoryService.Core.Departments.GetById;
using DirectoryService.Core.Departments.GetByParentId;
using DirectoryService.Core.Departments.LinkLocation;
using DirectoryService.Core.Departments.LinkPosition;
using DirectoryService.Core.Departments.Update;
using DirectoryService.Core.Locations;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
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
        services
            .AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly)
            .AddScoped<ILocationsService, LocationsService>()
            .AddScoped<IPositionMatrixService, PositionMatrixService>()
            .AddScoped<IDepartmentsService, DepartmentsService>()
            .AddScoped<IStatisticsService, StatisticsService>();

        services.AddScoped<IDepartmentsService, DepartmentsService>();

        // регистрация всех Handlers
        services.Scan(scan => scan.FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(ICommandHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
     /*   services
            .AddScoped<ICommandHandler<CreatedObjectGuid, CreateDepartmentCommand>, CreateDepartmentHandler>()
            .AddScoped<ICommandHandler<bool, DepartmentChangeChiefPositionMatrixCommand>, DepartmentChangeChiefPositionMatrixHandler>()
            .AddScoped<ICommandHandler<bool, ChangeParentDepartmentCommand>, ChangeParentDepartmentHandler>()
            .AddScoped<ICommandHandler<bool, DepartmentDetachLocationCommand>, DepartmentDetachLocationHandler>()
            .AddScoped<ICommandHandler<bool, DepartmentDetachPositionCommand>, DepartmentDetachPositionHandler>()
            .AddScoped<ICommandHandler<List<Department>, GetDepartmentCommand>, GetDepartmentHandler>()
            .AddScoped<ICommandHandler<Department?, GetDepartmentByIdCommand>, GetDepartmentByIdHandler>()
            .AddScoped<ICommandHandler<List<Department>, GetDepartmentByParentIdCommand>, GetDepartmentByParentIdHandler>()
            .AddScoped<ICommandHandler<CreatedObjectGuid, DepartmentLinkLocationCommand>, DepartmentLinkLocationHandler>()
            .AddScoped<ICommandHandler<CreatedObjectGuid, DepartmentLinkPositionCommand>, DepartmentLinkPositionHandler>()
            .AddScoped<ICommandHandler<bool, UpdateDepartmentCommand>, UpdateDepartmentHandler > ()
            .AddScoped<ICommandHandler<bool, DeleteDepartmentCommand>, DeleteDepartmentHandler > ();
     */
        return services;
    }
}
