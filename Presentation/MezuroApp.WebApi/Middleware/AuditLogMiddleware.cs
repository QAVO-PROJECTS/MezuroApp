using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Domain.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MezuroApp.WebApi.Middleware
{
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService, IAuditLookupService auditLookupService)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            var routeData = context.GetRouteData();
            var controller = routeData?.Values["controller"]?.ToString() ?? "Unknown";
            var routeAction = routeData?.Values["action"]?.ToString();
            var routeId = routeData?.Values["id"]?.ToString();

            var httpMethod = context.Request.Method.ToUpperInvariant();
            var action = NormalizeAction(routeAction, httpMethod);

            var userId = context.User?.FindFirst("sub")?.Value
                      ?? context.User?.FindFirst("nameidentifier")?.Value
                      ?? "Anonymous";

            var shouldAudit = httpMethod is "POST" or "PUT" or "PATCH" or "DELETE";

            Dictionary<string, object>? newValuesFromRequest = null;
            Dictionary<string, object>? oldValuesFromDb = null;
            Dictionary<string, object>? newValuesFromResponse = null;

            try
            {
                // Request body -> new values
                newValuesFromRequest = await ReadRequestBodyAsDictionaryAsync(context);

                // PUT/PATCH/DELETE -> old values (before operation)
                if (shouldAudit && (httpMethod == "PUT" || httpMethod == "PATCH" || httpMethod == "DELETE"))
                {
                    var bodyId = ExtractIdFromNewValues(newValuesFromRequest);
                    var effectiveId = routeId ?? bodyId;  // route'da yoxdursa, body'dən götür
                    oldValuesFromDb = await auditLookupService.GetOldValuesAsync(controller, action, effectiveId);
                }
            }
            catch
            {
                // ignore
            }

            // Response capture
            var originalResponseBody = context.Response.Body;
            await using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                await _next(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var respText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                if (!string.IsNullOrWhiteSpace(respText) && IsJson(respText))
                {
                    try
                    {
                        var jObj = JObject.Parse(respText);

                        if (jObj["data"] is JObject dataObj)
                            newValuesFromResponse = dataObj.ToObject<Dictionary<string, object>>();
                        else
                            newValuesFromResponse = jObj.ToObject<Dictionary<string, object>>();
                    }
                    catch { /* ignore */ }
                }
            }
            finally
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBuffer.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
            }

            try
            {
                if (shouldAudit)
                {
                    // EntityId: route -> body(request/response)
                    var bodyId = ExtractIdFromNewValues(newValuesFromRequest) ?? ExtractIdFromNewValues(newValuesFromResponse);
              
                    
                    Guid? entityGuid = null;
                    if (!string.IsNullOrWhiteSpace(routeId) && Guid.TryParse(routeId, out var rg)) entityGuid = rg;
                    else if (!string.IsNullOrWhiteSpace(bodyId) && Guid.TryParse(bodyId, out var bg)) entityGuid = bg;


                    var auditLog = new AuditLog
                    {
                        UserId = userId,
                        EntityType = controller,
                        EntityId = entityGuid, // ✅ string kimi yaz
                        Action = action,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        NewValues = newValuesFromResponse ?? newValuesFromRequest,
                        OldValues = oldValuesFromDb,
                        CreatedAt = DateTime.UtcNow
                    };

                    await auditLogService.LogAsync(auditLog);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string NormalizeAction(string? routeAction, string httpMethod)
        {
            if (!string.IsNullOrWhiteSpace(routeAction))
                return routeAction.Trim().ToUpperInvariant();

            return httpMethod switch
            {
                "POST"  => "CREATE",
                "PUT"   => "UPDATE",
                "PATCH" => "UPDATE",
                "DELETE"=> "DELETE",
                "GET"   => "GET",
                _       => httpMethod
            };
        }

        private static bool IsJson(string text)
        {
            var t = text.Trim();
            return (t.StartsWith("{") && t.EndsWith("}")) || (t.StartsWith("[") && t.EndsWith("]"));
        }

        private static async Task<Dictionary<string, object>?> ReadRequestBodyAsDictionaryAsync(HttpContext context)
        {
            var req = context.Request;

            if (req.ContentLength > 0 && (req.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                req.EnableBuffering();
                using var reader = new StreamReader(req.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                req.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body) && IsJson(body))
                {
                    try
                    {
                        var jObj = JObject.Parse(body);
                        return jObj.ToObject<Dictionary<string, object>>();
                    }
                    catch { /* ignore */ }
                }
            }

            if (req.HasFormContentType)
            {
                try
                {
                    var form = await req.ReadFormAsync();
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in form)
                        dict[kvp.Key] = kvp.Value.ToString();

                    if (form.Files != null && form.Files.Count > 0)
                    {
                        dict["_files"] = form.Files.Select(f => new
                        {
                            f.Name,
                            f.FileName,
                            f.ContentType,
                            f.Length
                        }).ToList();
                    }

                    return dict;
                }
                catch { /* ignore */ }
            }

            return null;
        }

        private static string? ExtractIdFromNewValues(Dictionary<string, object>? dict)
        {
            if (dict == null) return null;

            var keys = new[] { "id", "Id", "categoryId", "CategoryId", "productId", "ProductId" };

            foreach (var k in keys)
            {
                if (dict.TryGetValue(k, out var v) && v != null)
                {
                    var s = v.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
            }
            return null;
        }
    }

    public static class AuditLogMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<AuditLogMiddleware>();
    }
}
