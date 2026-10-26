using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KKR.PrintServer.Api.Endpoints;

public static class PrinterEndpoints
{
    public static void MapPrinterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/printers", (IPrinterDiscoveryService printerService) =>
        {
            var printers = printerService.GetPrinters(refresh: true);
            return Results.Ok(printers);
        });

        app.MapGet("/api/v1/printers/{name}", (string name, IPrinterDiscoveryService printerService) =>
        {
            var printer = printerService.GetPrinter(name);
            if (printer == null)
            {
                return Results.NotFound(new { error = "NotFound", message = $"Printer '{name}' not found." });
            }
            return Results.Ok(printer);
        });
    }
}

public static class JobEndpoints
{
    public static void MapJobEndpoints(this IEndpointRouteBuilder app, ServerConfig config)
    {
        app.MapPost("/api/v1/jobs", async (
            HttpRequest request,
            ISecurityService securityService,
            IPrinterDiscoveryService printerService,
            IJobQueueService queueService,
            HttpContext context) =>
        {
            var client = context.Items["Client"] as PairedClient;
            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "BadRequest", message = "Expected multipart/form-data content type." });
            }

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "BadRequest", message = "No file was attached in the 'file' field." });
            }

            // Extract Idempotency Key
            string? idempotencyKey = form["idempotencyKey"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                idempotencyKey = request.Headers["X-Idempotency-Key"].FirstOrDefault();
            }

            // Deduplication check
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && securityService.IsDuplicateRequest(idempotencyKey, out var existingJobId))
            {
                var existingJob = queueService.GetJob(existingJobId);
                if (existingJob != null)
                {
                    return Results.Ok(new
                    {
                        isDuplicate = true,
                        jobId = existingJob.JobId,
                        status = existingJob.Status.ToString(),
                        message = "Duplicate print submission detected. Returning existing job record.",
                        printerName = existingJob.PrinterName,
                        documentName = existingJob.DocumentName
                    });
                }
            }

            // Target printer
            string? printerName = form["printerName"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(printerName))
            {
                var defaultPrinter = printerService.GetPrinters().FirstOrDefault(p => p.IsDefault);
                printerName = defaultPrinter?.Name;
                if (string.IsNullOrWhiteSpace(printerName))
                {
                    return Results.BadRequest(new { error = "BadRequest", message = "Target printerName is required and no default printer is available." });
                }
            }

            var targetPrinter = printerService.GetPrinter(printerName);
            if (targetPrinter == null)
            {
                return Results.BadRequest(new { error = "BadRequest", message = $"Printer '{printerName}' does not exist on this Windows host." });
            }

            if (!printerService.IsPrinterReady(printerName, out var notReadyReason))
            {
                return Results.BadRequest(new { error = "PrinterNotReady", message = notReadyReason });
            }

            // Validate file payload
            using var fileStream = file.OpenReadStream();
            var validation = securityService.ValidateUploadedDocument(fileStream, file.FileName, file.Length, config.MaxUploadSizeBytes);
            if (!validation.IsValid)
            {
                return Results.BadRequest(new { error = "InvalidPayload", message = validation.ErrorMessage });
            }

            // Parse settings
            int copies = 1;
            if (int.TryParse(form["copies"].FirstOrDefault(), out int parsedCopies))
            {
                copies = Math.Clamp(parsedCopies, 1, targetPrinter.Capabilities.MaxCopies);
            }

            string? pageRange = form["pageRange"].FirstOrDefault();

            var orientation = PrintOrientation.Auto;
            if (Enum.TryParse<PrintOrientation>(form["orientation"].FirstOrDefault(), true, out var parsedOri))
            {
                orientation = parsedOri;
            }

            string? paperSize = form["paperSize"].FirstOrDefault();

            var colorMode = PrintColorMode.Auto;
            if (Enum.TryParse<PrintColorMode>(form["colorMode"].FirstOrDefault(), true, out var parsedColor))
            {
                colorMode = parsedColor;
            }

            var scalingMode = PrintScalingMode.FitPage;
            if (Enum.TryParse<PrintScalingMode>(form["scalingMode"].FirstOrDefault(), true, out var parsedScale))
            {
                scalingMode = parsedScale;
            }

            bool centerOnPage = true;
            if (bool.TryParse(form["centerOnPage"].FirstOrDefault(), out bool parsedCenter))
            {
                centerOnPage = parsedCenter;
            }

            // Safe temporary file creation
            string ext = validation.DetectedType == PrintDocumentType.Pdf ? ".pdf" :
                         validation.DetectedType == PrintDocumentType.Png ? ".png" : ".jpg";
            string tempFilePath = securityService.CreateSafeTempFile(ext);

            fileStream.Position = 0;
            using (var destination = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(destination);
            }

            var jobRecord = new PrintJobRecord
            {
                JobId = Guid.NewGuid(),
                ClientId = client?.ClientId ?? "anonymous",
                ClientDeviceName = client?.DeviceName ?? "Android Device",
                ClientIp = clientIp,
                PrinterName = targetPrinter.Name,
                DocumentName = Path.GetFileName(file.FileName),
                DocumentType = validation.DetectedType,
                FileSizeBytes = file.Length,
                PageCount = 1,
                Status = JobStatus.Accepted,
                StatusMessage = "Job accepted and spooled for printer.",
                TempFilePath = tempFilePath,
                SettingsSummary = $"Copies: {copies}, Range: {pageRange ?? "all"}, Orientation: {orientation}, Color: {colorMode}, Scale: {scalingMode}",
                Request = new PrintJobRequest
                {
                    PrinterName = targetPrinter.Name,
                    DocumentType = validation.DetectedType,
                    Copies = copies,
                    PageRange = pageRange,
                    Orientation = orientation,
                    PaperSizeName = paperSize,
                    ColorMode = colorMode,
                    ScalingMode = scalingMode,
                    CenterOnPage = centerOnPage,
                    IdempotencyKey = idempotencyKey,
                    ClientId = client?.ClientId,
                    ClientDeviceName = client?.DeviceName,
                    SourceIp = clientIp,
                    DocumentName = file.FileName
                }
            };

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                securityService.RegisterIdempotency(idempotencyKey, jobRecord.JobId);
            }

            queueService.EnqueueJob(jobRecord);

            return Results.Accepted($"/api/v1/jobs/{jobRecord.JobId}", new
            {
                jobId = jobRecord.JobId,
                status = jobRecord.Status.ToString(),
                printerName = jobRecord.PrinterName,
                documentName = jobRecord.DocumentName,
                fileSizeBytes = jobRecord.FileSizeBytes,
                createdAtUtc = jobRecord.CreatedAtUtc,
                message = "Job accepted by Windows Print Server and queued for spooling."
            });
        }).DisableAntiforgery();

        app.MapGet("/api/v1/jobs/{id:guid}", (Guid id, IJobQueueService queueService) =>
        {
            var job = queueService.GetJob(id);
            if (job == null)
            {
                return Results.NotFound(new { error = "NotFound", message = $"Job '{id}' was not found in active history." });
            }

            return Results.Ok(new
            {
                jobId = job.JobId,
                status = job.Status.ToString(),
                statusMessage = job.StatusMessage,
                printerName = job.PrinterName,
                documentName = job.DocumentName,
                documentType = job.DocumentType.ToString(),
                fileSizeBytes = job.FileSizeBytes,
                pageCount = job.PageCount,
                createdAtUtc = job.CreatedAtUtc,
                startedAtUtc = job.StartedAtUtc,
                completedAtUtc = job.CompletedAtUtc,
                errorMessage = job.ErrorMessage,
                settingsSummary = job.SettingsSummary
            });
        });

        app.MapPost("/api/v1/jobs/{id:guid}/cancel", (Guid id, IJobQueueService queueService) =>
        {
            if (queueService.CancelJob(id, out var reason))
            {
                return Results.Ok(new { success = true, jobId = id, message = "Job cancellation requested successfully." });
            }

            return Results.BadRequest(new { success = false, jobId = id, error = reason });
        });

        app.MapGet("/api/v1/jobs", (IJobQueueService queueService, HttpContext context) =>
        {
            var client = context.Items["Client"] as PairedClient;
            var jobs = queueService.GetAllJobs();

            var result = jobs.Select(j => new
            {
                jobId = j.JobId,
                status = j.Status.ToString(),
                statusMessage = j.StatusMessage,
                printerName = j.PrinterName,
                documentName = j.DocumentName,
                documentType = j.DocumentType.ToString(),
                fileSizeBytes = j.FileSizeBytes,
                pageCount = j.PageCount,
                createdAtUtc = j.CreatedAtUtc,
                completedAtUtc = j.CompletedAtUtc,
                errorMessage = j.ErrorMessage,
                isOwnedByRequester = client != null && j.ClientId == client.ClientId
            });

            return Results.Ok(result);
        });
    }
}
