using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Database
{
    public interface IDbConnectionFactory
    {
        public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    }
}
