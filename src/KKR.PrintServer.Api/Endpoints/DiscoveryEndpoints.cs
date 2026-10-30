using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KKR.PrintServer.Api.Endpoints;

public static class DiscoveryEndpoints
{
    public static void MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/discovery", (ICertificateService certService, IPrinterDiscoveryService printerService) =>
        {
            var printers = printerService.GetPrinters();
            return Results.Ok(new
            {
                serverName = "Print Ofo Server",
                version = "1.0.0",
                machineName = Environment.MachineName,
                requiresPairing = true,
                httpsThumbprint = certService.GetCertificateThumbprint(),
                activePrinters = printers.Select(p => p.Name).ToList(),
                supportedFormats = new[] { "pdf", "jpg", "jpeg", "png" }
            });
        });

        app.MapGet("/api/v1/health", (IPrinterDiscoveryService printerService, IJobQueueService queueService) =>
        {
            var printers = printerService.GetPrinters();
            return Results.Ok(new
            {
                status = "Healthy",
                timestampUtc = DateTime.UtcNow,
                onlinePrintersCount = printers.Count(p => p.IsOnline),
                totalPrintersCount = printers.Count,
                queuedJobsCount = queueService.GetAllJobs().Count(j => j.Status is JobStatus.Queued or JobStatus.Processing)
            });
        });
    }
}

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/pair", (PairRequest request, ISecurityService securityService, HttpContext context) =>
        {
            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = securityService.AuthenticatePairing(request.Pin, request.DeviceName, request.ClientType, clientIp);

            if (!result.Success)
            {
                return Results.BadRequest(result);
            }

            return Results.Ok(result);
        });

        app.MapGet("/api/v1/auth/clients", (ISecurityService securityService) =>
        {
            var clients = securityService.GetPairedClients().Select(c => new
            {
                c.ClientId,
                c.DeviceName,
                c.ClientType,
                c.PairedAtUtc,
                c.LastSeenUtc,
                c.LastKnownIp,
                c.IsRevoked
            });
            return Results.Ok(clients);
        });
    }
}
