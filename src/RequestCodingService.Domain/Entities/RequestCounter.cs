namespace RequestCodingService.Domain.Entities;

/// <summary>
/// Atomic counter per SYSTEM_ID + NATIONAL_CODE pair.
/// Never decremented; deleted request codes are not reused.
/// </summary>
public sealed class RequestCounter
{
    public long Id { get; set; }

    public int SystemId { get; set; }

    public string NationalCode { get; set; } = string.Empty;

    /// <summary>Last allocated 5-digit counter value.</summary>
    public int Counter { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public SystemDefinition System { get; set; } = null!;
}
