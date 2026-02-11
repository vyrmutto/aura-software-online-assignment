using ClinicPOS.Application.Auth.Dtos;
using ClinicPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Auth;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthService(IAppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        // Login bypasses tenant filter - need to query across tenants
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = _jwt.GenerateToken(user);
        var branchIds = user.UserBranches.Select(ub => ub.BranchId).ToList();

        return new LoginResponse(
            token,
            new UserInfo(user.Id, user.Username, user.Role, user.TenantId, branchIds)
        );
    }
}
