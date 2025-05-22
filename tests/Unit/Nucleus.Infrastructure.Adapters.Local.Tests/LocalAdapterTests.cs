// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Moq;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Results;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Infrastructure.Adapters.Local;
using Nucleus.Abstractions.Models; // Required for PlatformType

namespace Nucleus.Infrastructure.Adapters.Local.Tests;

public class LocalAdapterTests
{
    private readonly Mock<ILogger<LocalAdapter>> _mockLogger;
    private readonly Mock<IOrchestrationService> _mockOrchestrationService;
    private readonly LocalAdapter _adapter; // Renamed from _localAdapter for clarity

    public LocalAdapterTests()
    {
        _mockLogger = new Mock<ILogger<LocalAdapter>>();
        _mockOrchestrationService = new Mock<IOrchestrationService>();
        _adapter = new LocalAdapter(_mockLogger.Object, _mockOrchestrationService.Object); // Use renamed field
    }

    private static AdapterRequest CreateValidAdapterRequest(string queryText = "Hello", string? messageId = null) => new(
        PlatformType: PlatformType.Test,
        ConversationId: "test-conversation",
        UserId: "test-user",
        QueryText: queryText,
        MessageId: messageId ?? Guid.NewGuid().ToString()
    );

    [Fact]
    public async Task PersistInteractionAsync_WithValidRequest_LogsInformation()
    {
        // Arrange
        var request = CreateValidAdapterRequest();

        // Act
        await _adapter.PersistInteractionAsync(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalAdapter: Interaction audit log.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PersistInteractionAsync_WithNullRequest_LogsWarningAndCompletes()
    {
        // Arrange
        AdapterRequest? request = null;

        // Act
        await _adapter.PersistInteractionAsync(request!, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PersistInteractionAsync called with a null request")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Tests for SubmitInteractionAsync
    [Fact]
    public async Task SubmitInteractionAsync_OrchestrationServiceSucceeds_ReturnsSuccessResponse()
    {
        // Arrange
        var request = CreateValidAdapterRequest(messageId: "test-id-success");
        var expectedResponse = new AdapterResponse(true, "Success");
        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterResponse, OrchestrationError>.Success(expectedResponse));

        // Act
        var result = await _adapter.SubmitInteractionAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedResponse.ResponseMessage, result.ResponseMessage);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("successfully processed by orchestration service")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitInteractionAsync_OrchestrationServiceFails_ReturnsFailureResponse()
    {
        // Arrange
        var request = CreateValidAdapterRequest(messageId: "test-id-fail");
        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.ArtifactProcessingFailed));

        // Act
        var result = await _adapter.SubmitInteractionAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Orchestration failed: ArtifactProcessingFailed", result.ErrorMessage);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Orchestration service indicated failure")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitInteractionAsync_NullRequest_ReturnsFailureResponse()
    {
        // Act
        var result = await _adapter.SubmitInteractionAsync(null!); // Explicitly pass null

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Request cannot be null.", result.ResponseMessage);
        Assert.Equal("AdapterRequest was null.", result.ErrorMessage);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SubmitInteractionAsync called with a null request")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitInteractionAsync_OrchestrationServiceThrowsException_ReturnsFailureResponse()
    {
        // Arrange
        var request = CreateValidAdapterRequest(messageId: "test-id-exception");
        var exceptionMessage = "Test exception";
        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _adapter.SubmitInteractionAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("An unexpected error occurred while submitting the interaction.", result.ResponseMessage);
        Assert.Equal(exceptionMessage, result.ErrorMessage);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception during LocalAdapter.SubmitInteractionAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Remove duplicated/old tests if any
    // The tests below this comment seem to be duplicates or older versions of the ones above for SubmitInteractionAsync
    // and PersistInteractionAsync. They should be removed to avoid confusion and redundant test execution.
    // Keeping them for now to ensure no logic is lost, but they should be cleaned up.

    // [Fact]
    // public async Task SendInteractionRequestAsync_WithValidRequest_AndOrchestrationSucceeds_ReturnsSuccessResult()
    // {
    //     // ... old code ...
    // }

    // [Fact]
    // public async Task SendInteractionRequestAsync_WithValidRequest_AndOrchestrationFails_ReturnsFailureResult()
    // {
    //     // ... old code ...
    // }
    
    // [Fact]
    // public async Task SendInteractionRequestAsync_WithNullRequest_ThrowsArgumentNullException()
    // {
    //     // ... old code ...
    // }

    // [Fact]
    // public async Task SendInteractionRequestAsync_OrchestrationServiceThrowsException_ReturnsFailureResultWithUnknownError()
    // {
    //     // ... old code ...
    // }

    // [Fact]
    // public async Task SendInteractionRequestAsync_OrchestrationServiceSucceeds_ReturnsSuccessResponse()
    // {
    //    // ... old code ...
    // }

    // [Fact]
    // public async Task SendInteractionRequestAsync_OrchestrationServiceFails_ReturnsFailureResponse()
    // {
    //     // ... old code ...
    // }

    // [Fact]
    // public async Task SendInteractionRequestAsync_NullRequest_ReturnsFailureResponse()
    // {
    //     // ... old code ...
    // }


    // [Fact]
    // public async Task SendInteractionRequestAsync_OrchestrationServiceThrowsException_ReturnsFailureResponse()
    // {
    //     // ... old code ...
    // }
}
