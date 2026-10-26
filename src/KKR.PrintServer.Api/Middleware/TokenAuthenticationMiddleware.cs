using System.Net;
using System.Text.Json;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Services;
using Microsoft.AspNetCore.Http;

namespace KKR.PrintServer.Api.Middleware;

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISecurityService _securityService;

    // Public endpoints that do not require Bearer token
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/discovery",
        "/api/v1/health",
        "/api/v1/auth/pair"
    };

    public TokenAuthenticationMiddleware(RequestDelegate next, ISecurityService securityService)
    {
        _next = next;
        _securityService = securityService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        // Skip auth for public endpoints and OPTIONS preflight
        if (context.Request.Method == HttpMethods.Options ||
            PublicPaths.Contains(path) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        string? authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            await WriteUnauthorizedResponseAsync(context, "Authorization header missing. Please provide a Bearer token.");
            return;
        }

        if (!_securityService.ValidateToken(authHeader, out var client) || client == null)
        {
            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            AppLogger.Instance.Security("Auth", $"Unauthorized request rejected from {clientIp} for {path}");
            await WriteUnauthorizedResponseAsync(context, "Invalid, expired, or revoked authentication token. Please re-pair your device.");
            return;
        }

        // Attach client info to HttpContext for downstream endpoints
        context.Items["Client"] = client;
        await _next(context);
    }

    private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Unauthorized",
            message = message,
            statusCode = 401,
            timestampUtc = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
