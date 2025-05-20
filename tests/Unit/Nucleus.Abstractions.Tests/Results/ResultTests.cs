// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Results;
using Xunit;

namespace Nucleus.Abstractions.Tests.Results;

public class ResultTests
{
    private enum TestError
    {
        DefaultError,
        SpecificError
    }

    private class TestSuccessData
    {
        public string Message { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Fact]
    public void Success_WhenCalled_SetsPropertiesCorrectly()
    {
        // Arrange
        var successData = new TestSuccessData { Message = "Operation succeeded", Value = 123 };

        // Act
        var result = Result<TestSuccessData, TestError>.Success(successData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(successData, result.SuccessValue);
        Assert.Throws<InvalidOperationException>(() => result.ErrorValue);
    }

    [Fact]
    public void Failure_WhenCalled_SetsPropertiesCorrectly()
    {
        // Arrange
        var error = TestError.SpecificError;

        // Act
        var result = Result<TestSuccessData, TestError>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.ErrorValue);
        Assert.Throws<InvalidOperationException>(() => result.SuccessValue);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_CreatesSuccessResult()
    {
        // Arrange
        var successData = new TestSuccessData { Message = "Implicit success", Value = 456 };

        // Act
        Result<TestSuccessData, TestError> result = successData;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(successData, result.SuccessValue);
        Assert.Throws<InvalidOperationException>(() => result.ErrorValue);
    }

    [Fact]
    public void ImplicitConversion_FromErrorValue_CreatesFailureResult()
    {
        // Arrange
        var error = TestError.DefaultError;

        // Act
        Result<TestSuccessData, TestError> result = error;

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.ErrorValue);
        Assert.Throws<InvalidOperationException>(() => result.SuccessValue);
    }

    [Fact]
    public void SuccessValue_ThrowsInvalidOperationException_WhenResultIsFailure()
    {
        // Arrange
        var result = Result<TestSuccessData, TestError>.Failure(TestError.DefaultError);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.SuccessValue);
    }

    [Fact]
    public void ErrorValue_ThrowsInvalidOperationException_WhenResultIsSuccess()
    {
        // Arrange
        var successData = new TestSuccessData { Message = "Test", Value = 1 };
        var result = Result<TestSuccessData, TestError>.Success(successData);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.ErrorValue);
    }
}
