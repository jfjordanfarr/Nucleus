// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Nucleus.Abstractions.Models; // For ArtifactReference, ArtifactContent
using Nucleus.Abstractions.Orchestration; // For ContentExtractionResult

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the processed information from a single source artifact, including details about
/// its origin, the raw content fetched, and the result of content extraction.
/// </summary>
/// <param name="OriginalArtifactReferenceId">The unique identifier from the originating <see cref="ArtifactReference.Id"/>.</param>
/// <param name="OriginalReferenceType">The <see cref="ArtifactReference.ReferenceType"/> used to fetch the artifact.</param>
/// <param name="OriginalSourceUri">The <see cref="ArtifactReference.SourceUri"/> of the artifact.</param>
/// <param name="FetchedContentType">The MIME type of the content as determined by the <see cref="IArtifactProvider"/> (from <see cref="ArtifactContent.ContentType"/>).</param>
/// <param name="ExtractionDetails">The complete <see cref="ContentExtractionResult"/> from the <see cref="IContentExtractor"/>.</param>
/// <remarks>
/// This record provides a full trace of an artifact's journey through fetching and extraction.
/// It is stored in <see cref="InteractionContext.ProcessedArtifacts"/> (renaming from ExtractedContents for clarity).
/// See: InteractionContext which holds a list of these.
/// See: [ARCHITECTURE_PROCESSING_INTERFACES.md](../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md)
/// </remarks>
public record ExtractedArtifact(
    string OriginalArtifactReferenceId,
    string OriginalReferenceType,
    Uri? OriginalSourceUri,
    string FetchedContentType,
    ContentExtractionResult ExtractionDetails
);
