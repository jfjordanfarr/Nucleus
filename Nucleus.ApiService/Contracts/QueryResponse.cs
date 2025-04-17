using System.Collections.Generic;

namespace Nucleus.ApiService.Contracts;

/// <summary>
/// Represents the response payload for the query endpoint.
/// </summary>
public record QueryResponse(
    string ResponseText,
    List<string> SourceReferences
);
