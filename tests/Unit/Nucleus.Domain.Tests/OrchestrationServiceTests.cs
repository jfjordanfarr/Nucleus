// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Moq;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Results;
using Nucleus.Domain.Processing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nucleus.Domain.Tests;

public class OrchestrationServiceTests
{
    private readonly Mock<ILogger<OrchestrationService>> _mockLogger;
    private readonly Mock<IActivationChecker> _mockActivationChecker;
    private readonly Mock<IBackgroundTaskQueue> _mockBackgroundTaskQueue;
    private readonly Mock<IPersonaConfigurationProvider> _mockPersonaConfigurationProvider;
    private readonly OrchestrationService _orchestrationService;

    public OrchestrationServiceTests()
    {
        _mockLogger = new Mock<ILogger<OrchestrationService>>();
        _mockActivationChecker = new Mock<IActivationChecker>();
        _mockBackgroundTaskQueue = new Mock<IBackgroundTaskQueue>();
        _mockPersonaConfigurationProvider = new Mock<IPersonaConfigurationProvider>();

        _orchestrationService = new OrchestrationService(
            _mockLogger.Object,
            _mockActivationChecker.Object,
            _mockBackgroundTaskQueue.Object,
            _mockPersonaConfigurationProvider.Object);
    }

    private static AdapterRequest CreateValidAdapterRequest(string? messageId = null) => new AdapterRequest(
        PlatformType: Abstractions.Models.PlatformType.Test,
        ConversationId: "test-conversation-id",
        UserId: "test-user-id",
        QueryText: "Hello, Nucleus!",
        MessageId: messageId ?? Guid.NewGuid().ToString()
    );

    [Fact]
    public async Task ProcessInteractionAsync_WithValidRequestAndSuccessfulActivation_ReturnsSuccessQueued()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var personaConfig = new PersonaConfiguration { PersonaId = "test-persona" };
        var activationResult = new ActivationResult(ShouldActivate: true, PersonaId: "test-persona");

        _mockPersonaConfigurationProvider
            .Setup(p => p.GetAllConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonaConfiguration> { personaConfig });

        _mockActivationChecker
            .Setup(ac => ac.CheckActivationAsync(request, It.IsAny<IEnumerable<PersonaConfiguration>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activationResult);

        _mockBackgroundTaskQueue
            .Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<NucleusIngestionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask); // Corrected from Task.CompletedTask

        // Act
        var result = await _orchestrationService.ProcessInteractionAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.SuccessValue);
        Assert.True(result.SuccessValue.Success);
        Assert.Contains("Request for persona 'test-persona' received and queued", result.SuccessValue.ResponseMessage);
        _mockBackgroundTaskQueue.Verify(q => q.QueueBackgroundWorkItemAsync(
            It.Is<NucleusIngestionRequest>(nir => nir.ResolvedPersonaId == "test-persona"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessInteractionAsync_WithFailedActivation_ReturnsFailureActivationCheckFailed()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var personaConfig = new PersonaConfiguration { PersonaId = "test-persona" };
        // Simulate activation check failing
        var activationResult = new ActivationResult(ShouldActivate: false, PersonaId: null); 

        _mockPersonaConfigurationProvider
            .Setup(p => p.GetAllConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonaConfiguration> { personaConfig });

        _mockActivationChecker
            .Setup(ac => ac.CheckActivationAsync(request, It.IsAny<IEnumerable<PersonaConfiguration>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activationResult);

        // Act
        var result = await _orchestrationService.ProcessInteractionAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(OrchestrationError.ActivationCheckFailed, result.ErrorValue);
        _mockBackgroundTaskQueue.Verify(q => q.QueueBackgroundWorkItemAsync(
            It.IsAny<NucleusIngestionRequest>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessInteractionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        AdapterRequest? request = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _orchestrationService.ProcessInteractionAsync(request!, CancellationToken.None));
         _mockBackgroundTaskQueue.Verify(q => q.QueueBackgroundWorkItemAsync(
            It.IsAny<NucleusIngestionRequest>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task ProcessInteractionAsync_ActivationCheckerThrowsException_ReturnsFailureUnknownError()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var personaConfig = new PersonaConfiguration { PersonaId = "test-persona" };

        _mockPersonaConfigurationProvider
            .Setup(p => p.GetAllConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonaConfiguration> { personaConfig });

        _mockActivationChecker
            .Setup(ac => ac.CheckActivationAsync(request, It.IsAny<IEnumerable<PersonaConfiguration>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception from activation checker"));

        // Act
        var result = await _orchestrationService.ProcessInteractionAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(OrchestrationError.UnknownError, result.ErrorValue);
         _mockBackgroundTaskQueue.Verify(q => q.QueueBackgroundWorkItemAsync(
            It.IsAny<NucleusIngestionRequest>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessInteractionAsync_BackgroundTaskQueueThrowsException_ReturnsFailureUnknownError()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var personaConfig = new PersonaConfiguration { PersonaId = "test-persona" };
        var activationResult = new ActivationResult(ShouldActivate: true, PersonaId: "test-persona");

        _mockPersonaConfigurationProvider
            .Setup(p => p.GetAllConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonaConfiguration> { personaConfig });

        _mockActivationChecker
            .Setup(ac => ac.CheckActivationAsync(request, It.IsAny<IEnumerable<PersonaConfiguration>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activationResult);

        _mockBackgroundTaskQueue
            .Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<NucleusIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception from background task queue"));
            
        // Act
        var result = await _orchestrationService.ProcessInteractionAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(OrchestrationError.UnknownError, result.ErrorValue);
    }

    [Fact]
    public async Task ProcessInteractionAsync_PersonaConfigurationProviderThrowsException_ReturnsFailureUnknownError()
    {
        // Arrange
        var request = CreateValidAdapterRequest();

        _mockPersonaConfigurationProvider
            .Setup(p => p.GetAllConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception from persona config provider"));
            
        // Act
        var result = await _orchestrationService.ProcessInteractionAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(OrchestrationError.UnknownError, result.ErrorValue);
        _mockActivationChecker.Verify(ac => ac.CheckActivationAsync(
            It.IsAny<AdapterRequest>(), It.IsAny<IEnumerable<PersonaConfiguration>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockBackgroundTaskQueue.Verify(q => q.QueueBackgroundWorkItemAsync(
            It.IsAny<NucleusIngestionRequest>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
