using Microsoft.AspNetCore.Mvc;
using VitaSignal.Application.Patients;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.Patients;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PatientsController : ControllerBase
{
    private readonly RegisterPatientUseCase _registerPatientUseCase;
    private readonly IVitalReadingRepository _vitalReadingRepository;
    private readonly IPatientRepository _patientRepository;

    public PatientsController(
        RegisterPatientUseCase registerPatientUseCase,
        IVitalReadingRepository vitalReadingRepository, 
        IPatientRepository patientRepository)
    {
        _registerPatientUseCase = registerPatientUseCase;
        _vitalReadingRepository = vitalReadingRepository;
        _patientRepository = patientRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Patient>> Register(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await _registerPatientUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpGet("{id:guid}/readings")]
    public async Task<ActionResult<IReadOnlyList<VitalReading>>> GetReadings(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
            return NotFound();

        var readings = await _vitalReadingRepository.GetByPatientIdAsync(id, cancellationToken);
        return Ok(readings);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Patient>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
            return NotFound();

        return Ok(patient);
    }
}