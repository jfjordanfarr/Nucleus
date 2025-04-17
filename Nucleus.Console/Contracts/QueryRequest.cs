using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Need to ensure this is referenced if not already

namespace Nucleus.Console.Contracts;

/// <summary>
/// Represents the request payload for the query endpoint. (Mirrors ApiService contract)
/// </summary>
public record QueryRequest(
    [Required]
    string QueryText,
    string? UserId, 
    Dictionary<string, object>? Context
);
