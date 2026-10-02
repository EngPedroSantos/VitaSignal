using Microsoft.AspNetCore.Mvc;
using VitaSignal.Api.Contracts;
using VitaSignal.Application.Alerts;

namespace VitaSignal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AlertsController : ControllerBase
{
    private readonly AcknowledgeAlertUseCase _acknowledgeAlertUseCase;

    public AlertsController(AcknowledgeAlertUseCase acknowledgeAlertUseCase)
    {
        _acknowledgeAlertUseCase = acknowledgeAlertUseCase;
    }

    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType<VitalAlertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VitalAlertResponse>> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _acknowledgeAlertUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(alert.ToResponse());
    }
}
