using ClinicPOS.Application.Common.Exceptions;
using ClinicPOS.Application.Interfaces;
using ClinicPOS.Application.Patients.Dtos;
using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Application.Patients.Services;

public class PatientService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICacheService _cache;

    public PatientService(IAppDbContext db, ITenantContext tenant, ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken ct)
    {
        // Pre-check for duplicate phone (defense in depth alongside DB constraint)
        var exists = await _db.Patients
            .AnyAsync(p => p.PhoneNumber == request.PhoneNumber, ct);
        if (exists)
            throw new DuplicatePhoneException(request.PhoneNumber);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            TenantId = _tenant.TenantId,
            PrimaryBranchId = request.PrimaryBranchId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Patients.Add(patient);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicatePhoneException(request.PhoneNumber);
        }

        await _cache.RemoveByPrefixAsync($"tenant:{_tenant.TenantId}:patients:", ct);

        return MapToDto(patient);
    }

    public async Task<PatientListResponse> ListAsync(Guid tenantId, Guid? branchId, int page, int pageSize, CancellationToken ct)
    {
        // Two-layer protection: validate query param tenantId matches JWT tenant
        if (tenantId != _tenant.TenantId)
            throw new ForbiddenException("Tenant ID mismatch");

        var cacheKey = $"tenant:{_tenant.TenantId}:patients:branch:{branchId?.ToString() ?? "all"}:p:{page}:s:{pageSize}";

        var cached = await _cache.GetAsync<PatientListResponse>(cacheKey, ct);
        if (cached != null) return cached;

        var query = _db.Patients.AsQueryable();

        if (branchId.HasValue)
            query = query.Where(p => p.PrimaryBranchId == branchId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientDto(
                p.Id, p.FirstName, p.LastName, p.PhoneNumber,
                p.TenantId, p.PrimaryBranchId, p.CreatedAt))
            .ToListAsync(ct);

        var result = new PatientListResponse(items, page, pageSize, totalCount);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }

    private static PatientDto MapToDto(Patient p) =>
        new(p.Id, p.FirstName, p.LastName, p.PhoneNumber, p.TenantId, p.PrimaryBranchId, p.CreatedAt);

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Npgsql PostgresException SqlState "23505" = unique_violation
        var inner = ex.InnerException;
        return inner != null && inner.GetType().Name == "PostgresException"
            && inner.GetType().GetProperty("SqlState")?.GetValue(inner)?.ToString() == "23505";
    }
}
