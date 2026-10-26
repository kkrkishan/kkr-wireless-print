using KKR.PrintServer.Core.Services;
using Xunit;

namespace KKR.PrintServer.Tests;

public class PageRangeParserTests
{
    [Theory]
    [InlineData(null, 5, new[] { 1, 2, 3, 4, 5 })]
    [InlineData("", 3, new[] { 1, 2, 3 })]
    [InlineData("all", 4, new[] { 1, 2, 3, 4 })]
    [InlineData("ALL", 2, new[] { 1, 2 })]
    public void Parse_AllPages_ReturnsFullRange(string? input, int totalPages, int[] expected)
    {
        var result = PageRangeParser.Parse(input, totalPages);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_CommaAndRange_ReturnsSortedDistinctPages()
    {
        var result = PageRangeParser.Parse("1-3, 5, 2, 8", 10);
        Assert.Equal(new[] { 1, 2, 3, 5, 8 }, result);
    }

    [Fact]
    public void Parse_OutOfRangePages_FiltersOutInvalidNumbers()
    {
        var result = PageRangeParser.Parse("0, 1, 2, 99", 3);
        Assert.Equal(new[] { 1, 2 }, result);
    }
}

public class IdempotencyTests
{
    private readonly SecurityService _service;

    public IdempotencyTests()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "kkr_idem_test_" + Guid.NewGuid().ToString("N"));
        _service = new SecurityService(tempDir);
    }

    [Fact]
    public void DuplicateRequest_IsIdentified()
    {
        string key = "req-uuid-123456";
        var jobId = Guid.NewGuid();

        bool isDup1 = _service.IsDuplicateRequest(key, out var existingId1);
        Assert.False(isDup1);
        Assert.Equal(Guid.Empty, existingId1);

        _service.RegisterIdempotency(key, jobId);

        bool isDup2 = _service.IsDuplicateRequest(key, out var existingId2);
        Assert.True(isDup2);
        Assert.Equal(jobId, existingId2);
    }
}
