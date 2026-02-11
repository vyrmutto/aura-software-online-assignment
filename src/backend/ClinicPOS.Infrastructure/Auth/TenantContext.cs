using ClinicPOS.Domain.Interfaces;

namespace ClinicPOS.Infrastructure.Auth;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<Guid> BranchIds { get; set; } = [];
}
