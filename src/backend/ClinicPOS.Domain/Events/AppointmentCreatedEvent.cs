namespace ClinicPOS.Domain.Events;

public record AppointmentCreatedEvent(
    Guid EventId,
    string EventType,
    Guid TenantId,
    Guid AppointmentId,
    Guid BranchId,
    Guid PatientId,
    DateTime StartAt,
    DateTime OccurredAt
);
