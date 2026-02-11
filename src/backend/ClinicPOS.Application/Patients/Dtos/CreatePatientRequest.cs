namespace ClinicPOS.Application.Patients.Dtos;

public record CreatePatientRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid? PrimaryBranchId
);
