using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class AuditHelper : IAuditHelper
{
    private readonly IAuditLogService _auditService;
    private readonly IHttpContextAccessor _http;

    public AuditHelper(IAuditLogService auditService, IHttpContextAccessor http)
    {
        _auditService = auditService;
        _http = http;
    }

    public async Task LogAsync(
        string entityType,
        string action,
        string eventName,
        Guid? entityId = null,
        Dictionary<string, object>? oldValues = null,
        Dictionary<string, object>? newValues = null)
    {
        var userId = GetUserId();
        var (ip, ua) = GetRequestInfo();

        var mergedNew = new Dictionary<string, object>
        {
            ["Event"] = eventName,
            ["Data"] = newValues ?? new Dictionary<string, object>()
        };

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId?.ToString(),
            Module = entityType,
            EntityId = entityId,
            ActionType = action,
            OldValuesJson = oldValues ?? new Dictionary<string, object>(),
            NewValuesJson = mergedNew,
            IpAddress = ip,
            UserAgent = ua,
            CreatedAt = DateTime.UtcNow
        });
    }

    private Guid? GetUserId()
    {
        var id = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private (string? ip, string? ua) GetRequestInfo()
    {
        var ctx = _http.HttpContext;

        return (
            ctx?.Connection?.RemoteIpAddress?.ToString(),
            ctx?.Request?.Headers["User-Agent"].ToString()
        );
    }
}