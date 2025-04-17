using System.ComponentModel.DataAnnotations;

namespace Nucleus.ApiService.Contracts;

/// <summary>
/// Represents a user's query request to the API.
/// </summary>
/// <param name="QueryText">The main text of the user's query.</param>
/// <param name="UserId">Optional identifier for the user making the request.</param>
/// <param name="ContextSourceIdentifier">Optional identifier (e.g., file path) for previously ingested content to use as context.</param>
/// <seealso cref="Nucleus.ApiService.Controllers.QueryController.HandleQuery(QueryRequest)"/>
/// <seealso href="~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#3-contextual-query-post-apiquery">Architecture: AI Integration - Contextual Query</seealso>
public record QueryRequest(
    [Required]
    string QueryText,
    string? UserId = null,
    string? ContextSourceIdentifier = null // Added optional context identifier
);
