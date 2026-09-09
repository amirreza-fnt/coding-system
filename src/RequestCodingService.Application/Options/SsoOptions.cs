namespace RequestCodingService.Application.Options;

public sealed class SsoOptions
{
    public const string SectionName = "Sso";

    public string BaseUrl { get; set; } = string.Empty;

    public string UserInfoPath { get; set; } = "/api/auth/me";

    public int TimeoutSeconds { get; set; } = 10;

    public int RetryCount { get; set; } = 2;

    public int CircuitBreakerMinThroughput { get; set; } = 8;

    public int CircuitBreakerFailureRatio { get; set; } = 50;
}
