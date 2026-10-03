using Microsoft.AspNetCore.Mvc;
using VitaSignal.Api.Contracts;
using VitaSignal.Application.VitalReadings;

namespace VitaSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class VitalReadingsController : ControllerBase
{
    private readonly RegisterVitalReadingUseCase _registerVitalReadingUseCase;
    private readonly IVitalReadingRepository _vitalReadingRepository;

    public VitalReadingsController(
        RegisterVitalReadingUseCase registerVitalReadingUseCase,
        IVitalReadingRepository vitalReadingRepository)
    {
        _registerVitalReadingUseCase = registerVitalReadingUseCase;
        _vitalReadingRepository = vitalReadingRepository;
    }

    [HttpPost]
    [ProducesResponseType<RegisterVitalReadingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegisterVitalReadingResponse>> Register(
        RegisterVitalReadingRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerVitalReadingUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Reading.Id }, result.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<VitalReadingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VitalReadingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reading = await _vitalReadingRepository.GetByIdAsync(id, cancellationToken);
        if (reading is null)
            return NotFound();

        return Ok(reading.ToResponse());
    }
}
