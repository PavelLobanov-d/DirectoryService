using DirectoryService.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations
{
    internal class DepartmentConfiguration: IEntityTypeConfiguration <Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {

        }
    }
}
