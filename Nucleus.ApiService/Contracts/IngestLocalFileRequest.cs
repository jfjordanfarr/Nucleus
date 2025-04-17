using System.ComponentModel.DataAnnotations;

namespace Nucleus.ApiService.Contracts;

/// <summary>
/// Request model for initiating ingestion of a local file.
/// </summary>
/// <param name="FilePath">The absolute path to the local file to ingest.</param>
/// <seealso cref="Nucleus.ApiService.Controllers.IngestionController.IngestLocalFileAsync(IngestLocalFileRequest)"/>
/// <seealso href="~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#1-ingestion-endpoint-post-apiingestionlocalfile">Architecture: AI Integration - Ingestion Endpoint</seealso>
public record IngestLocalFileRequest(
    [Required]
    string FilePath
);
