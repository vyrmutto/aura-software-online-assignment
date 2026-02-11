namespace ClinicPOS.Application.Appointments.Dtos;

public record AppointmentDto(
    Guid Id,
    Guid TenantId,
    Guid BranchId,
    Guid PatientId,
    DateTime StartAt,
    DateTime CreatedAt
);
