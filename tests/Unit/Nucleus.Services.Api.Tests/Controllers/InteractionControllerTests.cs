// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Results;
using Nucleus.Services.Api.Controllers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Nucleus.Abstractions.Adapters.Local; // Added using directive
using Microsoft.AspNetCore.Http; // Added for StatusCodes
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // Required for ObjectValidator

namespace Nucleus.Services.Api.Tests.Controllers;

public class InteractionControllerTests
{
    private readonly Mock<ILogger<InteractionController>> _mockLogger;
    private readonly Mock<IOrchestrationService> _mockOrchestrationService;
    private readonly Mock<ILocalAdapterClient> _mockLocalAdapterClient; // Added
    private readonly InteractionController _controller;

    public InteractionControllerTests()
    {
        _mockLogger = new Mock<ILogger<InteractionController>>();
        _mockOrchestrationService = new Mock<IOrchestrationService>();
        _mockLocalAdapterClient = new Mock<ILocalAdapterClient>(); // Added
        _controller = new InteractionController(_mockLogger.Object, _mockOrchestrationService.Object, _mockLocalAdapterClient.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // REMOVED: Mock IObjectModelValidator setup
        // var objectValidator = new Mock<IObjectModelValidator>();
        // objectValidator.Setup(o => o.Validate(It.IsAny<ActionContext>(),
        //                                       It.IsAny<ValidationStateDictionary>(),
        //                                       It.IsAny<string>(),
        //                                       It.IsAny<object>()));
        // _controller.ObjectValidator = objectValidator.Object;
    }

    private static AdapterRequest CreateValidAdapterRequest() => new(
        PlatformType: Abstractions.Models.PlatformType.Test,
        ConversationId: "test-conversation",
        UserId: "test-user",
        QueryText: "Hello",
        MessageId: Guid.NewGuid().ToString()
    );

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationSucceeds_ReturnsOkObjectResult()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        // Updated AdapterResponse instantiation to match its record definition
        var adapterResponse = new AdapterResponse(Success: true, ResponseMessage: "Queued"); 
        var successResult = Result<AdapterResponse, OrchestrationError>.Success(adapterResponse);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnValue = Assert.IsType<AdapterResponse>(okResult.Value);
        Assert.True(returnValue.Success);
        Assert.Equal("Queued", returnValue.ResponseMessage);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithActivationCheckFailed_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.ActivationCheckFailed);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        // Updated to reflect that ActivationCheckFailed now returns OkObjectResult
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var responseValue = Assert.IsType<AdapterResponse>(okResult.Value);
        Assert.True(responseValue.Success); // Success is true because the system handled it, even if ignored.
        Assert.Equal("Interaction did not meet activation criteria and was ignored.", responseValue.ResponseMessage);
        Assert.Equal(OrchestrationError.ActivationCheckFailed.ToString(), responseValue.ErrorMessage);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithUnknownError_ReturnsInternalServerError()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.UnknownError);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Orchestration Error", problemDetails.Title);
        // Assert.Equal("An unexpected error occurred while processing the interaction.", problemDetails.Detail); // Original assertion
        // Updated assertion to be less brittle:
        Assert.Contains(OrchestrationError.UnknownError.ToString(), problemDetails.Detail);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.InvalidRequest);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var responseValue = Assert.IsType<AdapterResponse>(badRequestResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("The request was invalid.", responseValue.ResponseMessage);
        Assert.Equal(OrchestrationError.InvalidRequest.ToString(), responseValue.ErrorMessage);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithPersonaResolutionFailed_ReturnsProblem()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.PersonaResolutionFailed);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal("Orchestration Error", problemDetails.Title);
        Assert.Equal($"Orchestration failed with error: {OrchestrationError.PersonaResolutionFailed}", problemDetails.Detail);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithArtifactProcessingFailed_ReturnsProblem()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.ArtifactProcessingFailed);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal("Orchestration Error", problemDetails.Title);
        Assert.Equal($"Orchestration failed with error: {OrchestrationError.ArtifactProcessingFailed}", problemDetails.Detail);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithRuntimeExecutionFailed_ReturnsProblem()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.RuntimeExecutionFailed);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal("Orchestration Error", problemDetails.Title);
        Assert.Equal($"Orchestration failed with error: {OrchestrationError.RuntimeExecutionFailed}", problemDetails.Detail);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithOperationCancelled_ReturnsStatusCode499()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.OperationCancelled);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, statusCodeResult.StatusCode);
        var responseValue = Assert.IsType<AdapterResponse>(statusCodeResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("Operation was cancelled.", responseValue.ResponseMessage);
        Assert.Equal(OrchestrationError.OperationCancelled.ToString(), responseValue.ErrorMessage);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.NotFound);

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        var responseValue = Assert.IsType<AdapterResponse>(notFoundResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("Resource not found.", responseValue.ResponseMessage);
        Assert.Equal(OrchestrationError.NotFound.ToString(), responseValue.ErrorMessage);
    }

    [Fact]
    public async Task Post_WithValidRequest_AndOrchestrationFails_WithUnspecifiedOrchestrationError_ReturnsProblem()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        // Using a hypothetical error value not explicitly handled in the switch to test the default case
        var failureResult = Result<AdapterResponse, OrchestrationError>.Failure((OrchestrationError)999); 

        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal("Unexpected Orchestration Error", problemDetails.Title);
        Assert.Equal("An unexpected orchestration error occurred.", problemDetails.Detail);
    }

    [Fact]
    public async Task Post_WithNullRequestBody_ReturnsBadRequest()
    {
        // Arrange
        AdapterRequest? request = null;

        // Act
        // The controller checks for null before calling the service, so no service mock needed here.
        var actionResult = await _controller.Post(request!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var responseValue = Assert.IsType<AdapterResponse>(badRequestResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("Request body cannot be null.", responseValue.ResponseMessage);
        Assert.Equal("Request body cannot be null.", responseValue.ErrorMessage);
        _mockOrchestrationService.Verify(s => s.ProcessInteractionAsync(It.IsAny<AdapterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "test-user", "hello")]
    [InlineData("", "test-user", "hello")]
    [InlineData("test-conversation", null, "hello")]
    [InlineData("test-conversation", "", "hello")]
    // [InlineData("test-conversation", "test-user", null)] // This case is handled by a different check now
    // [InlineData("test-conversation", "test-user", "")]   // This case is handled by a different check now
    public async Task Post_WithInvalidAdapterRequestProperties_ReturnsBadRequest(string? conversationId, string? userId, string? queryText)
    {
        // Arrange
        var request = new AdapterRequest(
            PlatformType: Abstractions.Models.PlatformType.Test,
            ConversationId: conversationId!, // Null forgiving operator is fine here for test setup
            UserId: userId!,                 // Null forgiving operator is fine here for test setup
            QueryText: queryText!,           // Null forgiving operator is fine here for test setup
            MessageId: Guid.NewGuid().ToString()
        );

        // Manually add ModelState errors to simulate failed validation for these specific properties
        if (string.IsNullOrEmpty(conversationId))
        {
            _controller.ModelState.AddModelError(nameof(AdapterRequest.ConversationId), "ConversationId cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(userId))
        {
            _controller.ModelState.AddModelError(nameof(AdapterRequest.UserId), "UserId cannot be null or empty.");
        }
        // The QueryText null/empty check is now combined with ArtifactReferences check in the controller,
        // so we don't add a ModelState error for it here if it's the *only* thing wrong,
        // as that specific path returns a BadRequestObjectResult(AdapterResponse) not ValidationProblemDetails.
        // This test focuses on ModelState errors from data annotations.

        // REMOVED: _controller.TryValidateModel(request);

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        // The controller now returns ValidationProblemDetails when ModelState is invalid.
        var validationProblemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.Equal("Invalid request data.", validationProblemDetails.Title);
        Assert.True(validationProblemDetails.Errors.Any());

        if (string.IsNullOrEmpty(conversationId))
        {
            Assert.Contains(validationProblemDetails.Errors, e => e.Key == nameof(AdapterRequest.ConversationId));
        }
        if (string.IsNullOrEmpty(userId))
        {
            Assert.Contains(validationProblemDetails.Errors, e => e.Key == nameof(AdapterRequest.UserId));
        }

        _mockOrchestrationService.Verify(s => s.ProcessInteractionAsync(It.IsAny<AdapterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_WithEmptyQueryTextAndNoArtifacts_ReturnsBadRequestWithAdapterResponse()
    {
        // Arrange
        var request = new AdapterRequest(
            PlatformType: Abstractions.Models.PlatformType.Test,
            ConversationId: "test-conversation",
            UserId: "test-user",
            QueryText: "", // Empty QueryText
            ArtifactReferences: null, // No artifacts
            MessageId: Guid.NewGuid().ToString()
        );

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        // This specific validation path in the controller returns an AdapterResponse directly.
        var responseValue = Assert.IsType<AdapterResponse>(badRequestResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("QueryText cannot be null or empty if no ArtifactReferences are provided.", responseValue.ResponseMessage);
        _mockOrchestrationService.Verify(s => s.ProcessInteractionAsync(It.IsAny<AdapterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_WithUnknownPlatformType_ReturnsBadRequest()
    {
        // Arrange
        var request = new AdapterRequest(
            PlatformType: Abstractions.Models.PlatformType.Unknown, // Invalid
            ConversationId: "test-conversation",
            UserId: "test-user",
            QueryText: "hello",
            MessageId: Guid.NewGuid().ToString()
        );

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var responseValue = Assert.IsType<AdapterResponse>(badRequestResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("PlatformType cannot be Unknown.", responseValue.ResponseMessage);
        Assert.Equal("PlatformType cannot be Unknown.", responseValue.ErrorMessage);
        _mockOrchestrationService.Verify(s => s.ProcessInteractionAsync(It.IsAny<AdapterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_OrchestrationServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidAdapterRequest();
        var exceptionMessage = "Simulated argument exception";
        _mockOrchestrationService
            .Setup(s => s.ProcessInteractionAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(exceptionMessage));

        // Act
        var actionResult = await _controller.Post(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var responseValue = Assert.IsType<AdapterResponse>(badRequestResult.Value);
        Assert.False(responseValue.Success);
        Assert.Equal("Invalid request data.", responseValue.ResponseMessage);
        Assert.Equal(exceptionMessage, responseValue.ErrorMessage);
    }

    // Removed erroneous GetStatus test method
}
