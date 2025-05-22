using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Threading;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Nucleus.Domain.Processing;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.ApiContracts; // Added for AdapterRequest
using Nucleus.Domain.Personas.Core.Interfaces;
using Nucleus.Abstractions.Adapters;
using System.IO; // Added for MemoryStream
using System.Text; // Added for Encoding
using Nucleus.Abstractions.Exceptions; // Added for NotificationFailedException
using Nucleus.Abstractions.Extraction; // Added for IContentExtractor

namespace Nucleus.Domain.Tests;

/// <summary>
/// Contains unit tests for the <see cref="QueuedInteractionProcessorService"/>.
/// These tests verify the core logic of processing queued interaction requests,
/// including interactions with persona configurations, persona runtimes, and notification services.
/// </summary>
/// <seealso href="../../../../Docs/Architecture/09_ARCHITECTURE_TESTING.md" />
public class QueuedInteractionProcessorServiceTests : IDisposable
{
    private readonly Mock<ILogger<QueuedInteractionProcessorService>> _mockLogger;
    private readonly Mock<IBackgroundTaskQueue> _mockBackgroundTaskQueue;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IPersonaConfigurationProvider> _mockPersonaConfigProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IPersonaRuntime> _mockPersonaRuntime;
    private readonly Mock<IPlatformNotifier> _mockPlatformNotifier;
    private readonly List<Mock<IArtifactProvider>> _mockArtifactProviders;
    private readonly Mock<IArtifactProvider> _mockArtifactProvider1;
    private readonly Mock<IEnumerable<IContentExtractor>> _mockContentExtractors;

    private readonly QueuedInteractionProcessorService _service;
    private bool disposedValue;

    // Helper methods for creating test data
    private static AdapterRequest CreateAdapterRequest(
        PlatformType platformType = PlatformType.Test,
        string conversationId = "test-conversation",
        string userId = "test-user",
        string queryText = "Hello",
        // Corrected order and types based on AdapterRequest record definition
        string? messageId = null,
        string? replyToMessageId = null,
        List<ArtifactReference>? artifactReferences = null,
        Dictionary<string, string>? metadata = null,
        string? interactionType = null,
        string? tenantId = "test-tenant",
        string? personaId = "DefaultPersona",
        DateTimeOffset? timestampUtc = null)
    {
        return new AdapterRequest(
            PlatformType: platformType,
            ConversationId: conversationId,
            UserId: userId,
            QueryText: queryText,
            MessageId: messageId,
            ReplyToMessageId: replyToMessageId,
            ArtifactReferences: artifactReferences ?? new List<ArtifactReference>(),
            Metadata: metadata,
            InteractionType: interactionType,
            TenantId: tenantId,
            PersonaId: personaId,
            TimestampUtc: timestampUtc ?? DateTimeOffset.UtcNow
        );
    }

    private static NucleusIngestionRequest CreateNucleusIngestionRequest(
        AdapterRequest adapterRequest,
        string? correlationId = null)
    {
        return new NucleusIngestionRequest(
            PlatformType: adapterRequest.PlatformType,
            OriginatingUserId: adapterRequest.UserId,
            OriginatingConversationId: adapterRequest.ConversationId,
            OriginatingReplyToMessageId: adapterRequest.ReplyToMessageId,
            OriginatingMessageId: adapterRequest.MessageId,
            ResolvedPersonaId: adapterRequest.PersonaId!,
            TimestampUtc: adapterRequest.TimestampUtc ?? DateTimeOffset.UtcNow,
            QueryText: adapterRequest.QueryText,
            ArtifactReferences: adapterRequest.ArtifactReferences,
            CorrelationId: correlationId,
            Metadata: adapterRequest.Metadata,
            TenantId: adapterRequest.TenantId // Added TenantId
        );
    }

    private static PersonaConfiguration CreatePersonaConfiguration(
        string personaId = "test-persona",
        string displayName = "Test Persona",
        bool isEnabled = true,
        string tenantId = "test-tenant",
        string chatModelId = "gpt-4",
        string systemMessage = "Test System Message")
    {
        return new PersonaConfiguration
        {
            PersonaId = personaId,
            DisplayName = displayName,
            IsEnabled = isEnabled,
            SystemMessage = systemMessage,
            LlmConfiguration = new LlmConfiguration
            {
                ChatModelId = chatModelId
            },
            DataGovernance = new DataGovernanceConfiguration
            {
                AllowedTenantIds = { tenantId }
            }
        };
    }


    public QueuedInteractionProcessorServiceTests()
    {
        _mockLogger = new Mock<ILogger<QueuedInteractionProcessorService>>();
        _mockBackgroundTaskQueue = new Mock<IBackgroundTaskQueue>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockPersonaConfigProvider = new Mock<IPersonaConfigurationProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockPersonaRuntime = new Mock<IPersonaRuntime>();
        _mockPlatformNotifier = new Mock<IPlatformNotifier>();
        _mockArtifactProviders = new List<Mock<IArtifactProvider>>();
        _mockArtifactProvider1 = new Mock<IArtifactProvider>();
        _mockArtifactProviders.Add(_mockArtifactProvider1);
        _mockContentExtractors = new Mock<IEnumerable<IContentExtractor>>();

        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);

        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IPersonaRuntime))).Returns(_mockPersonaRuntime.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<IPlatformNotifier>)))
            .Returns(() => new[] { _mockPlatformNotifier.Object });
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<QueuedInteractionProcessorService>)))
            .Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<IArtifactProvider>)))
            .Returns(() => _mockArtifactProviders.Select(m => m.Object));
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_mockServiceScopeFactory.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IPersonaConfigurationProvider))).Returns(_mockPersonaConfigProvider.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<IContentExtractor>))).Returns(_mockContentExtractors.Object);

        // Corrected constructor arguments for QueuedInteractionProcessorService
        _service = new QueuedInteractionProcessorService(
            _mockLogger.Object,
            _mockBackgroundTaskQueue.Object,
            _mockServiceProvider.Object, // IServiceProvider
            _mockArtifactProviders.Select(m => m.Object).ToList(), // IEnumerable<IArtifactProvider>
            _mockContentExtractors.Object // IEnumerable<IContentExtractor>
            );
    }

    // Standard IDisposable implementation
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                _service?.Dispose();
            }

            // No unmanaged resources to free
            // No large fields to set to null
            disposedValue = true;
        }
    }

    ~QueuedInteractionProcessorServiceTests()
    {
        Dispose(false);
    }

    [Fact]
    public async Task ProcessRequestAsync_ValidMessage_ShouldProcessSuccessfully()
    {
        var adapterRequest = CreateAdapterRequest(personaId: "DefaultPersona", tenantId: "test-tenant");
        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, new object());
        var cancellationToken = CancellationToken.None;

        var personaConfiguration = CreatePersonaConfiguration(personaId: adapterRequest.PersonaId!, tenantId: adapterRequest.TenantId!);

        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken))
            .ReturnsAsync(personaConfiguration);

        var expectedPersonaResponse = new AdapterResponse(true, "Processed successfully");
        _mockPersonaRuntime.Setup(p => p.ExecuteAsync(
                It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfiguration.PersonaId),
                It.Is<InteractionContext>(ic =>
                    ic.ResolvedPersonaId == personaConfiguration.PersonaId &&
                    ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText && // Restored some checks
                    ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                    ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                    ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                    ic.OriginalRequest.PersonaId == nucleusIngestionRequest.ResolvedPersonaId &&
                    (ic.OriginalRequest.ArtifactReferences == null || !ic.OriginalRequest.ArtifactReferences.Any()) &&
                    !ic.RawArtifacts.Any()
                ),
                cancellationToken))
            .ReturnsAsync((expectedPersonaResponse, PersonaExecutionStatus.Success));

        _mockPlatformNotifier.Setup(n => n.SupportedPlatformType).Returns(adapterRequest.PlatformType.ToString());
        _mockPlatformNotifier.Setup(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            cancellationToken
        )).ReturnsAsync((true, "test-sent-message-id", null as string));

        await _service.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken), Times.Once);
        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(
            It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfiguration.PersonaId),
            It.Is<InteractionContext>(ic =>
                ic.ResolvedPersonaId == personaConfiguration.PersonaId &&
                ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText && // Restored some checks
                ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                ic.OriginalRequest.PersonaId == nucleusIngestionRequest.ResolvedPersonaId &&
                (ic.OriginalRequest.ArtifactReferences == null || !ic.OriginalRequest.ArtifactReferences.Any()) &&
                !ic.RawArtifacts.Any()
            ),
            cancellationToken), Times.Once);
        _mockPlatformNotifier.Verify(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, It.IsAny<Exception>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task ProcessRequestAsync_PersonaRuntimeThrowsException_ShouldAbandonMessage()
    {
        var adapterRequest = CreateAdapterRequest(
            personaId: "FaultyPersona",
            tenantId: "test-tenant-runtime-ex",
            queryText: "Hello with runtime exception",
            conversationId: "test-conversation-runtime-ex",
            userId: "test-user-runtime-ex"
        );
        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, new object());
        var cancellationToken = CancellationToken.None;

        // Use a more specific personaId in the config to ensure the mock is correctly targeted
        var personaConfig = CreatePersonaConfiguration(personaId: adapterRequest.PersonaId!, displayName: "FaultyPersona");
        var runtimeException = new InvalidOperationException("Runtime error");

        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken))
            .ReturnsAsync(personaConfig);

        _mockPersonaRuntime.Setup(p => p.ExecuteAsync(
                It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
                It.Is<InteractionContext>(ic =>
                    ic.ResolvedPersonaId == personaConfig.PersonaId &&
                    ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText &&
                    ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                    ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                    ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                    ic.OriginalRequest.PersonaId == personaConfig.PersonaId && 
                    (ic.OriginalRequest.ArtifactReferences == null || !ic.OriginalRequest.ArtifactReferences.Any()) &&
                    !ic.RawArtifacts.Any() && 
                    !ic.ProcessedArtifacts.Any()
                    ),
                cancellationToken))
            .ThrowsAsync(runtimeException);

        await _service.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken), Times.Once);
        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(
            It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
            It.Is<InteractionContext>(ic =>
                ic.ResolvedPersonaId == personaConfig.PersonaId &&
                ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText &&
                ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                ic.OriginalRequest.PersonaId == personaConfig.PersonaId && 
                (ic.OriginalRequest.ArtifactReferences == null || !ic.OriginalRequest.ArtifactReferences.Any()) &&
                !ic.RawArtifacts.Any() &&
                !ic.ProcessedArtifacts.Any()
                ),
            cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Never);
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, runtimeException, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessRequestAsync_NotificationThrowsException_ShouldCompleteMessageAndLogError()
    {
        // Arrange
        var adapterRequest = CreateAdapterRequest(
            personaId: "testPersonaId",
            queryText: "Hello",
            messageId: "message-id-123"
            // Removed correlationId as it's not a param of AdapterRequest
        );
        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest, correlationId: "trace-002");

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, "test-message-context-notification-fail");

        var personaConfig = CreatePersonaConfiguration(
            personaId: nucleusIngestionRequest.ResolvedPersonaId!,
            displayName: "Test Persona",
            systemMessage: "Test Instructions",
            chatModelId: "gpt-4"
        );
        personaConfig.LlmConfiguration.Provider = "TestProvider";
        personaConfig.LlmConfiguration.EmbeddingModelId = "text-embedding-ada-002";

        var dummyAdapterResponse = new AdapterResponse(true, "Processed successfully by runtime");
        var personaExecutionResult = (Response: dummyAdapterResponse, Status: PersonaExecutionStatus.Success);

        _mockPersonaConfigProvider
            .Setup(p => p.GetConfigurationAsync(nucleusIngestionRequest.ResolvedPersonaId!, cancellationToken)) // Use nucleusIngestionRequest
            .ReturnsAsync(personaConfig);

        _mockPersonaRuntime
            .Setup(r => r.ExecuteAsync(
                It.Is<PersonaConfiguration>(pc => pc.PersonaId == nucleusIngestionRequest.ResolvedPersonaId),
                It.IsAny<InteractionContext>(),
                cancellationToken))
            .Returns(Task.FromResult(personaExecutionResult));

        var notificationException = new NotificationFailedException("Simulated notification failure");
        _mockPlatformNotifier
            .Setup(n => n.SendNotificationAsync(
                nucleusIngestionRequest.OriginatingConversationId, // Use nucleusIngestionRequest
                personaExecutionResult.Response.ResponseMessage!,
                nucleusIngestionRequest.OriginatingMessageId, // Use nucleusIngestionRequest
                cancellationToken))
            .ThrowsAsync(notificationException);

        _mockPlatformNotifier
            .Setup(n => n.SupportedPlatformType)
            .Returns(adapterRequest.PlatformType.ToString()); // Use adapterRequest

        // Act
        await _service.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        // Assert
        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(nucleusIngestionRequest.ResolvedPersonaId!, cancellationToken), Times.Once);
        _mockPersonaRuntime.Verify(r => r.ExecuteAsync(
            It.Is<PersonaConfiguration>(pc => pc.PersonaId == nucleusIngestionRequest.ResolvedPersonaId),
            It.IsAny<InteractionContext>(),
            cancellationToken), Times.Once);

        _mockPlatformNotifier.Verify(n => n.SendNotificationAsync(
            nucleusIngestionRequest.OriginatingConversationId,
            personaExecutionResult.Response.ResponseMessage!,
            nucleusIngestionRequest.OriginatingMessageId,
            cancellationToken), Times.Once);

        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"[BackgroundQueue:{dequeuedMessage.MessageContext}] Error during platform notification.")),
                notificationException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(
            dequeuedMessage.MessageContext,
            It.IsAny<Exception>(),
            cancellationToken
        ), Times.Never);
    }

    [Fact]
    public async Task ProcessRequestAsync_PersonaConfigNotFound_ShouldAbandonMessage()
    {
        var adapterRequest = CreateAdapterRequest(
            personaId: "NonExistentPersona",
            queryText: "Hello with config not found",
            conversationId: "test-conversation-cfg-notfound",
            userId: "test-user-cfg-notfound"
        );
        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, new object());
        var cancellationToken = CancellationToken.None;

        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken))
            .ReturnsAsync(default(PersonaConfiguration?));

        await _service.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken), Times.Once);
        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(It.IsAny<PersonaConfiguration>(), It.IsAny<InteractionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPlatformNotifier.Verify(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Never);
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, 
            It.Is<ArgumentException>(ex => ex.Message == $"Persona configuration not found for {adapterRequest.PersonaId}"), // Exact match for exception message
            cancellationToken), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v != null && 
                    v.ToString()!.Contains($"[BackgroundQueue:{dequeuedMessage.MessageContext}] Persona configuration not found for PersonaId: {adapterRequest.PersonaId}. Abandoning message.")), 
                null, // Service logs this specific error without an exception instance
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
    }

    [Fact]
    public async Task ProcessRequestAsync_PersonaConfigDisabled_ShouldAbandonMessage()
    {
        var adapterRequest = CreateAdapterRequest(
            personaId: "DisabledPersona",
            queryText: "Config disabled test",
            conversationId: "test-conversation-cfg-disabled"
        );
        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, new object());
        var cancellationToken = CancellationToken.None;

        var personaConfig = CreatePersonaConfiguration(
            personaId: "DisabledPersona", // Match the adapterRequest.PersonaId for clarity in mock setup
            displayName: "DisabledPersonaDisplay",
            isEnabled: false
        );
        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken))
            .ReturnsAsync(personaConfig);

        await _service.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken), Times.Once);
        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(It.IsAny<PersonaConfiguration>(), It.IsAny<InteractionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPlatformNotifier.Verify(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Never);
        // Verify abandon with InvalidOperationException and specific message
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, 
            It.Is<InvalidOperationException>(ex => ex.Message == $"Persona configuration {adapterRequest.PersonaId} is disabled."), 
            cancellationToken), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v != null && 
                    v.ToString()!.Contains($"[BackgroundQueue:{dequeuedMessage.MessageContext}] Persona configuration {adapterRequest.PersonaId} is disabled. Aborting processing for message {nucleusIngestionRequest.OriginatingMessageId ?? "unknown"}.")),
                null, // Service logs this specific warning without an exception instance
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithArtifacts_ProviderFound_ShouldFetchAndPassToRuntime()
    {
        var artifactReference1 = new ArtifactReference(
            ReferenceId: "artifact1",
            ReferenceType: "TestArtifactType",
            SourceUri: new Uri("test://artifact1"),
            TenantId: "test-tenant"
        );
        var adapterRequest = CreateAdapterRequest(
            personaId: "ArtifactPersona",
            tenantId: "test-tenant-artifacts",
            queryText: "Hello with artifacts",
            artifactReferences: new List<ArtifactReference> { artifactReference1 },
            conversationId: "test-conversation-artifacts",
            userId: "test-user-artifacts"
        );

        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, new object());
        var cancellationToken = CancellationToken.None;

        var personaConfig = CreatePersonaConfiguration(personaId: adapterRequest.PersonaId!, displayName: "ArtifactPersona", tenantId: adapterRequest.TenantId!);
        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken))
            .ReturnsAsync(personaConfig);

        var expectedPersonaResponse = new AdapterResponse(true, "Processed successfully");
        
        using var fetchedArtifactContentStream = new MemoryStream(Encoding.UTF8.GetBytes("This is fetched content."));
        // The CA2000 warning was here. ArtifactContent is IDisposable.
        using var fetchedArtifactContent = new ArtifactContent(
            originalReference: artifactReference1,
            contentStream: fetchedArtifactContentStream, 
            contentType: "text/plain",
            textEncoding: Encoding.UTF8
        );

        var mockArtifactProvider = new Mock<IArtifactProvider>();
        mockArtifactProvider.Setup(p => p.SupportedReferenceTypes).Returns(new[] { "TestArtifactType" });
        mockArtifactProvider.Setup(p => p.GetContentAsync(artifactReference1, cancellationToken))
            .ReturnsAsync(fetchedArtifactContent);

        var specificProviders = new List<IArtifactProvider> { mockArtifactProvider.Object }; 
        
        // Ensure no content extractors are available for this specific test to guarantee ProcessedArtifacts is empty
        // The CA2000 warning was here. QueuedInteractionProcessorService is IDisposable.
        // This instance is already explicitly disposed at the end of the test, so a using statement is not strictly necessary
        // but can be added for consistency if preferred. However, the explicit Dispose() call is sufficient.
        // For now, I will leave the explicit Dispose() as it was already in place.
        // If the CA2000 warning persists for this line after the other fixes, we can revisit.
        var serviceWithSpecificProviderAndNoExtractors = new QueuedInteractionProcessorService(
            _mockLogger.Object,
            _mockBackgroundTaskQueue.Object,
            _mockServiceProvider.Object, 
            specificProviders, 
            new List<IContentExtractor>() // Pass an empty list of content extractors
        );

        _mockPersonaRuntime.Setup(p => p.ExecuteAsync(
                It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
                It.Is<InteractionContext>(ic =>
                    ic.ResolvedPersonaId == personaConfig.PersonaId &&
                    ic.RawArtifacts.Count == 1 && 
                    ic.RawArtifacts[0].OriginalReference.ReferenceId == artifactReference1.ReferenceId &&
                    ic.RawArtifacts[0].OriginalReference.ReferenceType == artifactReference1.ReferenceType &&
                    ic.RawArtifacts[0].ContentType == "text/plain" && 
                    ic.ProcessedArtifacts.Count == 0 && // This should now hold true
                    ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText &&
                    ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                    ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                    ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                    ic.OriginalRequest.PersonaId == personaConfig.PersonaId &&
                    ((ic.OriginalRequest.ArtifactReferences != null && ic.OriginalRequest.ArtifactReferences.Count == 1) && 
                     (ic.OriginalRequest.ArtifactReferences[0].ReferenceId == artifactReference1.ReferenceId))
                    ),
                cancellationToken))
            .ReturnsAsync((expectedPersonaResponse, PersonaExecutionStatus.Success));

        _mockPlatformNotifier.Setup(n => n.SupportedPlatformType).Returns(adapterRequest.PlatformType.ToString());
        _mockPlatformNotifier.Setup(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            cancellationToken
        )).ReturnsAsync((true, "test-sent-message-id", null as string));
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<IPlatformNotifier>)))
            .Returns(new[] { _mockPlatformNotifier.Object });

        await serviceWithSpecificProviderAndNoExtractors.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken), Times.Once);
        mockArtifactProvider.Verify(p => p.GetContentAsync(artifactReference1, cancellationToken), Times.Once);

        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(
            It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
            It.Is<InteractionContext>(ic =>
                ic.ResolvedPersonaId == personaConfig.PersonaId &&
                ic.RawArtifacts.Count == 1 &&
                ic.RawArtifacts[0].OriginalReference.ReferenceId == artifactReference1.ReferenceId &&
                ic.RawArtifacts[0].OriginalReference.ReferenceType == artifactReference1.ReferenceType &&
                ic.RawArtifacts[0].ContentType == "text/plain" &&
                ic.ProcessedArtifacts.Count == 0 && // Verify this condition
                ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText &&
                ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                ic.OriginalRequest.PersonaId == personaConfig.PersonaId &&
                ((ic.OriginalRequest.ArtifactReferences != null && ic.OriginalRequest.ArtifactReferences.Count == 1) && 
                 (ic.OriginalRequest.ArtifactReferences[0].ReferenceId == artifactReference1.ReferenceId))
                ),
            cancellationToken), Times.Once);

        _mockPlatformNotifier.Verify(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, It.IsAny<Exception>(), cancellationToken), Times.Never);
        
        // Dispose the service instance created for this test
        serviceWithSpecificProviderAndNoExtractors.Dispose();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithArtifacts_NoMatchingProvider_ShouldProceedWithoutArtifactsAndLogWarning()
    {
        var artifactReference1 = new ArtifactReference(
            ReferenceId: "test-artifact-id-1",
            ReferenceType: "UnsupportedArtifactType",
            SourceUri: new Uri("http://example.com/artifact1"),
            TenantId: "test-tenant"
        );
        var adapterRequest = CreateAdapterRequest(
            personaId: "NoProviderPersona",
            tenantId: "test-tenant-no-provider",
            queryText: "Hello with unsupportable artifacts",
            artifactReferences: new List<ArtifactReference> { artifactReference1 },
            conversationId: "test-conversation-no-provider",
            userId: "test-user-no-provider"
        );

        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, new object());
        var cancellationToken = CancellationToken.None;

        var personaConfig = CreatePersonaConfiguration(personaId: adapterRequest.PersonaId!, displayName: "NoProviderPersona", tenantId: adapterRequest.TenantId!);
        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken))
            .ReturnsAsync(personaConfig);

        // Ensure no providers are available for this test by creating a new service instance with an empty list
        // The CA2000 warning was here. QueuedInteractionProcessorService is IDisposable.
        using var serviceWithNoProviders = new QueuedInteractionProcessorService(
            _mockLogger.Object,
            _mockBackgroundTaskQueue.Object,
            _mockServiceProvider.Object,
            new List<IArtifactProvider>(), // Empty list of artifact providers
            _mockContentExtractors.Object
        );

        var expectedPersonaResponse = new AdapterResponse(true, "Processed without artifacts as none were resolvable.");
        var expectedOriginalArtifactCount = nucleusIngestionRequest.ArtifactReferences?.Count ?? 0;
        _mockPersonaRuntime.Setup(p => p.ExecuteAsync(
                It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
                It.Is<InteractionContext>(ic =>
                    ic.ResolvedPersonaId == personaConfig.PersonaId &&
                    ic.RawArtifacts.Count == 0 && // Expect no raw artifacts
                    ic.ProcessedArtifacts.Count == 0 && // Expect no processed artifacts
                    ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText &&
                    ic.OriginalRequest.PlatformType == nucleusIngestionRequest.PlatformType &&
                    ic.OriginalRequest.UserId == nucleusIngestionRequest.OriginatingUserId &&
                    ic.OriginalRequest.ConversationId == nucleusIngestionRequest.OriginatingConversationId &&
                    ic.OriginalRequest.PersonaId == personaConfig.PersonaId &&
                    ((ic.OriginalRequest.ArtifactReferences == null && expectedOriginalArtifactCount == 0) || 
                     (ic.OriginalRequest.ArtifactReferences != null && ic.OriginalRequest.ArtifactReferences.Count == expectedOriginalArtifactCount))
                    ),
                cancellationToken))
        .ReturnsAsync((expectedPersonaResponse, PersonaExecutionStatus.Success));

        _mockPlatformNotifier.Setup(n => n.SupportedPlatformType).Returns(adapterRequest.PlatformType.ToString());
        _mockPlatformNotifier.Setup(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            cancellationToken
        )).ReturnsAsync((true, "test-sent-message-id", null as string));
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<IPlatformNotifier>)))
            .Returns(new[] { _mockPlatformNotifier.Object });

        // Act: Use the service instance with no providers
        await serviceWithNoProviders.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, cancellationToken);

        // Assert
        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, cancellationToken), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"No IArtifactProvider found for Artifact ReferenceId: {artifactReference1.ReferenceId} with ReferenceType: {artifactReference1.ReferenceType}. This artifact will be skipped.")),
                null, 
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(
            It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
            It.Is<InteractionContext>(ic =>
                ic.ResolvedPersonaId == personaConfig.PersonaId &&
                ic.RawArtifacts.Count == 0 && 
                ic.ProcessedArtifacts.Count == 0 &&
                ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText
                ),
            cancellationToken), Times.Once);

        _mockPlatformNotifier.Verify(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, cancellationToken), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, It.IsAny<Exception>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithArtifacts_ProviderThrowsException_ShouldProceedWithoutArtifactsAndLogError()
    {
        string logVerificationMessageContext = "test-context-for-artifact-exception";

        var artifactReference1 = new ArtifactReference(
            "test-ref-id-1",
            "file",
            new Uri("file:///test/file1.txt"),
            "test-tenant-id" 
        );

        var adapterRequest = CreateAdapterRequest(
            personaId: "test-persona-id-ex", // Unique personaId for this test
            queryText: "Test query with artifact exception",
            artifactReferences: new List<ArtifactReference> { artifactReference1 },
            tenantId: artifactReference1.TenantId 
        );
        var nucleusIngestionRequest = CreateNucleusIngestionRequest(adapterRequest);
        var dequeuedMessage = new DequeuedMessage<NucleusIngestionRequest>(nucleusIngestionRequest, logVerificationMessageContext);

        var personaConfig = CreatePersonaConfiguration(personaId: adapterRequest.PersonaId!, isEnabled: true, tenantId: artifactReference1.TenantId! ); 

        var providerException = new InvalidOperationException("Provider failed to fetch");
        
        var specificMockArtifactProvider = new Mock<IArtifactProvider>();
        specificMockArtifactProvider.Setup(p => p.SupportedReferenceTypes).Returns(new[] { "file" });
        specificMockArtifactProvider.Setup(p => p.GetContentAsync(artifactReference1, It.IsAny<CancellationToken>()))
                                   .ThrowsAsync(providerException);
        
        // Create a new service instance with only the throwing provider
        // The CA2000 warning was here. QueuedInteractionProcessorService is IDisposable.
        using var serviceWithThrowingProvider = new QueuedInteractionProcessorService(
            _mockLogger.Object,
            _mockBackgroundTaskQueue.Object,
            _mockServiceProvider.Object,
            new List<IArtifactProvider> { specificMockArtifactProvider.Object }, // Only the throwing provider
            _mockContentExtractors.Object
        );

        _mockPersonaConfigProvider.Setup(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, It.IsAny<CancellationToken>())) 
                                  .ReturnsAsync(personaConfig);
        
        var expectedPersonaResponse = new AdapterResponse(true, "Processed despite artifact error");
        _mockPersonaRuntime.Setup(p => p.ExecuteAsync(
                It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
                It.Is<InteractionContext>(ic => 
                    ic.RawArtifacts.Count == 0 && 
                    ic.ProcessedArtifacts.Count == 0 &&
                    ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText // Ensure other parts of context are as expected
                    ), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedPersonaResponse, PersonaExecutionStatus.Success));

        _mockPlatformNotifier.Setup(n => n.SupportedPlatformType).Returns(adapterRequest.PlatformType.ToString());
        _mockPlatformNotifier.Setup(n => n.SendNotificationAsync(
            It.IsAny<string>(),
            expectedPersonaResponse.ResponseMessage,
            dequeuedMessage.Payload.OriginatingMessageId,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((true, "test-sent-message-id", null as string));

        // Act
        await serviceWithThrowingProvider.ProcessRequestAsync(dequeuedMessage.Payload, dequeuedMessage.MessageContext, CancellationToken.None);

        // Assert
        _mockPersonaConfigProvider.Verify(p => p.GetConfigurationAsync(adapterRequest.PersonaId!, It.IsAny<CancellationToken>()), Times.Once);
        specificMockArtifactProvider.Verify(p => p.GetContentAsync(artifactReference1, It.IsAny<CancellationToken>()), Times.Once); // Verify the provider was called

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v != null && v.ToString()!.Contains($"[BackgroundQueue:{logVerificationMessageContext}] Error fetching content for Artifact ReferenceId: {artifactReference1.ReferenceId} using provider {specificMockArtifactProvider.Object.GetType().Name}")),
                providerException, 
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

        _mockPersonaRuntime.Verify(p => p.ExecuteAsync(
            It.Is<PersonaConfiguration>(pc => pc.PersonaId == personaConfig.PersonaId),
            It.Is<InteractionContext>(ic => 
                ic.RawArtifacts.Count == 0 && 
                ic.ProcessedArtifacts.Count == 0 &&
                ic.OriginalRequest.QueryText == nucleusIngestionRequest.QueryText
                ),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _mockBackgroundTaskQueue.Verify(q => q.CompleteAsync(dequeuedMessage.MessageContext, CancellationToken.None), Times.Once);
        _mockBackgroundTaskQueue.Verify(q => q.AbandonAsync(dequeuedMessage.MessageContext, It.IsAny<Exception>(), CancellationToken.None), Times.Never);
    }
}
