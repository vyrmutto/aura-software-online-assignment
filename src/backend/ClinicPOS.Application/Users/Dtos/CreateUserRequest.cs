namespace ClinicPOS.Application.Users.Dtos;

public record CreateUserRequest(
    string Username,
    string Password,
    string Role,
    List<Guid> BranchIds
);
