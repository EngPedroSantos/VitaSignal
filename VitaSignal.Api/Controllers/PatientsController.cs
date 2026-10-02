using Microsoft.AspNetCore.Mvc;
using VitaSignal.Api.Contracts;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Patients;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PatientsController : ControllerBase
{
    private readonly RegisterPatientUseCase _registerPatientUseCase;
    private readonly ListPatientReadingsUseCase _listPatientReadingsUseCase;
    private readonly ListPatientAlertsUseCase _listPatientAlertsUseCase;
    private readonly IPatientRepository _patientRepository;

    public PatientsController(
        RegisterPatientUseCase registerPatientUseCase,
        ListPatientReadingsUseCase listPatientReadingsUseCase,
        ListPatientAlertsUseCase listPatientAlertsUseCase,
        IPatientRepository patientRepository)
    {
        _registerPatientUseCase = registerPatientUseCase;
        _listPatientReadingsUseCase = listPatientReadingsUseCase;
        _listPatientAlertsUseCase = listPatientAlertsUseCase;
        _patientRepository = patientRepository;
    }

    [HttpPost]
    [ProducesResponseType<PatientResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PatientResponse>> Register(CancellationToken cancellationToken)
    {
        var patient = await _registerPatientUseCase.ExecuteAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PatientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
            return NotFound();

        return Ok(patient.ToResponse());
    }

    [HttpGet("{id:guid}/readings")]
    [ProducesResponseType<PagedResponse<VitalReadingResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<VitalReadingResponse>>> GetReadings(
        Guid id,
        [FromQuery] VitalSignType? type,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var page = await _listPatientReadingsUseCase.ExecuteAsync(id, type, cursor, limit, cancellationToken);
        return Ok(page.ToResponse(r => r.ToResponse()));
    }

    [HttpGet("{id:guid}/alerts")]
    [ProducesResponseType<PagedResponse<VitalAlertResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<VitalAlertResponse>>> GetAlerts(
        Guid id,
        [FromQuery] AlertSeverity? severity,
        [FromQuery] bool pendingOnly,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var page = await _listPatientAlertsUseCase.ExecuteAsync(id, severity, pendingOnly, cursor, limit, cancellationToken);
        return Ok(page.ToResponse(a => a.ToResponse()));
    }
}
