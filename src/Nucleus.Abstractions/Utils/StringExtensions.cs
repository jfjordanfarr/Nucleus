// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Abstractions.Utils;

/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Sanitizes a string for logging by removing newline characters and providing a default value for null or whitespace.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <param name="defaultValue">The default value to return if the input is null, empty, or whitespace. Defaults to "N/A".</param>
    /// <returns>The sanitized string, or the default value if the input was null/empty/whitespace.</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? SanitizeLogInput(this string? input, string defaultValue = "N/A")
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        // Replace newline characters with a space to prevent log injection or formatting issues.
        // Using a space preserves readability better than an empty string in some log viewers.
        return input.Replace("\n", " ").Replace("\r", " ");
    }
}
