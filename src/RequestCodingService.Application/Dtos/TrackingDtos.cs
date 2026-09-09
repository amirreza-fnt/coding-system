namespace RequestCodingService.Application.Dtos;

public sealed record CreateTrackingRequestDto(
    int SystemId,
    string NationalCode,
    string FirstName,
    string LastName,
    string? Mobile,
    string? Landline,
    string? Description);

public sealed record CreateTrackingResponseDto(
    Guid Id,
    int SystemId,
    string SystemCode,
    string SystemName,
    string TrackingCode,
    int Counter,
    string NationalCode,
    DateTime CreatedAtUtc);

public sealed record TrackingRequestDetailDto(
    Guid Id,
    int SystemId,
    string SystemCode,
    string SystemName,
    string TrackingCode,
    int Counter,
    string NationalCode,
    string FirstName,
    string LastName,
    string? Mobile,
    string? Landline,
    string? Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record OperatorSearchQueryDto(
    int SystemId,
    int? Counter,
    string? NationalCode,
    string? FirstName,
    string? LastName,
    string? Mobile,
    string? Landline,
    DateTime? FromDate,
    DateTime? ToDate);

public sealed record SystemDefinitionDto(
    int Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record SsoUserInfoDto(
    string? NationalCode,
    string? FirstName,
    string? LastName,
    string? Mobile,
    IReadOnlyList<string> Roles);

public sealed record UpdateStatusRequestDto(string Status);
