// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the result of checking if a persona should be activated for a given request.
/// </summary>
/// <param name="ShouldActivate">Indicates whether any persona should be activated.</param>
/// <param name="PersonaId">The ID of the persona that should be activated, if any.</param>
/// <param name="Configuration">The configuration of the persona that should be activated, if any.</param>
public record ActivationResult(
    bool ShouldActivate,
    string? PersonaId = null,
    PersonaConfiguration? Configuration = null
);
