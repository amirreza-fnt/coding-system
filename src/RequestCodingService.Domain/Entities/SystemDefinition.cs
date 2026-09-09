namespace RequestCodingService.Domain.Entities;

/// <summary>
/// Registered organization/system (e.g. 137, fire department).
/// Each system has an independent counter namespace per national code.
/// </summary>
public sealed class SystemDefinition
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
