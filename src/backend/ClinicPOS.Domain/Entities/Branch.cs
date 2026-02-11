namespace ClinicPOS.Domain.Entities;

public class Branch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
