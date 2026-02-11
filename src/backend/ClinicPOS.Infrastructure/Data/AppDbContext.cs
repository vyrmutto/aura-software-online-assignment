using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tenant
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // Branch
        modelBuilder.Entity<Branch>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).IsRequired().HasMaxLength(200);
            e.Property(b => b.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(b => b.Tenant).WithMany(t => t.Branches).HasForeignKey(b => b.TenantId);
            e.HasIndex(b => b.TenantId).HasDatabaseName("ix_branches_tenant_id");
            e.HasQueryFilter(b => _tenantContext == null || b.TenantId == _tenantContext.TenantId);
        });

        // Patient
        modelBuilder.Entity<Patient>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            e.Property(p => p.LastName).IsRequired().HasMaxLength(100);
            e.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(p => p.Tenant).WithMany().HasForeignKey(p => p.TenantId);
            e.HasOne(p => p.PrimaryBranch).WithMany().HasForeignKey(p => p.PrimaryBranchId);
            e.HasIndex(p => new { p.TenantId, p.PhoneNumber }).IsUnique()
                .HasDatabaseName("uq_patients_tenant_phone");
            e.HasIndex(p => new { p.TenantId, p.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("ix_patients_tenant_created");
            e.HasQueryFilter(p => _tenantContext == null || p.TenantId == _tenantContext.TenantId);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(u => u.Tenant).WithMany().HasForeignKey(u => u.TenantId);
            e.HasQueryFilter(u => _tenantContext == null || u.TenantId == _tenantContext.TenantId);
        });

        // UserBranch (many-to-many join)
        modelBuilder.Entity<UserBranch>(e =>
        {
            e.HasKey(ub => new { ub.UserId, ub.BranchId });
            e.HasOne(ub => ub.User).WithMany(u => u.UserBranches).HasForeignKey(ub => ub.UserId);
            e.HasOne(ub => ub.Branch).WithMany().HasForeignKey(ub => ub.BranchId);
        });

        // Appointment
        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(a => a.Tenant).WithMany().HasForeignKey(a => a.TenantId);
            e.HasOne(a => a.Branch).WithMany().HasForeignKey(a => a.BranchId);
            e.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId);
            e.HasIndex(a => new { a.TenantId, a.PatientId, a.BranchId, a.StartAt }).IsUnique()
                .HasDatabaseName("uq_appointments_tenant_patient_branch_start");
            e.HasIndex(a => new { a.TenantId, a.BranchId })
                .HasDatabaseName("ix_appointments_tenant_branch");
            e.HasQueryFilter(a => _tenantContext == null || a.TenantId == _tenantContext.TenantId);
        });
    }
}
