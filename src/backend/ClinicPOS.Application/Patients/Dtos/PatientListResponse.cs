namespace ClinicPOS.Application.Patients.Dtos;

public record PatientListResponse(
    List<PatientDto> Items,
    int Page,
    int PageSize,
    int TotalCount
);
