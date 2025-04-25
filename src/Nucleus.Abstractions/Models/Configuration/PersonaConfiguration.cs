// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Defines the preferred processing mode for interactions handled by a persona.
/// </summary>
public enum ProcessingPreference
{
    /// <summary>
    /// Prefer synchronous processing if possible, but allow fallback to asynchronous.
    /// (Default behavior if not specified).
    /// </summary>
    PreferSync = 0,

    /// <summary>
    /// Prefer asynchronous processing, but allow fallback to synchronous if necessary/trivial.
    /// </summary>
    PreferAsync = 1,

    /// <summary>
    /// Always process interactions asynchronously, regardless of complexity.
    /// </summary>
    ForceAsync = 2
}

/// <summary>
/// Represents configuration settings specific to a Persona.
/// This would typically be loaded from appsettings.json or another configuration source.
/// </summary>
public class PersonaConfiguration
{
    /// <summary>
    /// Unique identifier for the persona this configuration applies to.
    /// Matches the key used for registering the IPersonaManager.
    /// </summary>
    public string PersonaId { get; set; } = string.Empty;

    /// <summary>
    /// Specifies the preferred processing mode (Synchronous/Asynchronous).
    /// Defaults to PreferSync.
    /// </summary>
    public ProcessingPreference ProcessingPreference { get; set; } = ProcessingPreference.PreferSync;

    // Add other persona-specific settings here in the future, e.g.:
    // public Dictionary<string, string> DefaultPromptParameters { get; set; } = new();
    // public List<string> EnabledTools { get; set; } = new();
    // public double Temperature { get; set; } = 0.7;
}
