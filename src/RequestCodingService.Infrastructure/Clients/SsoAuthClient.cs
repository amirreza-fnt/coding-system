using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RequestCodingService.Application.Dtos;
using RequestCodingService.Application.Exceptions;
using RequestCodingService.Application.Interfaces;
using RequestCodingService.Application.Options;

namespace RequestCodingService.Infrastructure.Clients;

public sealed class SsoAuthClient : ISsoAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly SsoOptions _options;
    private readonly ILogger<SsoAuthClient> _logger;

    public SsoAuthClient(HttpClient httpClient, IOptions<SsoOptions> options, ILogger<SsoAuthClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SsoUserInfoDto> GetUserInfoAsync(string authorizationHeader, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new NotAuthenticatedException("Authorization header is missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoPath.TrimStart('/'));
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSO user-info call failed.");
            throw new DependencyUnavailableException("SSO service is unavailable.", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new NotAuthenticatedException("Invalid or expired token.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new DependencyUnavailableException($"SSO returned {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var nationalCode = GetString(root, "nationalCode", "national_code", "NationalCode");
        var firstName = GetString(root, "firstName", "first_name", "FirstName");
        var lastName = GetString(root, "lastName", "last_name", "LastName");
        var mobile = GetString(root, "mobile", "phone", "Mobile");

        var roles = new List<string>();
        if (root.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in rolesEl.EnumerateArray())
            {
                if (role.ValueKind == JsonValueKind.String)
                {
                    roles.Add(role.GetString() ?? string.Empty);
                }
            }
        }
        else if (root.TryGetProperty("role", out var singleRole) && singleRole.ValueKind == JsonValueKind.String)
        {
            roles.Add(singleRole.GetString() ?? string.Empty);
        }

        return new SsoUserInfoDto(nationalCode, firstName, lastName, mobile, roles);
    }

    private static string? GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
            {
                return el.GetString();
            }
        }

        return null;
    }
}
