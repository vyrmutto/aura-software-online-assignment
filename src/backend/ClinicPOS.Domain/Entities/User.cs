namespace ClinicPOS.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
}
