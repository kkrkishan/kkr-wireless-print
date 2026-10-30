using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KKR.PrintServer.Api.Endpoints;
using KKR.PrintServer.Api.Middleware;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KKR.PrintServer.Tests;

public class ApiIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;
    private readonly SecurityService _securityService;
    private readonly CertificateService _certService;
    private readonly PrinterDiscoveryService _discoveryService;
    private readonly JobQueueService _queueService;
    private readonly string _testBaseDir;

    public ApiIntegrationTests()
    {
        _testBaseDir = Path.Combine(Path.GetTempPath(), "kkr_api_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testBaseDir);

        var config = new ServerConfig
        {
            CertDirectory = Path.Combine(_testBaseDir, "certs"),
            LogDirectory = Path.Combine(_testBaseDir, "logs"),
            SpoolTempDirectory = Path.Combine(_testBaseDir, "spool")
        };

        _certService = new CertificateService(config.CertDirectory);
        _securityService = new SecurityService(config.SpoolTempDirectory);
        _discoveryService = new PrinterDiscoveryService();
        var printProcessor = new PrintProcessor(_discoveryService, _securityService);
        _queueService = new JobQueueService(printProcessor);

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(config);
                services.AddSingleton<ISecurityService>(_securityService);
                services.AddSingleton<ICertificateService>(_certService);
                services.AddSingleton<IPrinterDiscoveryService>(_discoveryService);
                services.AddSingleton<IJobQueueService>(_queueService);
                services.AddRouting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseMiddleware<TokenAuthenticationMiddleware>();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapDiscoveryEndpoints();
                    endpoints.MapAuthEndpoints();
                    endpoints.MapPrinterEndpoints();
                    endpoints.MapJobEndpoints(config);
                });
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }

    [Fact]
    public async Task GetDiscovery_Returns200_WithoutAuth()
    {
        var response = await _client.GetAsync("/api/v1/discovery");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Print Ofo Server", json);
        Assert.Contains("supportedFormats", json);
    }

    [Fact]
    public async Task GetHealth_Returns200_WithoutAuth()
    {
        var response = await _client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", json);
    }

    [Fact]
    public async Task GetPrinters_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/printers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPair_WithValidPin_ReturnsToken()
    {
        string validPin = _securityService.CurrentPin;
        var pairReq = new PairRequest
        {
            Pin = validPin,
            DeviceName = "Android Test Device",
            ClientType = "Android"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/pair", pairReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PairResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Token);

        // Now test accessing /api/v1/printers with this Bearer token
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/printers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

        var printersResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, printersResponse.StatusCode);
    }

    [Fact]
    public async Task PostPair_WithInvalidPin_Returns400BadRequest()
    {
        var pairReq = new PairRequest
        {
            Pin = "999999",
            DeviceName = "Android Device",
            ClientType = "Android"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/pair", pairReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostJob_WithValidPdf_Returns202Accepted()
    {
        // Pair first
        string validPin = _securityService.CurrentPin;
        var pairResponse = await _client.PostAsJsonAsync("/api/v1/auth/pair", new PairRequest { Pin = validPin });
        var pairResult = await pairResponse.Content.ReadFromJsonAsync<PairResponse>();
        Assert.NotNull(pairResult?.Token);

        // Prepare multipart form with sample PDF
        byte[] pdfBytes = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\nxref\n0 4\n0000000000 65535 f\ntrailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n180\n%%EOF"u8.ToArray();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "test_doc.pdf");
        content.Add(new StringContent("EPSON L3210 Series"), "printerName");
        content.Add(new StringContent("1"), "copies");
        content.Add(new StringContent("FitPage"), "scalingMode");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pairResult.Token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("jobId", json);
        Assert.True(json.Contains("Queued") || json.Contains("Accepted"));
        Assert.Contains("EPSON L3210 Series", json);
    }

    [Fact]
    public async Task PostJob_WithIdempotencyKey_DetectsDuplicate()
    {
        // Pair first
        string validPin = _securityService.CurrentPin;
        var pairResponse = await _client.PostAsJsonAsync("/api/v1/auth/pair", new PairRequest { Pin = validPin });
        var pairResult = await pairResponse.Content.ReadFromJsonAsync<PairResponse>();
        Assert.NotNull(pairResult?.Token);

        byte[] pdfBytes = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\ntrailer\n<< /Size 2 /Root 1 0 R >>\nstartxref\n50\n%%EOF"u8.ToArray();

        string idempotencyKey = "client-mobile-dedup-" + Guid.NewGuid().ToString("N");

        // Submit 1
        using (var content1 = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(pdfBytes);
            content1.Add(fileContent, "file", "invoice.pdf");
            content1.Add(new StringContent("Canon MF4800 Series UFRII LT"), "printerName");
            content1.Add(new StringContent(idempotencyKey), "idempotencyKey");

            var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs") { Content = content1 };
            req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pairResult.Token);
            var resp1 = await _client.SendAsync(req1);
            Assert.Equal(HttpStatusCode.Accepted, resp1.StatusCode);
        }

        // Submit 2 (identical duplicate immediately following)
        using (var content2 = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(pdfBytes);
            content2.Add(fileContent, "file", "invoice.pdf");
            content2.Add(new StringContent("Canon MF4800 Series UFRII LT"), "printerName");
            content2.Add(new StringContent(idempotencyKey), "idempotencyKey");

            var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs") { Content = content2 };
            req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pairResult.Token);
            var resp2 = await _client.SendAsync(req2);

            Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
            var json2 = await resp2.Content.ReadAsStringAsync();
            Assert.Contains("isDuplicate", json2);
            Assert.Contains("true", json2);
        }
    }

    [Fact]
    public async Task PostJob_WithNonexistentPrinter_Returns400BadRequest()
    {
        string validPin = _securityService.CurrentPin;
        var pairResponse = await _client.PostAsJsonAsync("/api/v1/auth/pair", new PairRequest { Pin = validPin });
        var pairResult = await pairResponse.Content.ReadFromJsonAsync<PairResponse>();

        byte[] pdfBytes = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\ntrailer\n<< /Size 2 /Root 1 0 R >>\nstartxref\n50\n%%EOF"u8.ToArray();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(pdfBytes), "file", "doc.pdf");
        content.Add(new StringContent("NonExistentFakePrinter999"), "printerName");

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs") { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pairResult!.Token);

        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("does not exist", json);
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
        _queueService.Dispose();
        try
        {
            if (Directory.Exists(_testBaseDir))
            {
                Directory.Delete(_testBaseDir, true);
            }
        }
        catch
        {
            // Ignore
        }
    }
}
