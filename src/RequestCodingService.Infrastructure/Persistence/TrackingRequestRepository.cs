using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RequestCodingService.Application.Dtos;
using RequestCodingService.Application.Exceptions;
using RequestCodingService.Application.Interfaces;
using RequestCodingService.Domain.Entities;
using RequestCodingService.Domain.Enums;

namespace RequestCodingService.Infrastructure.Persistence;

public sealed class TrackingRequestRepository : ITrackingRequestRepository
{
    private readonly RequestCodingDbContext _db;
    private readonly ILogger<TrackingRequestRepository> _logger;

    public TrackingRequestRepository(RequestCodingDbContext db, ILogger<TrackingRequestRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<TrackingRequest?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.TrackingRequests
            .Include(x => x.System)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<TrackingRequest?> GetBySystemCounterAndNationalCodeAsync(
        int systemId, int counter, string nationalCode, CancellationToken ct)
        => _db.TrackingRequests
            .Include(x => x.System)
            .FirstOrDefaultAsync(x =>
                x.SystemId == systemId
                && x.Counter == counter
                && x.NationalCode == nationalCode, ct);

    public async Task<IReadOnlyList<TrackingRequest>> SearchOperatorAsync(
        OperatorSearchQueryDto query, CancellationToken ct)
    {
        var q = _db.TrackingRequests
            .Include(x => x.System)
            .Where(x => x.SystemId == query.SystemId);

        if (query.Counter.HasValue)
        {
            q = q.Where(x => x.Counter == query.Counter.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.NationalCode))
        {
            q = q.Where(x => x.NationalCode == query.NationalCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            var fn = query.FirstName.Trim();
            q = q.Where(x => x.FirstName.Contains(fn));
        }

        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            var ln = query.LastName.Trim();
            q = q.Where(x => x.LastName.Contains(ln));
        }

        if (!string.IsNullOrWhiteSpace(query.Mobile))
        {
            q = q.Where(x => x.Mobile == query.Mobile.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Landline))
        {
            q = q.Where(x => x.Landline == query.Landline.Trim());
        }

        if (query.FromDate.HasValue)
        {
            q = q.Where(x => x.CreatedAtUtc >= query.FromDate.Value.ToUniversalTime());
        }

        if (query.ToDate.HasValue)
        {
            q = q.Where(x => x.CreatedAtUtc <= query.ToDate.Value.ToUniversalTime());
        }

        return await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TrackingRequest>> GetByNationalCodeAsync(
        string nationalCode, int? systemId, CancellationToken ct)
    {
        var q = _db.TrackingRequests
            .Include(x => x.System)
            .Where(x => x.NationalCode == nationalCode);

        if (systemId.HasValue)
        {
            q = q.Where(x => x.SystemId == systemId.Value);
        }

        return await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);
    }

    public async Task<TrackingRequest> CreateWithNextCounterAsync(
        TrackingRequest entity, int maxCounter, int initialCounter, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var nextCounter = await AllocateNextCounterAsync(
                entity.SystemId, entity.NationalCode, maxCounter, initialCounter, ct);

            entity.Counter = nextCounter;
            _db.TrackingRequests.Add(entity);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await _db.Entry(entity).Reference(x => x.System).LoadAsync(ct);
            return entity;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Atomically increments counter using UPDLOCK to prevent race conditions.
    /// Creates counter row on first request for this person in this system.
    /// </summary>
    private async Task<int> AllocateNextCounterAsync(
        int systemId, string nationalCode, int maxCounter, int initialCounter, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = """
            DECLARE @NextCounter INT;

            UPDATE RequestCounters WITH (UPDLOCK, ROWLOCK)
            SET @NextCounter = Counter = Counter + 1,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE SystemId = @SystemId AND NationalCode = @NationalCode;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO RequestCounters (SystemId, NationalCode, Counter, UpdatedAtUtc)
                VALUES (@SystemId, @NationalCode, @InitialCounter, SYSUTCDATETIME());
                SET @NextCounter = @InitialCounter;
            END

            SELECT @NextCounter;
            """;

        cmd.Parameters.Add(new SqlParameter("@SystemId", systemId));
        cmd.Parameters.Add(new SqlParameter("@NationalCode", nationalCode));
        cmd.Parameters.Add(new SqlParameter("@InitialCounter", initialCounter));

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException("Failed to allocate tracking counter.");
        }

        var nextCounter = Convert.ToInt32(result);
        if (nextCounter > maxCounter)
        {
            _logger.LogWarning(
                "Counter exhausted for system {SystemId}, national code ending {Suffix}",
                systemId, nationalCode[^4..]);
            throw new CounterExhaustedException(
                $"Maximum tracking codes ({maxCounter}) reached for this person in this system.");
        }

        return nextCounter;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.TrackingRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        if (entity.DeletedAtUtc.HasValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        entity.Status = RequestStatus.Deleted;
        entity.DeletedAtUtc = now;
        entity.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, RequestStatus status, CancellationToken ct)
    {
        var entity = await _db.TrackingRequests.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        entity.Status = status;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<SystemDefinition>> GetActiveSystemsAsync(CancellationToken ct)
        => _db.Systems
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<SystemDefinition>)t.Result, ct);

    public Task<SystemDefinition?> GetSystemByIdAsync(int systemId, CancellationToken ct)
        => _db.Systems.FirstOrDefaultAsync(x => x.Id == systemId, ct);
}
