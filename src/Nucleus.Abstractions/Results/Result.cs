// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Nucleus.Abstractions.Results;

/// <summary>
/// Represents the outcome of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value (typically an enum or error object).</typeparam>
/// <remarks>
/// This is a discriminated union, meaning an instance will either hold a <see cref="SuccessValue"/> or an <see cref="ErrorValue"/>, but not both.
/// The <see cref="IsSuccess"/> property indicates which state the Result is in.
/// </remarks>
/// <seealso cref="OrchestrationError"/>
public sealed class Result<TSuccess, TError>
{
    private readonly TSuccess? _successValue;
    private readonly TError? _errorValue;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value if the operation was successful.
    /// Throws an <see cref="InvalidOperationException"/> if the operation failed.
    /// </summary>
    public TSuccess SuccessValue => IsSuccess ? _successValue! : throw new InvalidOperationException("Result is not in a success state.");

    /// <summary>
    /// Gets the error value if the operation failed.
    /// Throws an <see cref="InvalidOperationException"/> if the operation was successful.
    /// </summary>
    public TError ErrorValue => !IsSuccess ? _errorValue! : throw new InvalidOperationException("Result is not in a failure state.");

    private Result(TSuccess successValue)
    {
        IsSuccess = true;
        _successValue = successValue;
        _errorValue = default;
    }

    private Result(TError errorValue)
    {
        IsSuccess = false;
        _successValue = default;
        _errorValue = errorValue;
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static Result<TSuccess, TError> Success(TSuccess value) => new(value);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">The error value.</param>
    public static Result<TSuccess, TError> Failure(TError error) => new(error);

    /// <summary>
    /// Implicitly converts a success value to a Result.
    /// </summary>
    public static implicit operator Result<TSuccess, TError>(TSuccess successValue) => Success(successValue);

    /// <summary>
    /// Implicitly converts an error value to a Result.
    /// </summary>
    public static implicit operator Result<TSuccess, TError>(TError errorValue) => Failure(errorValue);

    /// <summary>
    /// Executes the appropriate function based on whether the result is a success or a failure.
    /// </summary>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

        return IsSuccess ? onSuccess(SuccessValue) : onFailure(ErrorValue);
    }

    /// <summary>
    /// Executes the appropriate action based on whether the result is a success or a failure.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the result is a success.</param>
    /// <param name="onFailure">The action to execute if the result is a failure.</param>
    public void Switch(Action<TSuccess> onSuccess, Action<TError> onFailure)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

        if (IsSuccess)
        {
            onSuccess(SuccessValue);
        }
        else
        {
            onFailure(ErrorValue);
        }
    }
}
