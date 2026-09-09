using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using RequestCodingService.Infrastructure.Persistence;

namespace RequestCodingService.Infrastructure;

public sealed class RequestCodingDbContextFactory : IDesignTimeDbContextFactory<RequestCodingDbContext>
{
    public RequestCodingDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "RequestCodingService.Api");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("RequestCoding")
            ?? "Server=localhost;Database=apiweb-codingsystem;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<RequestCodingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new RequestCodingDbContext(options);
    }
}
