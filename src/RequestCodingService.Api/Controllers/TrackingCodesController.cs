using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RequestCodingService.Application.Dtos;
using RequestCodingService.Application.Interfaces;
using RequestCodingService.Application.Options;

namespace RequestCodingService.Api.Controllers;

[ApiController]
[Route("api/v1/tracking-codes")]
[Produces("application/json")]
public sealed class TrackingCodesController : ControllerBase
{
    private readonly ITrackingCodingService _service;
    private readonly IOptions<InternalAuthOptions> _internalAuth;

    public TrackingCodesController(
        ITrackingCodingService service,
        IOptions<InternalAuthOptions> internalAuth)
    {
        _service = service;
        _internalAuth = internalAuth;
    }

    /// <summary>
    /// Allocates a new 5-digit tracking code for a request.
    /// Requires operator SSO token or internal X-Api-Key.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTrackingResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateTrackingResponseDto>> Create(
        [FromBody] CreateTrackingRequestDto request,
        CancellationToken ct)
    {
        var response = await _service.CreateAsync(request, AuthHeader(), ApiKeyHeader(), ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Gets a request by internal id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrackingRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackingRequestDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var response = await _service.GetByIdAsync(id, AuthHeader(), ApiKeyHeader(), ct);
        return Ok(response);
    }

    /// <summary>
    /// Gets a request by system + 5-digit tracking code.
    /// Citizens: national code comes from SSO token.
    /// Operators/internal: pass nationalCode query parameter.
    /// </summary>
    [HttpGet("by-code/{systemId:int}/{counter:int}")]
    [ProducesResponseType(typeof(TrackingRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackingRequestDetailDto>> GetByCode(
        int systemId,
        int counter,
        [FromQuery] string? nationalCode,
        CancellationToken ct)
    {
        var response = await _service.GetByTrackingCodeAsync(
            systemId, counter, AuthHeader(), ApiKeyHeader(), nationalCode, ct);
        return Ok(response);
    }

    /// <summary>
    /// Operator search: combine tracking code with name, phone, date filters.
    /// Returns at most 200 results.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackingRequestDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrackingRequestDetailDto>>> Search(
        [FromQuery] OperatorSearchQueryDto query,
        CancellationToken ct)
    {
        var response = await _service.SearchOperatorAsync(query, AuthHeader(), ApiKeyHeader(), ct);
        return Ok(response);
    }

    /// <summary>Lists the authenticated citizen's own requests (national code from SSO).</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackingRequestDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrackingRequestDetailDto>>> GetMyRequests(
        [FromQuery] int? systemId,
        CancellationToken ct)
    {
        var auth = AuthHeader()
            ?? throw new Application.Exceptions.NotAuthenticatedException("Bearer token is required.");
        var response = await _service.GetMyRequestsAsync(auth, systemId, ct);
        return Ok(response);
    }

    /// <summary>Soft-deletes a request. The tracking code is never reused.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.SoftDeleteAsync(id, AuthHeader(), ApiKeyHeader(), ct);
        return NoContent();
    }

    private string? AuthHeader()
    {
        var value = Request.Headers.Authorization.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private string? ApiKeyHeader()
    {
        var value = Request.Headers[_internalAuth.Value.HeaderName].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
