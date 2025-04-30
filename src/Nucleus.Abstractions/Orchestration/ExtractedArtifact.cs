// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the successfully extracted content from a single source artifact.
/// </summary>
/// <param name="SourceId">The unique identifier of the original artifact source (e.g., ArtifactMetadata.Id).</param>
/// <param name="ExtractedText">The textual content extracted from the artifact.</param>
/// <param name="ContentType">The original MIME type of the artifact content.</param>
/// <param name="SourceUri">The original URI of the artifact source, if available.</param>
/// <remarks>
/// This record is created during the artifact processing stage within the OrchestrationService.
/// See: Nucleus.Services.Shared.Extraction.ContentExtractionResult for the raw result from extractors.
/// See: InteractionContext which holds a list of these.
/// See: [ARCHITECTURE_PROCESSING_INTERFACES.md](../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md)
/// </remarks>
public record ExtractedArtifact(string SourceId, string ExtractedText, string? ContentType, Uri? SourceUri);
