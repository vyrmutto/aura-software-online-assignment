using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Domain.Interfaces;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Patient> Patients { get; }
    DbSet<User> Users { get; }
    DbSet<UserBranch> UserBranches { get; }
    DbSet<Appointment> Appointments { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
