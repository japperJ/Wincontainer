using FluentAssertions;
using WinContainers.AI;

namespace WinContainers.Tests.Unit.Ai;

public class AgentErrorClassifierTests
{
    [Theory]
    [InlineData("HTTP 503 (server_error: chat_admission_busy)")]
    [InlineData("HTTP 429 Too Many Requests")]
    [InlineData("HTTP 502 Bad Gateway")]
    [InlineData("HTTP 504 Gateway Timeout")]
    [InlineData("Rate limit exceeded, slow down.")]
    [InlineData("The service is temporarily unavailable.")]
    [InlineData("Provider overloaded, try again later.")]
    public void IsRetryable_ShouldReturnTrue_ForTransientErrors(string message)
    {
        AgentErrorClassifier.IsRetryable(new InvalidOperationException(message)).Should().BeTrue();
    }

    [Theory]
    [InlineData("HTTP 400 (invalid_api_key)")]
    [InlineData("HTTP 401 Unauthorized")]
    [InlineData("HTTP 404 model not found")]
    [InlineData("Connection refused")]
    [InlineData("")]
    public void IsRetryable_ShouldReturnFalse_ForNonTransientErrors(string message)
    {
        AgentErrorClassifier.IsRetryable(new InvalidOperationException(message)).Should().BeFalse();
    }

    [Fact]
    public void IsRetryable_ShouldInspectInnerException()
    {
        var inner = new InvalidOperationException("server_error: chat_admission_busy");
        var outer = new InvalidOperationException("The AI assistant ran into a problem.", inner);

        AgentErrorClassifier.IsRetryable(outer).Should().BeTrue();
    }
}
