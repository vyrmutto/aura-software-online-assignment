namespace ClinicPOS.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid? PrimaryBranchId { get; set; }
    public Branch? PrimaryBranch { get; set; }
    public DateTime CreatedAt { get; set; }
}
