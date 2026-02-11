using System.IdentityModel.Tokens.Jwt;
using ClinicPOS.Infrastructure.Auth;

namespace ClinicPOS.API.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claims = context.User.Claims.ToList();

            var subClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)
                ?? claims.FirstOrDefault(c => c.Type == "sub");
            if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
                tenantContext.UserId = userId;

            var tenantClaim = claims.FirstOrDefault(c => c.Type == "tenant_id");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
                tenantContext.TenantId = tenantId;

            var roleClaim = claims.FirstOrDefault(c => c.Type == "role");
            if (roleClaim != null)
                tenantContext.Role = roleClaim.Value;

            var branchClaim = claims.FirstOrDefault(c => c.Type == "branch_ids");
            if (branchClaim != null && !string.IsNullOrEmpty(branchClaim.Value))
            {
                tenantContext.BranchIds = branchClaim.Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => Guid.TryParse(s, out _))
                    .Select(Guid.Parse)
                    .ToList();
            }
        }

        await _next(context);
    }
}
