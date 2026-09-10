using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReconFlow.Infrastructure.Data;

public sealed class ReconFlowDbContextFactory : IDesignTimeDbContextFactory<ReconFlowDbContext>
{
    public ReconFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=reconflow;Username=reconflow;Password=reconflow-dev-password";
        var options = new DbContextOptionsBuilder<ReconFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ReconFlowDbContext(options);
    }
}
