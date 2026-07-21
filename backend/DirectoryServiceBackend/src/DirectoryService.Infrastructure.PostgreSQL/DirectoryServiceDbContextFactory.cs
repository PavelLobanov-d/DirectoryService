using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace DirectoryService.Infrastructure.PostgreSQL;

public class DirectoryServiceDbContextFactory : IDesignTimeDbContextFactory<DirectoryServiceDbContext>
{
    public DirectoryServiceDbContext CreateDbContext(string[] args)
    {
        DotEnv.Load();
        string? connectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException("\"DIRECTORY_SERVICE_CONNECTIONSTRING\"");
        }
        var optionsBuilder = new DbContextOptionsBuilder<DirectoryServiceDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DirectoryServiceDbContext(optionsBuilder.Options);
    }
}
