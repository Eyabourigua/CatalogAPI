using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Tests;

public class ProductIntegrationTests
{
    [SqlServerFact]
    public async Task AddProduct_And_VerifyPersistenceInRealDatabase()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDbConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__CatalogDbConnection to run this SQL Server integration test.");
        }

        using var baseFactory = new WebApplicationFactory<Program>();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connectionString));
            });
        });

        var client = factory.CreateClient();
        var uniqueName = $"Serveur-{Guid.NewGuid():N}"[..20];
        var product = new Product { Name = uniqueName, Price = 1299.99m };

        var postResponse = await client.PostAsJsonAsync("/api/products", product);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedProduct = await db.Products
            .FirstOrDefaultAsync(existingProduct => existingProduct.Name == uniqueName);

        Assert.NotNull(persistedProduct);
        Assert.Equal(1299.99m, persistedProduct.Price);
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDbConnection")))
        {
            Skip = "Set ConnectionStrings__CatalogDbConnection to run this SQL Server integration test.";
        }
    }
}
