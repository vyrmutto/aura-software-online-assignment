using ClinicPOS.Application.Patients.Dtos;
using ClinicPOS.Application.Patients.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly PatientService _patientService;
    private readonly IValidator<CreatePatientRequest> _validator;

    public PatientsController(PatientService patientService, IValidator<CreatePatientRequest> validator)
    {
        _patientService = patientService;
        _validator = validator;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreatePatient")]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var result = await _patientService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "tenantId")] Guid tenantId,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _patientService.ListAsync(tenantId, branchId, page, pageSize, ct);
        return Ok(result);
    }
}
