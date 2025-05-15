namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents the source platform of an interaction.
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md"/>
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// Unknown or unspecified platform.
    /// </summary>
    Unknown,

    /// <summary>
    /// Microsoft Teams platform.
    /// Reference: [../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md](../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md)
    /// </summary>
    Teams,

    /// <summary>
    /// Slack platform.
    /// Reference: [../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_SLACK.md](../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_SLACK.md)
    /// </summary>
    Slack,

    /// <summary>
    /// Local application adapter.
    /// Reference: [../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md](../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md)
    /// </summary>
    Local,

    /// <summary>
    /// Direct API interaction.
    /// Note: No specific adapter doc, represents direct calls to the Nucleus API.
    /// </summary>
    Api,

    /// <summary>
    /// Email interaction.
    /// Reference: [../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_EMAIL.md](../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_EMAIL.md)
    /// </summary>
    Email,

    /// <summary>
    /// Discord platform.
    /// Reference: [../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_DISCORD.md](../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_DISCORD.md)
    /// </summary>
    Discord,

    /// <summary>
    /// Platform type for testing purposes.
    /// </summary>
    Test

    // Add other platforms as needed
}
