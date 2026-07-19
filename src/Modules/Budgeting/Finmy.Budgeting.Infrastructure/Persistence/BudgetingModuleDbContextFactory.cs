using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Finmy.Budgeting.Infrastructure.Persistence;

public sealed class BudgetingModuleDbContextFactory : IDesignTimeDbContextFactory<BudgetingModuleDbContext>
{
    public BudgetingModuleDbContext CreateDbContext(string[] args)
    {
        // UserSecretID configure at Finmy.Budgeting.Infrastructure.csproj (<PropertyGroup>)
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BudgetingModuleDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();

        string? connectionString = configuration.GetConnectionString("BudgetingDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'BudgetingDb' is not configured.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BudgetingModuleDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new BudgetingModuleDbContext(optionsBuilder.Options);
    }
}
