namespace ClinicPOS.Application.Patients.Dtos;

public record PatientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid TenantId,
    Guid? PrimaryBranchId,
    DateTime CreatedAt
);
