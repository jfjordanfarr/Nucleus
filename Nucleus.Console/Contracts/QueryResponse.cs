using System.Collections.Generic;

namespace Nucleus.Console.Contracts;

/// <summary>
/// Represents the response payload for the query endpoint. (Mirrors ApiService contract)
/// </summary>
public record QueryResponse(
    string ResponseText,
    List<string> SourceReferences
);
