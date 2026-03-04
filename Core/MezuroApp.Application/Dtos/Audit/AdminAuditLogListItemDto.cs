namespace MezuroApp.Application.Dtos.Audit;


public sealed record AdminAuditLogListItemDto(
    string Id,
    string AdminId,
    string AdminName,
    string AdminSurname,
    string AdminEmail,
    string EntityType,
    string Action,
    string IpAddress,
    string UserAgent,
    string CreatedAtUtc,
    Dictionary<string,object> OldValuesJson,
    Dictionary<string,object> NewValuesJson
);