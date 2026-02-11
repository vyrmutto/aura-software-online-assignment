namespace ClinicPOS.Application.Auth.Dtos;

public record LoginResponse(string Token, UserInfo User);

public record UserInfo(
    Guid Id,
    string Username,
    string Role,
    Guid TenantId,
    List<Guid> BranchIds
);
