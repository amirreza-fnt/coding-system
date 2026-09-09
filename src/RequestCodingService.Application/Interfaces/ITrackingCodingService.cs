using RequestCodingService.Application.Dtos;

namespace RequestCodingService.Application.Interfaces;

public interface ITrackingCodingService
{
    Task<CreateTrackingResponseDto> CreateAsync(
        CreateTrackingRequestDto request,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct);

    Task<TrackingRequestDetailDto> GetByIdAsync(
        Guid id,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct);

    Task<TrackingRequestDetailDto> GetByTrackingCodeAsync(
        int systemId,
        int counter,
        string? authorizationHeader,
        string? apiKeyHeader,
        string? nationalCodeForCitizen,
        CancellationToken ct);

    Task<IReadOnlyList<TrackingRequestDetailDto>> SearchOperatorAsync(
        OperatorSearchQueryDto query,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct);

    Task<IReadOnlyList<TrackingRequestDetailDto>> GetMyRequestsAsync(
        string authorizationHeader,
        int? systemId,
        CancellationToken ct);

    Task SoftDeleteAsync(
        Guid id,
        string? authorizationHeader,
        string? apiKeyHeader,
        CancellationToken ct);

    Task<IReadOnlyList<SystemDefinitionDto>> GetSystemsAsync(CancellationToken ct);
}
