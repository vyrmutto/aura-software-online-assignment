namespace ClinicPOS.Application.Users.Dtos;

public record UserDto(
    Guid Id,
    string Username,
    string Role,
    Guid TenantId,
    List<Guid> BranchIds,
    DateTime CreatedAt
);
