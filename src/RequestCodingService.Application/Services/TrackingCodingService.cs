using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RequestCodingService.Application.Dtos;
using RequestCodingService.Application.Exceptions;
using RequestCodingService.Application.Interfaces;
using RequestCodingService.Application.Options;
using RequestCodingService.Domain.Entities;
using RequestCodingService.Domain.Enums;

namespace RequestCodingService.Application.Services;

public sealed class TrackingCodingService : ITrackingCodingService
{
    private readonly IValidator<CreateTrackingRequestDto> _createValidator;
    private readonly IValidator<OperatorSearchQueryDto> _searchValidator;
    private readonly ITrackingRequestRepository _repository;
    private readonly ISsoAuthClient _ssoAuthClient;
    private readonly IOptions<InternalAuthOptions> _internalAuth;
    private readonly IOptions<CodingOptions> _codingOptions;
    private readonly ILogger<TrackingCodingService> _logger;

    public TrackingCodingService(
        IValidator<CreateTrackingRequestDto> createValidator,
        IValidator<OperatorSearchQueryDto> searchValidator,
        ITrackingRequestRepository repository,
        ISsoAuthClient ssoAuthClient,
        IOptions<InternalAuthOptions> internalAuth,
        IOptions<CodingOptions> codingOptions,
        ILogger<TrackingCodingService> logger)
    {
        _createValidator = createValidator;
        _searchValidator = searchValidator;
        _repository = repository;
        _ssoAuthClient = ssoAuthClient;
        _internalAuth = internalAuth;
        _codingOptions = codingOptions;
        _logger = logger;
    }

    public async Task<CreateTrackingResponseDto> CreateAsync(
        CreateTrackingRequestDto request,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        await RequireInternalOrOperatorAsync(authorizationHeader, apiKeyHeader, ct);

        var system = await _repository.GetSystemByIdAsync(request.SystemId, ct)
            ?? throw new NotFoundException($"System '{request.SystemId}' was not found.");

        if (!system.IsActive)
        {
            throw new DomainValidationException($"System '{system.Code}' is not active.");
        }

        var now = DateTime.UtcNow;
        var entity = new TrackingRequest
        {
            Id = Guid.NewGuid(),
            SystemId = request.SystemId,
            NationalCode = request.NationalCode.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Mobile = NormalizeOptional(request.Mobile),
            Landline = NormalizeOptional(request.Landline),
            Description = NormalizeOptional(request.Description),
            Status = RequestStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var opts = _codingOptions.Value;
        var created = await _repository.CreateWithNextCounterAsync(
            entity, opts.MaxCounter, opts.InitialCounter, ct);

        _logger.LogInformation(
            "Tracking code {TrackingCode} allocated for system {SystemId}, national code {NationalCode}",
            created.TrackingCodeDisplay, created.SystemId, MaskNationalCode(created.NationalCode));

        return MapCreateResponse(created, system);
    }

    public async Task<TrackingRequestDetailDto> GetByIdAsync(
        Guid id,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct)
    {
        var caller = await ResolveCallerAsync(authorizationHeader, apiKeyHeader, ct);
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        EnsureCanView(caller, entity);
        return MapDetail(entity);
    }

    public async Task<TrackingRequestDetailDto> GetByTrackingCodeAsync(
        int systemId,
        int counter,
        string? authorizationHeader,
        string? apiKeyHeader,
        string? nationalCodeForCitizen,
        CancellationToken ct)
    {
        if (counter is < 1 or > 99999)
        {
            throw new DomainValidationException("Tracking code must be between 1 and 99999.");
        }

        var caller = await ResolveCallerAsync(authorizationHeader, apiKeyHeader, ct);

        if (caller.IsCitizen)
        {
            var nc = caller.NationalCode
                ?? throw new NotAuthorizedException("Citizen national code is not available from SSO.");
            var entity = await _repository.GetBySystemCounterAndNationalCodeAsync(systemId, counter, nc, ct)
                ?? throw new NotFoundException("Request was not found.");
            return MapDetail(entity);
        }

        if (caller.IsInternal || caller.IsOperator)
        {
            if (string.IsNullOrWhiteSpace(nationalCodeForCitizen))
            {
                throw new DomainValidationException(
                    "For operator/internal lookup, nationalCode query parameter is required with tracking code.");
            }

            var entity = await _repository.GetBySystemCounterAndNationalCodeAsync(
                systemId, counter, nationalCodeForCitizen.Trim(), ct)
                ?? throw new NotFoundException("Request was not found.");
            return MapDetail(entity);
        }

        throw new NotAuthorizedException("Access denied.");
    }

    public async Task<IReadOnlyList<TrackingRequestDetailDto>> SearchOperatorAsync(
        OperatorSearchQueryDto query,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct)
    {
        await RequireOperatorOrInternalAsync(authorizationHeader, apiKeyHeader, ct);

        var validation = await _searchValidator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var hasFilter = query.Counter.HasValue
            || !string.IsNullOrWhiteSpace(query.NationalCode)
            || !string.IsNullOrWhiteSpace(query.FirstName)
            || !string.IsNullOrWhiteSpace(query.LastName)
            || !string.IsNullOrWhiteSpace(query.Mobile)
            || !string.IsNullOrWhiteSpace(query.Landline)
            || query.FromDate.HasValue
            || query.ToDate.HasValue;

        if (!hasFilter)
        {
            throw new DomainValidationException(
                "At least one search filter is required (counter, nationalCode, name, phone, or date range).");
        }

        var results = await _repository.SearchOperatorAsync(query, ct);
        return results.Select(MapDetail).ToList();
    }

    public async Task<IReadOnlyList<TrackingRequestDetailDto>> GetMyRequestsAsync(
        string authorizationHeader,
        int? systemId,
        CancellationToken ct)
    {
        var user = await _ssoAuthClient.GetUserInfoAsync(authorizationHeader, ct);
        if (string.IsNullOrWhiteSpace(user.NationalCode))
        {
            throw new NotAuthorizedException("National code is not available from SSO.");
        }

        var results = await _repository.GetByNationalCodeAsync(user.NationalCode, systemId, ct);
        return results.Select(MapDetail).ToList();
    }

    public async Task SoftDeleteAsync(
        Guid id,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct)
    {
        await RequireInternalOrOperatorAsync(authorizationHeader, apiKeyHeader, ct);
        await _repository.SoftDeleteAsync(id, ct);
        _logger.LogInformation("Request {RequestId} soft-deleted.", id);
    }

    public async Task<IReadOnlyList<SystemDefinitionDto>> GetSystemsAsync(CancellationToken ct)
    {
        var systems = await _repository.GetActiveSystemsAsync(ct);
        return systems.Select(s => new SystemDefinitionDto(s.Id, s.Code, s.Name, s.IsActive)).ToList();
    }

    private async Task RequireInternalOrOperatorAsync(
        string? authorizationHeader, string? apiKeyHeader, CancellationToken ct)
    {
        var caller = await ResolveCallerAsync(authorizationHeader, apiKeyHeader, ct);
        if (!caller.IsInternal && !caller.IsOperator)
        {
            throw new NotAuthorizedException("Only internal services or operators can perform this action.");
        }
    }

    private async Task RequireOperatorOrInternalAsync(
        string? authorizationHeader, string? apiKeyHeader, CancellationToken ct)
    {
        var caller = await ResolveCallerAsync(authorizationHeader, apiKeyHeader, ct);
        if (!caller.IsInternal && !caller.IsOperator)
        {
            throw new NotAuthorizedException("Operator or internal access required.");
        }
    }

    private async Task<CallerContext> ResolveCallerAsync(
        string? authorizationHeader, string? apiKeyHeader, CancellationToken ct)
    {
        if (IsValidApiKey(apiKeyHeader))
        {
            return CallerContext.Internal();
        }

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            var user = await _ssoAuthClient.GetUserInfoAsync(authorizationHeader, ct);
            var isOperator = user.Roles.Any(r =>
                r.Contains("OPERATOR", StringComparison.OrdinalIgnoreCase)
                || r.Contains("ADMIN", StringComparison.OrdinalIgnoreCase)
                || r.Contains("ROLE_137", StringComparison.OrdinalIgnoreCase));

            if (isOperator)
            {
                return CallerContext.Operator(user.NationalCode);
            }

            return CallerContext.Citizen(user.NationalCode);
        }

        throw new NotAuthenticatedException("Authorization header or X-Api-Key is required.");
    }

    private bool IsValidApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        return _internalAuth.Value.ApiKeys.Any(k =>
            !string.IsNullOrWhiteSpace(k.Key)
            && string.Equals(k.Key, apiKey, StringComparison.Ordinal));
    }

    private static void EnsureCanView(CallerContext caller, TrackingRequest entity)
    {
        if (caller.IsInternal || caller.IsOperator)
        {
            return;
        }

        if (caller.IsCitizen
            && !string.IsNullOrWhiteSpace(caller.NationalCode)
            && string.Equals(caller.NationalCode, entity.NationalCode, StringComparison.Ordinal))
        {
            return;
        }

        throw new NotAuthorizedException("You are not allowed to view this request.");
    }

    private static CreateTrackingResponseDto MapCreateResponse(TrackingRequest entity, SystemDefinition system)
        => new(
            entity.Id,
            entity.SystemId,
            system.Code,
            system.Name,
            entity.TrackingCodeDisplay,
            entity.Counter,
            entity.NationalCode,
            entity.CreatedAtUtc);

    private static TrackingRequestDetailDto MapDetail(TrackingRequest entity)
    {
        var system = entity.System;
        return new TrackingRequestDetailDto(
            entity.Id,
            entity.SystemId,
            system?.Code ?? entity.SystemId.ToString(),
            system?.Name ?? string.Empty,
            entity.TrackingCodeDisplay,
            entity.Counter,
            entity.NationalCode,
            entity.FirstName,
            entity.LastName,
            entity.Mobile,
            entity.Landline,
            entity.Description,
            entity.Status.ToString(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string MaskNationalCode(string nc)
        => nc.Length >= 4 ? $"{nc[..3]}***{nc[^2..]}" : "***";

    private sealed record CallerContext(bool IsInternal, bool IsOperator, bool IsCitizen, string? NationalCode)
    {
        public static CallerContext Internal() => new(true, false, false, null);
        public static CallerContext Operator(string? nc) => new(false, true, false, nc);
        public static CallerContext Citizen(string? nc) => new(false, false, true, nc);
    }
}
