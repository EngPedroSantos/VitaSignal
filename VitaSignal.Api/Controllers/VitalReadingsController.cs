using Microsoft.AspNetCore.Mvc;
using VitaSignal.Application.VitalReadings;

namespace VitaSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VitalReadingsController : ControllerBase
{
    private readonly RegisterVitalReadingUseCase _registerVitalReadingUseCase;

    public VitalReadingsController(RegisterVitalReadingUseCase registerVitalReadingUseCase)
    {
        _registerVitalReadingUseCase = registerVitalReadingUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<RegisterVitalReadingResult>> Register(RegisterVitalReadingRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerVitalReadingUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(result);
    }
}