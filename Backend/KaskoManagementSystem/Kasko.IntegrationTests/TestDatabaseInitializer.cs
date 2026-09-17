using Kasko.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Kasko.IntegrationTests;

internal static class TestDatabaseInitializer
{
    public const string ConnectionString =
        "Server=.;Database=KaskoManagementDb_Test;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=60;";

    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);

        var options = new DbContextOptionsBuilder<KaskoContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new KaskoContext(options);
        context.Database.Migrate();
    }
}
