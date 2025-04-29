// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Base class for strategy-specific parameters. Concrete handlers should define classes inheriting from this.
/// This base class allows the AgenticStrategyConfiguration to hold different parameter types polymorphically.
/// </summary>
public abstract class AgenticStrategyParametersBase
{
    // Base class can be empty or contain common properties if needed in the future.
}
