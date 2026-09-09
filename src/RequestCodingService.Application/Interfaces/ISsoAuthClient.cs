using RequestCodingService.Application.Dtos;

namespace RequestCodingService.Application.Interfaces;

public interface ISsoAuthClient
{
    Task<SsoUserInfoDto> GetUserInfoAsync(string authorizationHeader, CancellationToken ct);
}
