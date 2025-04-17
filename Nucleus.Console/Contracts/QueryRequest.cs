using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Need to ensure this is referenced if not already

namespace Nucleus.Console.Contracts;

/// <summary>
/// Represents a user's query request, mirroring the API service contract.
/// </summary>
/// <param name="QueryText">The main text of the user's query.</param>
/// <param name="UserId">Optional identifier for the user making the request.</param>
/// <param name="ContextSourceIdentifier">Optional identifier (e.g., file path) for previously ingested content to use as context.</param>
public record QueryRequest(
    [Required]
    string QueryText,
    string? UserId, 
    string? ContextSourceIdentifier
);
