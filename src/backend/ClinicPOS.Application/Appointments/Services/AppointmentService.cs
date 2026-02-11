using ClinicPOS.Application.Common.Exceptions;
using ClinicPOS.Application.Interfaces;
using ClinicPOS.Application.Appointments.Dtos;
using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Events;
using ClinicPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Application.Appointments.Services;

public class AppointmentService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICacheService _cache;
    private readonly IEventPublisher _eventPublisher;

    public AppointmentService(
        IAppDbContext db,
        ITenantContext tenant,
        ICacheService cache,
        IEventPublisher eventPublisher)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
        _eventPublisher = eventPublisher;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken ct)
    {
        // Pre-check for duplicate appointment (defense in depth alongside DB constraint)
        var exists = await _db.Appointments
            .AnyAsync(a => a.PatientId == request.PatientId
                && a.BranchId == request.BranchId
                && a.StartAt == request.StartAt, ct);
        if (exists)
            throw new DuplicateAppointmentException();

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            BranchId = request.BranchId,
            PatientId = request.PatientId,
            StartAt = request.StartAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateAppointmentException();
        }

        await _cache.RemoveByPrefixAsync($"tenant:{_tenant.TenantId}:appointments:", ct);

        var evt = new AppointmentCreatedEvent(
            EventId: Guid.NewGuid(),
            EventType: "AppointmentCreated",
            TenantId: appointment.TenantId,
            AppointmentId: appointment.Id,
            BranchId: appointment.BranchId,
            PatientId: appointment.PatientId,
            StartAt: appointment.StartAt,
            OccurredAt: appointment.CreatedAt
        );

        await _eventPublisher.PublishAsync("appointment.created", evt, ct);

        return MapToDto(appointment);
    }

    private static AppointmentDto MapToDto(Appointment a) =>
        new(a.Id, a.TenantId, a.BranchId, a.PatientId, a.StartAt, a.CreatedAt);

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        return inner != null && inner.GetType().Name == "PostgresException"
            && inner.GetType().GetProperty("SqlState")?.GetValue(inner)?.ToString() == "23505";
    }
}
