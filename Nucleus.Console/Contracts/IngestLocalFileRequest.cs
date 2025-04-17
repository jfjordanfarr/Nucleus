using System.ComponentModel.DataAnnotations;

namespace Nucleus.Console.Contracts;

/// <summary>
/// Request model for initiating ingestion of a local file, mirroring the API service contract.
/// </summary>
/// <param name="FilePath">The absolute path to the local file to ingest.</param>
public record IngestLocalFileRequest(
    [Required]
    string FilePath
);
