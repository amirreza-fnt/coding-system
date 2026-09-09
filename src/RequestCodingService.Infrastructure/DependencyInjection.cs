using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RequestCodingService.Application.Interfaces;
using RequestCodingService.Application.Options;
using RequestCodingService.Application.Services;
using RequestCodingService.Application.Validation;
using RequestCodingService.Infrastructure.Clients;
using RequestCodingService.Infrastructure.Persistence;

namespace RequestCodingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SsoOptions>()
            .Bind(configuration.GetSection(SsoOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Sso:BaseUrl is required.")
            .ValidateOnStart();

        services.AddOptions<CodingOptions>()
            .Bind(configuration.GetSection(CodingOptions.SectionName));

        services.Configure<InternalAuthOptions>(configuration.GetSection(InternalAuthOptions.SectionName));

        services.AddValidatorsFromAssemblyContaining<CreateTrackingRequestValidator>();

        var connectionString = configuration.GetConnectionString("RequestCoding")
            ?? throw new InvalidOperationException("ConnectionStrings:RequestCoding is missing.");

        services.AddDbContext<RequestCodingDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                sql.CommandTimeout(30);
            }));

        services.AddScoped<ITrackingRequestRepository, TrackingRequestRepository>();
        services.AddScoped<ITrackingCodingService, TrackingCodingService>();

        var sso = configuration.GetSection(SsoOptions.SectionName).Get<SsoOptions>() ?? new SsoOptions();
        services.AddHttpClient<ISsoAuthClient, SsoAuthClient>(client =>
        {
            client.BaseAddress = new Uri(EnsureTrailingSlash(sso.BaseUrl));
            client.Timeout = TimeSpan.FromSeconds(sso.TimeoutSeconds + 5);
        }).AddStandardResilienceHandler(opt =>
        {
            opt.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(sso.TimeoutSeconds + 2);
            opt.AttemptTimeout.Timeout = TimeSpan.FromSeconds(sso.TimeoutSeconds);
            opt.Retry.MaxRetryAttempts = sso.RetryCount;
            opt.Retry.Delay = TimeSpan.FromMilliseconds(300);
            opt.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            opt.CircuitBreaker.MinimumThroughput = sso.CircuitBreakerMinThroughput;
            opt.CircuitBreaker.FailureRatio = sso.CircuitBreakerFailureRatio / 100.0;
            opt.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });

        return services;
    }

    private static string EnsureTrailingSlash(string url)
        => url.EndsWith('/') ? url : url + "/";
}
