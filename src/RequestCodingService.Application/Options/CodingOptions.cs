namespace RequestCodingService.Application.Options;

public sealed class CodingOptions
{
    public const string SectionName = "Coding";

    /// <summary>Maximum counter value (default 99999 = 5 digits).</summary>
    public int MaxCounter { get; set; } = 99999;

    /// <summary>Starting counter when a new person registers in a system.</summary>
    public int InitialCounter { get; set; } = 1;
}
