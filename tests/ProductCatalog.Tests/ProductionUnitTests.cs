
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Tests;

public class ProductUnitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductUnitTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = $"UnitTestingDb_{Guid.NewGuid()}";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the original SQL Server configuration
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                // Register a fresh InMemory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
    }

    [Fact]
    public async Task AddProduct_ReturnsCreated_WhenProductIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();

        var product = new Product
        {
            Name = "Clavier Mecanique",
            Price = 89.50m
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/products", product);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdProduct =
            await response.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(createdProduct);
        Assert.Equal("Clavier Mecanique", createdProduct.Name);
        Assert.Equal(89.50m, createdProduct.Price);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOkAndProductList()
    {
        // Arrange
        var client = _factory.CreateClient();

        var product = new Product
        {
            Name = "Ecran 27 pouces",
            Price = 250.00m
        };

        // Act: Add product
        var postResponse = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var createdProduct =
            await postResponse.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(createdProduct);

        // Act: Get all products
        var getResponse = await client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var products =
            await getResponse.Content.ReadFromJsonAsync<List<Product>>();

        Assert.NotNull(products);

        Assert.Contains(products, p =>
            p.Id == createdProduct.Id &&
            p.Name == "Ecran 27 pouces" &&
            p.Price == 250.00m);
    }
}
