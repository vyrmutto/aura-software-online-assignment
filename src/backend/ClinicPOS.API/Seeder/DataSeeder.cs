using ClinicPOS.Domain.Entities;
using ClinicPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.API.Seeder;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Idempotent: check if tenant already exists
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync())
            return;

        var tenantId = Guid.NewGuid();
        var branchSukhumvitId = Guid.NewGuid();
        var branchSilomId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Clinic Siam",
            CreatedAt = DateTime.UtcNow
        };

        var branchSukhumvit = new Branch
        {
            Id = branchSukhumvitId,
            Name = "Branch Sukhumvit",
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var branchSilom = new Branch
        {
            Id = branchSilomId,
            Name = "Branch Silom",
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var adminId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var viewer1Id = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Admin",
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var user1 = new User
        {
            Id = user1Id,
            Username = "user1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "User",
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var viewer1 = new User
        {
            Id = viewer1Id,
            Username = "viewer1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Viewer",
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        db.Tenants.Add(tenant);
        db.Branches.AddRange(branchSukhumvit, branchSilom);
        db.Users.AddRange(admin, user1, viewer1);

        db.UserBranches.AddRange(
            new UserBranch { UserId = adminId, BranchId = branchSukhumvitId },
            new UserBranch { UserId = adminId, BranchId = branchSilomId },
            new UserBranch { UserId = user1Id, BranchId = branchSukhumvitId },
            new UserBranch { UserId = viewer1Id, BranchId = branchSilomId }
        );

        // Sample patients
        db.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = "Somchai",
                LastName = "Jaidee",
                PhoneNumber = "0812345678",
                TenantId = tenantId,
                PrimaryBranchId = branchSukhumvitId,
                CreatedAt = DateTime.UtcNow
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = "Narin",
                LastName = "Pongpat",
                PhoneNumber = "0898765432",
                TenantId = tenantId,
                PrimaryBranchId = branchSilomId,
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }
}
