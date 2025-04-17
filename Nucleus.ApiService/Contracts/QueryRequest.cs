using System.ComponentModel.DataAnnotations;

namespace Nucleus.ApiService.Contracts;

/// <summary>
/// Represents the request payload for the query endpoint.
/// </summary>
public record QueryRequest(
    [Required]
    string QueryText,
    string? UserId, // Optional for now, could be inferred from auth later
    Dictionary<string, object>? Context // Optional context
);
