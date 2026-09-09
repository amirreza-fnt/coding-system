using Microsoft.AspNetCore.Mvc;
using RequestCodingService.Application.Dtos;
using RequestCodingService.Application.Interfaces;

namespace RequestCodingService.Api.Controllers;

[ApiController]
[Route("api/v1/systems")]
[Produces("application/json")]
public sealed class SystemsController : ControllerBase
{
    private readonly ITrackingCodingService _service;

    public SystemsController(ITrackingCodingService service)
    {
        _service = service;
    }

    /// <summary>Lists active systems (137, fire department, ...).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SystemDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemDefinitionDto>>> GetSystems(CancellationToken ct)
    {
        var systems = await _service.GetSystemsAsync(ct);
        return Ok(systems);
    }
}
