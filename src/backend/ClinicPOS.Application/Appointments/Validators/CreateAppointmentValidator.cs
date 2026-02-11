using ClinicPOS.Application.Appointments.Dtos;
using FluentValidation;

namespace ClinicPOS.Application.Appointments.Validators;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.StartAt).NotEmpty().GreaterThan(DateTime.UtcNow);
    }
}
