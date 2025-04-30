// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Nucleus.ServiceDefaults.Instrumentation; // Adjusted namespace to match location

/// <summary>
/// Provides a central place to define and access the application's ActivitySource for OpenTelemetry.
/// Resides within ServiceDefaults as it's closely tied to the shared telemetry configuration.
/// See: [Project Census](../../../AgentOps/01_PROJECT_CONTEXT.md#aspire-layer-aspire)
/// </summary>
public static class NucleusActivitySource
{
    /// <summary>
    /// The name of the activity source, used for identifying traces originating from Nucleus.
    /// Follows OpenTelemetry naming conventions.
    /// </summary>
    public static readonly string Name = "Nucleus.Application";

    /// <summary>
    /// The singleton instance of the ActivitySource for the Nucleus application.
    /// </summary>
    public static readonly ActivitySource Source = new ActivitySource(Name);
}