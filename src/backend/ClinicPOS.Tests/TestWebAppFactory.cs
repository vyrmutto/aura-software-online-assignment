using ClinicPOS.Application.Interfaces;
using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Interfaces;
using ClinicPOS.Infrastructure.Auth;
using ClinicPOS.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ClinicPOS.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Suppress noisy logs so test names are visible in output
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
            logging.AddFilter("Microsoft.Hosting", LogLevel.None);
            logging.AddFilter("ClinicPOS.API.Middleware", LogLevel.None);
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to avoid dual-provider conflict
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Remove Redis
            var redisDescriptors = services
                .Where(d => d.ServiceType == typeof(IConnectionMultiplexer))
                .ToList();
            foreach (var d in redisDescriptors)
                services.Remove(d);

            // Remove cache service registrations
            var cacheDescriptors = services
                .Where(d => d.ServiceType == typeof(ICacheService))
                .ToList();
            foreach (var d in cacheDescriptors)
                services.Remove(d);

            // Remove event publisher
            var publisherDescriptors = services
                .Where(d => d.ServiceType == typeof(IEventPublisher))
                .ToList();
            foreach (var d in publisherDescriptors)
                services.Remove(d);

            // Add InMemory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Add no-op replacements
            services.AddScoped<ICacheService, NoOpCacheService>();
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();

            // Build an intermediate service provider to ensure DB is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task<(Guid TenantAId, Guid TenantBId, string AdminTokenA, string ViewerTokenA, string AdminTokenB)> SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwt = scope.ServiceProvider.GetRequiredService<JwtTokenService>();

        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();

        db.Tenants.AddRange(
            new Tenant { Id = tenantAId, Name = "Tenant A", CreatedAt = DateTime.UtcNow },
            new Tenant { Id = tenantBId, Name = "Tenant B", CreatedAt = DateTime.UtcNow }
        );

        db.Branches.AddRange(
            new Branch { Id = branchAId, Name = "Branch A", TenantId = tenantAId, CreatedAt = DateTime.UtcNow },
            new Branch { Id = branchBId, Name = "Branch B", TenantId = tenantBId, CreatedAt = DateTime.UtcNow }
        );

        var adminA = new User
        {
            Id = Guid.NewGuid(),
            Username = $"admin_a_{Guid.NewGuid():N}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Admin",
            TenantId = tenantAId,
            CreatedAt = DateTime.UtcNow
        };
        adminA.UserBranches.Add(new UserBranch { UserId = adminA.Id, BranchId = branchAId });

        var viewerA = new User
        {
            Id = Guid.NewGuid(),
            Username = $"viewer_a_{Guid.NewGuid():N}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Viewer",
            TenantId = tenantAId,
            CreatedAt = DateTime.UtcNow
        };
        viewerA.UserBranches.Add(new UserBranch { UserId = viewerA.Id, BranchId = branchAId });

        var adminB = new User
        {
            Id = Guid.NewGuid(),
            Username = $"admin_b_{Guid.NewGuid():N}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Admin",
            TenantId = tenantBId,
            CreatedAt = DateTime.UtcNow
        };
        adminB.UserBranches.Add(new UserBranch { UserId = adminB.Id, BranchId = branchBId });

        db.Users.AddRange(adminA, viewerA, adminB);
        await db.SaveChangesAsync();

        var adminTokenA = jwt.GenerateToken(adminA);
        var viewerTokenA = jwt.GenerateToken(viewerA);
        var adminTokenB = jwt.GenerateToken(adminB);

        return (tenantAId, tenantBId, adminTokenA, viewerTokenA, adminTokenB);
    }
}

public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) =>
        Task.CompletedTask;
}

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) =>
        Task.CompletedTask;
}
