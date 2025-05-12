// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Abstractions.Models.ApiContracts;

/// <summary>
/// Represents the response from an API endpoint that initiates an asynchronous job.
/// </summary>
/// <param name="JobId">The unique identifier for the accepted job.</param>
public record JobIdResponse(string JobId);
