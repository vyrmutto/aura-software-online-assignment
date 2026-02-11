namespace ClinicPOS.Application.Appointments.Dtos;

public record CreateAppointmentRequest(
    Guid BranchId,
    Guid PatientId,
    DateTime StartAt
);
