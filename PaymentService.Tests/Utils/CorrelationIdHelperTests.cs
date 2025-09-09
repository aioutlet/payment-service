using PaymentService.Utils;
using System.Collections.Concurrent;
using Xunit;

namespace PaymentService.Tests.Utils;

/// <summary>
/// Unit tests for CorrelationIdHelper to verify distributed tracing functionality
/// </summary>
public class CorrelationIdHelperTests
{
    [Fact]
    public void GetCorrelationId_WhenNotSet_ReturnsNewGuid()
    {
        // Act
        var correlationId = CorrelationIdHelper.GetCorrelationId();

        // Assert
        Assert.NotNull(correlationId);
        Assert.NotEmpty(correlationId);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public void SetCorrelationId_WhenCalled_SetsCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id-123";

        // Act
        CorrelationIdHelper.SetCorrelationId(expectedCorrelationId);
        var actualCorrelationId = CorrelationIdHelper.GetCorrelationId();

        // Assert
        Assert.Equal(expectedCorrelationId, actualCorrelationId);
    }

    [Fact]
    public void GenerateCorrelationId_WhenCalled_GeneratesAndSetsNewId()
    {
        // Act
        var generatedId = CorrelationIdHelper.GenerateCorrelationId();
        var retrievedId = CorrelationIdHelper.GetCorrelationId();

        // Assert
        Assert.NotNull(generatedId);
        Assert.NotEmpty(generatedId);
        Assert.True(Guid.TryParse(generatedId, out _));
        Assert.Equal(generatedId, retrievedId);
    }

    [Fact]
    public void CorrelationId_Property_GetterAndSetter_WorkCorrectly()
    {
        // Arrange
        var testCorrelationId = "property-test-123";

        // Act
        CorrelationIdHelper.CorrelationId = testCorrelationId;
        var result = CorrelationIdHelper.CorrelationId;

        // Assert
        Assert.Equal(testCorrelationId, result);
    }

    [Fact]
    public void CorrelationId_WhenNotSet_ReturnsGeneratedGuid()
    {
        // Arrange
        // Reset to ensure clean state
        CorrelationIdHelper.SetCorrelationId(null!);

        // Act
        var correlationId = CorrelationIdHelper.CorrelationId;

        // Assert
        Assert.NotNull(correlationId);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task CorrelationId_AsyncLocalBehavior_MaintainsPerTask()
    {
        // Arrange
        var task1CorrelationId = "task-1-correlation";
        var task2CorrelationId = "task-2-correlation";
        var retrievedIds = new ConcurrentBag<string>();

        // Act
        var task1 = Task.Run(() =>
        {
            CorrelationIdHelper.SetCorrelationId(task1CorrelationId);
            Thread.Sleep(100); // Simulate work
            retrievedIds.Add(CorrelationIdHelper.GetCorrelationId());
        });

        var task2 = Task.Run(() =>
        {
            CorrelationIdHelper.SetCorrelationId(task2CorrelationId);
            Thread.Sleep(100); // Simulate work
            retrievedIds.Add(CorrelationIdHelper.GetCorrelationId());
        });

        await Task.WhenAll(task1, task2);

        // Assert
        Assert.Contains(task1CorrelationId, retrievedIds);
        Assert.Contains(task2CorrelationId, retrievedIds);
        Assert.Equal(2, retrievedIds.Count);
    }
}