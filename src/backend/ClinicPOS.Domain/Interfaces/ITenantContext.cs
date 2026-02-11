namespace ClinicPOS.Domain.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string Role { get; }
    IReadOnlyList<Guid> BranchIds { get; }
}
