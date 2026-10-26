using System.Text;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Xunit;

namespace KKR.PrintServer.Tests;

public class SecurityServiceTests
{
    private readonly string _tempSpoolDir;
    private readonly SecurityService _service;

    public SecurityServiceTests()
    {
        _tempSpoolDir = Path.Combine(Path.GetTempPath(), "kkr_test_spool_" + Guid.NewGuid().ToString("N"));
        _service = new SecurityService(_tempSpoolDir, pinExpiryMinutes: 10);
    }

    [Fact]
    public void GenerateNewPin_ProducesValid6DigitPin()
    {
        string pin = _service.GenerateNewPin();
        Assert.NotNull(pin);
        Assert.Equal(6, pin.Length);
        Assert.True(int.TryParse(pin, out int num));
        Assert.InRange(num, 100000, 999999);
    }

    [Fact]
    public void AuthenticatePairing_WithValidPin_IssuesTokenAndClient()
    {
        string currentPin = _service.CurrentPin;
        var response = _service.AuthenticatePairing(currentPin, "Pixel 8 Pro", "Android", "10.92.81.50");

        Assert.True(response.Success);
        Assert.NotNull(response.Token);
        Assert.NotNull(response.ClientId);
        Assert.Null(response.ErrorMessage);

        bool isValid = _service.ValidateToken(response.Token, out var client);
        Assert.True(isValid);
        Assert.NotNull(client);
        Assert.Equal("Pixel 8 Pro", client.DeviceName);
    }

    [Fact]
    public void AuthenticatePairing_WithInvalidPin_Fails()
    {
        var response = _service.AuthenticatePairing("000000", "Pixel 8 Pro", "Android", "10.92.81.50");
        Assert.False(response.Success);
        Assert.Null(response.Token);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public void RevokeClient_InvalidatesToken()
    {
        string currentPin = _service.CurrentPin;
        var response = _service.AuthenticatePairing(currentPin, "Pixel 8 Pro", "Android", "10.92.81.50");
        Assert.True(response.Success);

        _service.RevokeClient(response.ClientId!);

        bool isValid = _service.ValidateToken(response.Token, out var client);
        Assert.False(isValid);
        Assert.Null(client);
    }
}
