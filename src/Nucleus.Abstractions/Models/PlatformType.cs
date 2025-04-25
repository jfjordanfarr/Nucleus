namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents the source platform of an interaction.
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// Unknown or unspecified platform.
    /// </summary>
    Unknown,

    /// <summary>
    /// Microsoft Teams platform.
    /// Reference: [ARCHITECTURE_ADAPTERS_TEAMS.md](../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md)
    /// </summary>
    Teams,

    /// <summary>
    /// Slack platform.
    /// Reference: [ARCHITECTURE_ADAPTERS_SLACK.md](../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_SLACK.md)
    /// </summary>
    Slack,

    /// <summary>
    /// Local Console application adapter.
    /// Reference: [ARCHITECTURE_ADAPTERS_CONSOLE.md](../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md)
    /// </summary>
    Console,

    /// <summary>
    /// Direct API interaction.
    /// Note: No specific adapter doc, represents direct calls to the Nucleus API.
    /// </summary>
    Api,

    /// <summary>
    /// Email interaction.
    /// Reference: [ARCHITECTURE_ADAPTERS_EMAIL.md](../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_EMAIL.md)
    /// </summary>
    Email,

    /// <summary>
    /// Discord platform.
    /// Reference: [ARCHITECTURE_ADAPTERS_DISCORD.md](../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_DISCORD.md)
    /// </summary>
    Discord

    // Add other platforms as needed
}
