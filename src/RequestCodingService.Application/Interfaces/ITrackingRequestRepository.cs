using RequestCodingService.Application.Dtos;
using RequestCodingService.Domain.Entities;

namespace RequestCodingService.Application.Interfaces;

public interface ITrackingRequestRepository
{
    Task<TrackingRequest?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<TrackingRequest?> GetBySystemCounterAndNationalCodeAsync(
        int systemId, int counter, string nationalCode, CancellationToken ct);

    Task<IReadOnlyList<TrackingRequest>> SearchOperatorAsync(
        OperatorSearchQueryDto query, CancellationToken ct);

    Task<IReadOnlyList<TrackingRequest>> GetByNationalCodeAsync(
        string nationalCode, int? systemId, CancellationToken ct);

    /// <summary>
    /// Atomically allocates the next counter and inserts the request in one transaction.
    /// Uses UPDLOCK on the counter row to prevent duplicate codes under concurrency.
    /// </summary>
    Task<TrackingRequest> CreateWithNextCounterAsync(
        TrackingRequest entity, int maxCounter, int initialCounter, CancellationToken ct);

    Task SoftDeleteAsync(Guid id, CancellationToken ct);

    Task UpdateStatusAsync(Guid id, Domain.Enums.RequestStatus status, CancellationToken ct);

    Task<IReadOnlyList<SystemDefinition>> GetActiveSystemsAsync(CancellationToken ct);

    Task<SystemDefinition?> GetSystemByIdAsync(int systemId, CancellationToken ct);
}
