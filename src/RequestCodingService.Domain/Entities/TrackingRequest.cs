using RequestCodingService.Domain.Enums;

namespace RequestCodingService.Domain.Entities;

/// <summary>
/// A citizen request with a short 5-digit tracking code.
/// Logical uniqueness: SYSTEM_ID + NATIONAL_CODE + COUNTER.
/// </summary>
public sealed class TrackingRequest
{
    public Guid Id { get; set; }

    public int SystemId { get; set; }

    /// <summary>5-digit tracking code (00001–99999).</summary>
    public int Counter { get; set; }

    public string NationalCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public string? Landline { get; set; }

    public string? Description { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public SystemDefinition System { get; set; } = null!;

    /// <summary>Formatted 5-digit tracking code for display.</summary>
    public string TrackingCodeDisplay => Counter.ToString("D5");
}
