using ClinicPOS.Application.Appointments.Dtos;
using ClinicPOS.Application.Appointments.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly IValidator<CreateAppointmentRequest> _validator;

    public AppointmentsController(
        AppointmentService appointmentService,
        IValidator<CreateAppointmentRequest> validator)
    {
        _appointmentService = appointmentService;
        _validator = validator;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateAppointment")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var result = await _appointmentService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
