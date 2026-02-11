using ClinicPOS.Application.Branches.Dtos;
using ClinicPOS.Application.Interfaces;
using ClinicPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Application.Branches.Services;

public class BranchService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICacheService _cache;

    public BranchService(IAppDbContext db, ITenantContext tenant, ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<List<BranchDto>> ListAsync(CancellationToken ct)
    {
        var cacheKey = $"tenant:{_tenant.TenantId}:branches";

        var cached = await _cache.GetAsync<List<BranchDto>>(cacheKey, ct);
        if (cached != null) return cached;

        var branches = await _db.Branches
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(b.Id, b.Name))
            .ToListAsync(ct);

        await _cache.SetAsync(cacheKey, branches, TimeSpan.FromMinutes(10), ct);

        return branches;
    }
}
