# Code Dump: Services

*Generated on: Aspire-AI-Template-Extended*

## JsonVectorStore.cs

```csharp
﻿using Microsoft.Extensions.VectorData;
using System.Numerics.Tensors;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Aspire_AI_Template_Extended.Services;

/// <summary>
/// This IVectorStore implementation is for prototyping only. Do not use this in production.
/// In production, you must replace this with a real vector store. There are many IVectorStore
/// implementations available, including ones for standalone vector databases like Qdrant or Milvus,
/// or for integrating with relational databases such as SQL Server or PostgreSQL.
/// 
/// This implementation stores the vector records in large JSON files on disk. It is very inefficient
/// and is provided only for convenience when prototyping.
/// </summary>
public class JsonVectorStore(string basePath) : IVectorStore
{
    public IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null) where TKey : notnull
        => new JsonVectorStoreRecordCollection<TKey, TRecord>(name, Path.Combine(basePath, name + ".json"), vectorStoreRecordDefinition);

    public IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken cancellationToken = default)
        => Directory.EnumerateFiles(basePath, "*.json").ToAsyncEnumerable();

    private class JsonVectorStoreRecordCollection<TKey, TRecord> : IVectorStoreRecordCollection<TKey, TRecord>
        where TKey : notnull
    {
        private static readonly Func<TRecord, TKey> _getKey = CreateKeyReader();
        private static readonly Func<TRecord, ReadOnlyMemory<float>> _getVector = CreateVectorReader();

        private readonly string _name;
        private readonly string _filePath;
        private Dictionary<TKey, TRecord>? _records;

        public JsonVectorStoreRecordCollection(string name, string filePath, VectorStoreRecordDefinition? vectorStoreRecordDefinition)
        {
            _name = name;
            _filePath = filePath;

            if (File.Exists(filePath))
            {
                _records = JsonSerializer.Deserialize<Dictionary<TKey, TRecord>>(File.ReadAllText(filePath));
            }
        }

        public string CollectionName => _name;

        public Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_records is not null);

        public async Task CreateCollectionAsync(CancellationToken cancellationToken = default)
        {
            _records = [];
            await WriteToDiskAsync(cancellationToken);
        }

        public async Task CreateCollectionIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_records is null)
            {
                await CreateCollectionAsync(cancellationToken);
            }
        }

        public Task DeleteAsync(TKey key, DeleteRecordOptions? options = null, CancellationToken cancellationToken = default)
        {
            _records!.Remove(key);
            return WriteToDiskAsync(cancellationToken);
        }

        public Task DeleteBatchAsync(IEnumerable<TKey> keys, DeleteRecordOptions? options = null, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _records!.Remove(key);
            }

            return WriteToDiskAsync(cancellationToken);
        }

        public Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
        {
            _records = null;
            File.Delete(_filePath);
            return Task.CompletedTask;
        }

        public Task<TRecord?> GetAsync(TKey key, GetRecordOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_records!.GetValueOrDefault(key));

        public IAsyncEnumerable<TRecord> GetBatchAsync(IEnumerable<TKey> keys, GetRecordOptions? options = null, CancellationToken cancellationToken = default)
            => keys.Select(key => _records!.GetValueOrDefault(key)!).Where(r => r is not null).ToAsyncEnumerable();

        public async Task<TKey> UpsertAsync(TRecord record, UpsertRecordOptions? options = null, CancellationToken cancellationToken = default)
        {
            var key = _getKey(record);
            _records![key] = record;
            await WriteToDiskAsync(cancellationToken);
            return key;
        }

        public async IAsyncEnumerable<TKey> UpsertBatchAsync(IEnumerable<TRecord> records, UpsertRecordOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var results = new List<TKey>();
            foreach (var record in records)
            {
                var key = _getKey(record);
                _records![key] = record;
                results.Add(key);
            }

            await WriteToDiskAsync(cancellationToken);

            foreach (var key in results)
            {
                yield return key;
            }
        }

        public Task<VectorSearchResults<TRecord>> VectorizedSearchAsync<TVector>(TVector vector, VectorSearchOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (vector is not ReadOnlyMemory<float> floatVector)
            {
                throw new NotSupportedException($"The provided vector type {vector!.GetType().FullName} is not supported.");
            }

            IEnumerable<TRecord> filteredRecords = _records!.Values;

            foreach (var clause in options?.Filter?.FilterClauses ?? [])
            {
                if (clause is EqualToFilterClause equalClause)
                {
                    var propertyInfo = typeof(TRecord).GetProperty(equalClause.FieldName);
                    filteredRecords = filteredRecords.Where(record => propertyInfo!.GetValue(record)!.Equals(equalClause.Value));
                }
                else
                {
                    throw new NotSupportedException($"The provided filter clause type {clause.GetType().FullName} is not supported.");
                }
            }

            var ranked = (from record in filteredRecords
                          let candidateVector = _getVector(record)
                          let similarity = TensorPrimitives.CosineSimilarity(candidateVector.Span, floatVector.Span)
                          orderby similarity descending
                          select (Record: record, Similarity: similarity));

            var results = ranked.Skip(options?.Skip ?? 0).Take(options?.Top ?? int.MaxValue);
            return Task.FromResult(new VectorSearchResults<TRecord>(
                results.Select(r => new VectorSearchResult<TRecord>(r.Record, r.Similarity)).ToAsyncEnumerable()));
        }

        private static Func<TRecord, TKey> CreateKeyReader()
        {
            var propertyInfo = typeof(TRecord).GetProperties()
                .Where(p => p.GetCustomAttribute<VectorStoreRecordKeyAttribute>() is not null
                    && p.PropertyType == typeof(TKey))
                .Single();
            return record => (TKey)propertyInfo.GetValue(record)!;
        }

        private static Func<TRecord, ReadOnlyMemory<float>> CreateVectorReader()
        {
            var propertyInfo = typeof(TRecord).GetProperties()
                .Where(p => p.GetCustomAttribute<VectorStoreRecordVectorAttribute>() is not null
                    && p.PropertyType == typeof(ReadOnlyMemory<float>))
                .Single();
            return record => (ReadOnlyMemory<float>)propertyInfo.GetValue(record)!;
        }

        private async Task WriteToDiskAsync(CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(_records);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
    }
}

```

## SemanticSearch.cs

```csharp
﻿using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Aspire_AI_Template_Extended.Services;

public class SemanticSearch(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore)
{
    public async Task<IReadOnlyList<SemanticSearchRecord>> SearchAsync(string text, string? filenameFilter, int maxResults)
    {
        var queryEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(text);
        var vectorCollection = vectorStore.GetCollection<string, SemanticSearchRecord>("data-aspire-ai-template-extended-ingested");
        var filter = filenameFilter is { Length: > 0 }
            ? new VectorSearchFilter().EqualTo(nameof(SemanticSearchRecord.FileName), filenameFilter)
            : null;

        var nearest = await vectorCollection.VectorizedSearchAsync(queryEmbedding, new VectorSearchOptions
        {
            Top = maxResults,
            Filter = filter,
        });
        var results = new List<SemanticSearchRecord>();
        await foreach (var item in nearest.Results)
        {
            results.Add(item.Record);
        }

        return results;
    }
}

```

## SemanticSearchRecord.cs

```csharp
﻿using Microsoft.Extensions.VectorData;

namespace Aspire_AI_Template_Extended.Services;

public class SemanticSearchRecord
{
    [VectorStoreRecordKey]
    public required string Key { get; set; }

    [VectorStoreRecordData]
    public required string FileName { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public required string Text { get; set; }

    [VectorStoreRecordVector(1536, DistanceFunction.CosineSimilarity)] // 1536 is the default vector size for the OpenAI text-embedding-3-small model
    public ReadOnlyMemory<float> Vector { get; set; }
}

```

## Ingestion/DataIngestor.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace Aspire_AI_Template_Extended.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore,
    IngestionCacheDbContext ingestionCacheDb)
{
    public static async Task IngestDataAsync(IServiceProvider services, IIngestionSource source)
    {
        using var scope = services.CreateScope();
        var ingestor = scope.ServiceProvider.GetRequiredService<DataIngestor>();
        await ingestor.IngestDataAsync(source);
    }

    public async Task IngestDataAsync(IIngestionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var vectorCollection = vectorStore.GetCollection<string, SemanticSearchRecord>("data-aspire-ai-template-extended-ingested");
        await vectorCollection.CreateCollectionIfNotExistsAsync();

        var documentsForSource = await ingestionCacheDb.Documents
            .Where(d => d.SourceId == source.SourceId)
            .Include(d => d.Records)
            .ToListAsync(); // Load into memory for easier processing

        var deletedFiles = await source.GetDeletedDocumentsAsync(documentsForSource);
        foreach (var deletedFile in deletedFiles)
        {
            logger.LogInformation("[Source: {SourceId}] Removing ingested data for {DocumentId}", source.SourceId, deletedFile.Id);
            if (deletedFile.Records.Count > 0)
            {
                await vectorCollection.DeleteBatchAsync(deletedFile.Records.Select(r => r.Id));
            }
            ingestionCacheDb.Documents.Remove(deletedFile);
        }
        // Save changes after deletions before processing modifications
        if (deletedFiles.Any()) 
        {
            await ingestionCacheDb.SaveChangesAsync();
        }

        // Get potential new/modified document IDs from the source
        var sourceDocumentInfos = await source.GetDocumentIdentifiersAsync();

        foreach (var docInfo in sourceDocumentInfos)
        {
            var existingCacheEntry = documentsForSource.FirstOrDefault(d => d.Id == docInfo.Id);
            string currentHash = await CalculateSha256HashAsync(docInfo.FullPath); // Assuming docInfo contains the full path
            DateTime currentLastModifiedUtc = File.GetLastWriteTimeUtc(docInfo.FullPath);

            if (existingCacheEntry != null)
            {
                // Document exists in cache, check if content or modification time changed
                if (existingCacheEntry.ContentHash == currentHash && existingCacheEntry.SourceLastModifiedUtc >= currentLastModifiedUtc)
                {
                    logger.LogDebug("[Source: {SourceId}] Skipping unchanged document {DocumentId}", source.SourceId, docInfo.Id);
                    continue; // No changes detected, skip to the next document
                }

                // Changes detected, update existing entry
                logger.LogInformation("[Source: {SourceId}] Updating document {DocumentId}. Hash changed: {HashChanged}, Time changed: {TimeChanged}", 
                    source.SourceId, docInfo.Id, existingCacheEntry.ContentHash != currentHash, existingCacheEntry.SourceLastModifiedUtc < currentLastModifiedUtc);

                // Remove old vector records
                if (existingCacheEntry.Records.Count > 0)
                {
                    await vectorCollection.DeleteBatchAsync(existingCacheEntry.Records.Select(r => r.Id));
                    existingCacheEntry.Records.Clear(); // Clear EF Core tracked list
                    ingestionCacheDb.RemoveRange(ingestionCacheDb.Records.Where(r => r.DocumentId == existingCacheEntry.Id)); // Ensure DB records are removed
                }

                // Create and upsert new vector records
                var updatedRecords = await source.CreateRecordsForDocumentAsync(embeddingGenerator, docInfo.Id);
                await foreach (var id in vectorCollection.UpsertBatchAsync(updatedRecords)) { }

                // Update cache entry properties
                existingCacheEntry.ContentHash = currentHash;
                existingCacheEntry.SourceLastModifiedUtc = currentLastModifiedUtc;
                existingCacheEntry.IngestedTimeUtc = DateTime.UtcNow;
                // Ensure DocumentId is set during creation
                existingCacheEntry.Records.AddRange(updatedRecords.Select(r => new IngestedRecord { Id = r.Key, DocumentId = existingCacheEntry.Id }));
                ingestionCacheDb.Documents.Update(existingCacheEntry);
            }
            else
            {
                // New document, ingest it
                logger.LogInformation("[Source: {SourceId}] Ingesting new document {DocumentId}", source.SourceId, docInfo.Id);

                // Create and upsert new vector records
                var newRecords = await source.CreateRecordsForDocumentAsync(embeddingGenerator, docInfo.Id);
                await foreach (var id in vectorCollection.UpsertBatchAsync(newRecords)) { }

                // Create new cache entry
                var newCacheEntry = new IngestedDocument
                {
                    Id = docInfo.Id,
                    SourceId = source.SourceId,
                    SourceLastModifiedUtc = currentLastModifiedUtc,
                    ContentHash = currentHash,
                    IngestedTimeUtc = DateTime.UtcNow,
                    // Ensure DocumentId is set during creation
                    Records = newRecords.Select(r => new IngestedRecord { Id = r.Key, DocumentId = docInfo.Id }).ToList()
                };
                ingestionCacheDb.Documents.Add(newCacheEntry);
            }
        }

        await ingestionCacheDb.SaveChangesAsync();
        logger.LogInformation("[Source: {SourceId}] Ingestion finished.", source.SourceId);
    }

    private static async Task<string> CalculateSha256HashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        byte[] hashBytes = await sha256.ComputeHashAsync(stream);
        // Convert byte array to a hexadecimal string
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}

```

## Ingestion/IIngestionSource.cs

```csharp
using Microsoft.Extensions.AI;

namespace Aspire_AI_Template_Extended.Services.Ingestion;

// Represents basic information about a document in the source.
public record DocumentInfo(string Id, string FullPath, DateTime LastModifiedUtc);

public interface IIngestionSource
{
    string SourceId { get; }

    // Gets identifiers and basic metadata for all current documents in the source.
    Task<IEnumerable<DocumentInfo>> GetDocumentIdentifiersAsync();

    // Determines which cached documents no longer exist in the source.
    Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IEnumerable<IngestedDocument> cachedDocuments);

    // Creates vector store records for a specific document.
    Task<IEnumerable<SemanticSearchRecord>> CreateRecordsForDocumentAsync(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, string documentId);
}

```

## Ingestion/IngestionCacheDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace Aspire_AI_Template_Extended.Services.Ingestion;

// A DbContext that keeps track of which documents have been ingested.
// This makes it possible to avoid re-ingesting documents that have not changed,
// and to delete documents that have been removed from the underlying source.
public class IngestionCacheDbContext : DbContext
{
    public IngestionCacheDbContext(DbContextOptions<IngestionCacheDbContext> options) : base(options)
    {
    }

    public DbSet<IngestedDocument> Documents { get; set; } = default!;
#pragma warning disable IDE0044 // EF Core needs the setter for change tracking and relationship fixup.
    public DbSet<IngestedRecord> Records { get; set; } = default!;
#pragma warning restore IDE0044

    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<IngestionCacheDbContext>();
        db.Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder); // Add null check
        base.OnModelCreating(modelBuilder); // Ensure base method is called

        modelBuilder.Entity<IngestedDocument>()
            .HasKey(d => d.Id);
        modelBuilder.Entity<IngestedDocument>().HasMany(d => d.Records).WithOne().HasForeignKey(r => r.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class IngestedDocument
{
    // TODO: Make Id+SourceId a composite key
    public required string Id { get; set; }
    public required string SourceId { get; set; }
    public required DateTime SourceLastModifiedUtc { get; set; }
    public required string ContentHash { get; set; }
    public required DateTime IngestedTimeUtc { get; set; }
#pragma warning disable CA2227 // EF Core needs the setter for change tracking and relationship fixup.
    public List<IngestedRecord> Records { get; set; } = [];
#pragma warning restore CA2227
}

public class IngestedRecord
{
    public required string Id { get; set; }
    public required string DocumentId { get; init; }
}

```

## Ingestion/PDFDirectorySource.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig;
using Microsoft.Extensions.AI;
using UglyToad.PdfPig.Content;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aspire_AI_Template_Extended.Services.Ingestion;

public class PDFDirectorySource(string sourceDirectory) : IIngestionSource
{
    public static string SourceFileId(string path) => Path.GetFileName(path);

    public string SourceId => $"{nameof(PDFDirectorySource)}:{sourceDirectory}";

    public Task<IEnumerable<DocumentInfo>> GetDocumentIdentifiersAsync()
    {
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pdf");
        var results = sourceFiles.Select(fullPath => 
            new DocumentInfo(
                Id: SourceFileId(fullPath),
                FullPath: fullPath,
                LastModifiedUtc: File.GetLastWriteTimeUtc(fullPath)
            ));
        
        return Task.FromResult(results);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IEnumerable<IngestedDocument> cachedDocuments)
    {
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pdf");
        var sourceFileIds = sourceFiles.Select(SourceFileId).ToHashSet(); // Use HashSet for efficient lookups
        
        var deletedDocs = cachedDocuments.Where(d => !sourceFileIds.Contains(d.Id));
        
        return Task.FromResult(deletedDocs);
    }

    public async Task<IEnumerable<SemanticSearchRecord>> CreateRecordsForDocumentAsync(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, string documentId)
    {
        ArgumentNullException.ThrowIfNull(embeddingGenerator);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        var fullPath = Path.Combine(sourceDirectory, documentId);
        if (!File.Exists(fullPath))
        {
            return Enumerable.Empty<SemanticSearchRecord>(); 
        }

        using var pdf = PdfDocument.Open(fullPath);
        var paragraphs = pdf.GetPages().SelectMany(GetPageParagraphs).ToList();
        
        if (!paragraphs.Any())
        {
            return Enumerable.Empty<SemanticSearchRecord>();
        }

        var embeddings = await embeddingGenerator.GenerateAsync(paragraphs.Select(c => c.Text));

        return paragraphs.Zip(embeddings).Select((pair, index) => new SemanticSearchRecord
        {
            Key = $"{Path.GetFileNameWithoutExtension(documentId)}_{pair.First.PageNumber}_{pair.First.IndexOnPage}",
            FileName = documentId,
            PageNumber = pair.First.PageNumber,
            Text = pair.First.Text,
            Vector = pair.Second.Vector,
        });
    }

    private static IEnumerable<(int PageNumber, int IndexOnPage, string Text)> GetPageParagraphs(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        if (!letters.Any()) return Enumerable.Empty<(int, int, string)>(); 

        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        if (!words.Any()) return Enumerable.Empty<(int, int, string)>(); 

        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        if (!textBlocks.Any()) return Enumerable.Empty<(int, int, string)>(); 

        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select((text, index) => (pdfPage.Number, index, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
    }
}

```

# Code Dump: Components

*Generated on: Aspire-AI-Template-Extended*

## App.razor

```
﻿<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <link rel="stylesheet" href="@Assets["Aspire-AI-Template-Extended.styles.css"]" />
    <ImportMap />
    <HeadOutlet @rendermode="@renderMode" />
</head>

<body>
    <Routes @rendermode="@renderMode" />
    <script src="@Assets["app.js"]" type="module"></script>
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>

</html>

@code {
    private readonly IComponentRenderMode renderMode = new InteractiveServerRenderMode(prerender: false);
}

```

## Routes.razor

```
﻿<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>

```

## _Imports.razor

```
﻿@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.Extensions.AI
@using Microsoft.JSInterop
@using Aspire_AI_Template_Extended
@using Aspire_AI_Template_Extended.Components
@using Aspire_AI_Template_Extended.Components.Layout
@using Aspire_AI_Template_Extended.Services

```

## Layout/LoadingSpinner.razor

```
<div class="lds-ellipsis"><div></div><div></div><div></div><div></div></div>

```

## Layout/LoadingSpinner.razor.css

```css
﻿/* Used under CC0 license */

.lds-ellipsis {
    color: #666;
    animation: fade-in 1s;
}

@keyframes fade-in {
    0% {
        opacity: 0;
    }

    100% {
        opacity: 1;
    }
}

    .lds-ellipsis,
    .lds-ellipsis div {
        box-sizing: border-box;
    }

.lds-ellipsis {
    margin: auto;
    display: block;
    position: relative;
    width: 80px;
    height: 80px;
}

    .lds-ellipsis div {
        position: absolute;
        top: 33.33333px;
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: currentColor;
        animation-timing-function: cubic-bezier(0, 1, 1, 0);
    }

        .lds-ellipsis div:nth-child(1) {
            left: 8px;
            animation: lds-ellipsis1 0.6s infinite;
        }

        .lds-ellipsis div:nth-child(2) {
            left: 8px;
            animation: lds-ellipsis2 0.6s infinite;
        }

        .lds-ellipsis div:nth-child(3) {
            left: 32px;
            animation: lds-ellipsis2 0.6s infinite;
        }

        .lds-ellipsis div:nth-child(4) {
            left: 56px;
            animation: lds-ellipsis3 0.6s infinite;
        }

@keyframes lds-ellipsis1 {
    0% {
        transform: scale(0);
    }

    100% {
        transform: scale(1);
    }
}

@keyframes lds-ellipsis3 {
    0% {
        transform: scale(1);
    }

    100% {
        transform: scale(0);
    }
}

@keyframes lds-ellipsis2 {
    0% {
        transform: translate(0, 0);
    }

    100% {
        transform: translate(24px, 0);
    }
}

```

## Layout/MainLayout.razor

```
﻿@inherits LayoutComponentBase

@Body

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>

```

## Layout/MainLayout.razor.css

```css
#blazor-error-ui {
    color-scheme: light only;
    background: lightyellow;
    bottom: 0;
    box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
    box-sizing: border-box;
    display: none;
    left: 0;
    padding: 0.6rem 1.25rem 0.7rem 1.25rem;
    position: fixed;
    width: 100%;
    z-index: 1000;
}

    #blazor-error-ui .dismiss {
        cursor: pointer;
        position: absolute;
        right: 0.75rem;
        top: 0.5rem;
    }

```

## Layout/SurveyPrompt.razor

```
<div class="surveyContainer">
    <div class="tool-icon" aria-hidden="true">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125" />
        </svg>
    </div>

    <div>
        How well is this template working for you? Please take a
        <a target="_blank" href="https://aka.ms/dotnet-chat-template-survey">brief survey</a>
        and tell us what you think.
    </div>
</div>

```

## Layout/SurveyPrompt.razor.css

```css
﻿.surveyContainer {
    display: flex;
    justify-content: center;
    gap: 0.5rem;
    font-size: 0.9em;
    margin: 0.5rem auto -0.7rem auto;
    max-width: 1024px;
    color: #444;
}

    .surveyContainer a {
        text-decoration: underline;
    }

    .surveyContainer .tool-icon {
        margin-top: 0.15rem;
        width: 1.25rem;
        height: 1.25rem;
        flex-shrink: 0;
    }

```

## Pages/Error.razor

```
﻿@page "/Error"
@using System.Diagnostics

<PageTitle>Error</PageTitle>

<h1 class="text-danger">Error.</h1>
<h2 class="text-danger">An error occurred while processing your request.</h2>

@if (ShowRequestId)
{
    <p>
        <strong>Request ID:</strong> <code>@RequestId</code>
    </p>
}

<h3>Development Mode</h3>
<p>
    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.
</p>
<p>
    <strong>The Development environment shouldn't be enabled for deployed applications.</strong>
    It can result in displaying sensitive information from exceptions to end users.
    For local debugging, enable the <strong>Development</strong> environment by setting the <strong>ASPNETCORE_ENVIRONMENT</strong> environment variable to <strong>Development</strong>
    and restarting the app.
</p>

@code{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}

```

## Pages/Chat/Chat.razor

```
﻿@page "/"
@using System.ComponentModel
@inject IChatClient ChatClient
@inject NavigationManager Nav
@inject SemanticSearch Search
@implements IDisposable

<PageTitle>Chat</PageTitle>

<ChatHeader OnNewChat="@ResetConversationAsync" />

<ChatMessageList Messages="@messages" InProgressMessage="@currentResponseMessage">
    <NoMessagesContent>
        <div>To get started, try asking about these example documents. You can replace these with your own data and replace this message.</div>
        <ChatCitation File="Example_Emergency_Survival_Kit.pdf"/>
        <ChatCitation File="Example_GPS_Watch.pdf"/>
    </NoMessagesContent>
</ChatMessageList>

<div class="chat-container">
    <ChatSuggestions OnSelected="@AddUserMessageAsync" @ref="@chatSuggestions" />
    <ChatInput OnSend="@AddUserMessageAsync" @ref="@chatInput" />
    <SurveyPrompt /> @* Remove this line to eliminate the template survey message *@
</div>

@code {
    private const string SystemPrompt = @"
        You are an assistant who answers questions about information you retrieve.
        Do not answer questions about anything else.
        Use only simple markdown to format your responses.

        Use the search tool to find relevant information. When you do this, end your
        reply with citations in the special XML format:

        <citation filename='string' page_number='number'>exact quote here</citation>

        Always include the citation in your response if there are results.

        The quote must be max 5 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
        Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text.
        ";

    private readonly ChatOptions chatOptions = new();
    private readonly List<ChatMessage> messages = new();
    private CancellationTokenSource? currentResponseCancellation;
    private ChatMessage? currentResponseMessage;
    private ChatInput? chatInput;
    private ChatSuggestions? chatSuggestions;

    protected override void OnInitialized()
    {
        messages.Add(new(ChatRole.System, SystemPrompt));
        chatOptions.Tools = [AIFunctionFactory.Create(SearchAsync)];
    }

    private async Task AddUserMessageAsync(ChatMessage userMessage)
    {
        CancelAnyCurrentResponse();

        // Add the user message to the conversation
        messages.Add(userMessage);
        chatSuggestions?.Clear();
        await chatInput!.FocusAsync();

        // Stream and display a new response from the IChatClient
        var responseText = new TextContent("");
        currentResponseMessage = new ChatMessage(ChatRole.Assistant, [responseText]);
        currentResponseCancellation = new();

        // Pass a copy of the messages then add messages to the list while handling the streaming responses
        var requestMessages = messages.ToArray();

        await foreach (var update in ChatClient.GetStreamingResponseAsync(requestMessages, chatOptions, currentResponseCancellation.Token))
        {
            AddMessages(messages, update, filter: c => c is not TextContent);
            responseText.Text += update.Text;
            ChatMessageItem.NotifyChanged(currentResponseMessage);
        }

        // Store the final response in the conversation, and begin getting suggestions
        messages.Add(currentResponseMessage!);
        currentResponseMessage = null;
        chatSuggestions?.Update(messages);
    }

    private void CancelAnyCurrentResponse()
    {
        // If a response was cancelled while streaming, include it in the conversation so it's not lost
        if (currentResponseMessage is not null)
        {
            messages.Add(currentResponseMessage);
        }

        currentResponseCancellation?.Cancel();
        currentResponseMessage = null;
    }

    private async Task ResetConversationAsync()
    {
        CancelAnyCurrentResponse();
        messages.Clear();
        messages.Add(new(ChatRole.System, SystemPrompt));
        chatSuggestions?.Clear();
        await chatInput!.FocusAsync();
    }

    // TODO: Needed until https://github.com/dotnet/extensions/issues/6114 is resolved, which will introduce
    // an extension method on IList<ChatMessage> for adding messages from a ChatResponseUpdate.
    private static void AddMessages(IList<ChatMessage> list, ChatResponseUpdate update, Func<AIContent, bool> filter)
    {
        var contentsList = update.Contents.Where(filter).ToList();
        if (contentsList.Count > 0)
        {
            list.Add(new(update.Role ?? ChatRole.Assistant, contentsList)
            {
                AuthorName = update.AuthorName,
                RawRepresentation = update.RawRepresentation,
                AdditionalProperties = update.AdditionalProperties,
            });
        }
    }

    [Description("Searches for information using a phrase or keyword")]
    private async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("Whenever possible, specify the filename to search that file only. If not provided, the search includes all files.")] string? filenameFilter = null)
    {
        await InvokeAsync(StateHasChanged);
        var results = await Search.SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result =>
            $"<result filename=\"{result.FileName}\" page_number=\"{result.PageNumber}\">{result.Text}</result>");
    }

    public void Dispose()
        => currentResponseCancellation?.Cancel();
}

```

## Pages/Chat/Chat.razor.css

```css
.chat-container {
    position: sticky; 
    bottom: 0; 
    padding-left: 1.5rem;
    padding-right: 1.5rem; 
    padding-top: 0.75rem; 
    padding-bottom: 1.5rem; 
    border-top-width: 1px; 
    background-color: #F3F4F6; 
    border-color: #E5E7EB;
}

```

## Pages/Chat/ChatCitation.razor

```
@using System.Web
@if (!string.IsNullOrWhiteSpace(viewerUrl))
{
    <a href="@viewerUrl" target="_blank" class="citation">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
        </svg>
        <div class="citation-content">
            <div class="citation-file">@File</div>
            <div>@Quote</div>
        </div>
    </a>
}

@code {
    [Parameter]
    public required string File { get; set; }

    [Parameter]
    public int? PageNumber { get; set; }

    [Parameter]
    public required string Quote { get; set; }

    private string? viewerUrl;

    protected override void OnParametersSet()
    {
        viewerUrl = null;

        // If you ingest other types of content besides PDF files, construct a URL to an appropriate viewer here
        if (File.EndsWith(".pdf"))
        {
            var search = Quote?.Trim('.', ',', ' ', '\n', '\r', '\t', '"', '\'');
            viewerUrl = $"lib/pdf_viewer/viewer.html?file=/Data/{HttpUtility.UrlEncode(File)}#page={PageNumber}&search={HttpUtility.UrlEncode(search)}&phrase=true";
        }
    }
}

```

## Pages/Chat/ChatCitation.razor.css

```css
﻿.citation {
    display: inline-flex;
    padding-top: 0.5rem;
    padding-bottom: 0.5rem;
    padding-left: 0.75rem;
    padding-right: 0.75rem;
    margin-top: 1rem;
    margin-right: 1rem;
    border-bottom: 2px solid #a770de;
    gap: 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.875rem;
    line-height: 1.25rem;
    background-color: #ffffff;
}

    .citation[href]:hover {
        outline: 1px solid #865cb1;
    }

    .citation svg {
        width: 1.5rem;
        height: 1.5rem;
    }

    .citation:active {
        background-color: rgba(0,0,0,0.05);
    }

.citation-content {
    display: flex;
    flex-direction: column;
}

.citation-file {
    font-weight: 600;
}

```

## Pages/Chat/ChatHeader.razor

```
<div class="chat-header-container main-background-gradient">
    <div class="chat-header-controls page-width">
        <button class="btn-default" @onclick="@OnNewChat">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="new-chat-icon">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            New chat
        </button>
    </div>

    <h1 class="page-width">Aspire-AI-Template-Extended</h1>
</div>

@code {
    [Parameter]
    public EventCallback OnNewChat { get; set; }
}

```

## Pages/Chat/ChatHeader.razor.css

```css
.chat-header-container {
    top: 0; 
    padding: 1.5rem; 
}

.chat-header-controls {
    margin-bottom: 1.5rem; 
}

h1 {
    overflow: hidden;
    text-overflow: ellipsis;
}

.new-chat-icon {
    width: 1.25rem;
    height: 1.25rem;
    color: rgb(55, 65, 81);
}

@media (min-width: 768px) {
    .chat-header-container {
        position: sticky;
    }
}

```

## Pages/Chat/ChatInput.razor

```
@inject IJSRuntime JS

<EditForm Model="@this" OnValidSubmit="@SendMessageAsync">
    <label class="input-box page-width">
        <textarea @ref="@textArea" @bind="@messageText" placeholder="Type your message..." rows="1"></textarea>

        <div class="tools">
            <button type="submit" title="Send" class="send-button">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="tool-icon">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5" />
                </svg>
            </button>
        </div>
    </label>
</EditForm>

@code {
    private ElementReference textArea;
    private string? messageText;

    [Parameter]
    public EventCallback<ChatMessage> OnSend { get; set; }

    public ValueTask FocusAsync()
        => textArea.FocusAsync();

    private async Task SendMessageAsync()
    {
        if (messageText is { Length: > 0 } text)
        {
            messageText = null;
            await OnSend.InvokeAsync(new ChatMessage(ChatRole.User, text));
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                var module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Chat/ChatInput.razor.js");
                await module.InvokeVoidAsync("init", textArea);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}

```

## Pages/Chat/ChatInput.razor.css

```css
﻿.input-box {
    display: flex; 
    flex-direction: column; 
    background: white;
    border: 1px solid rgb(229, 231, 235);
    border-radius: 8px;
    padding: 0.5rem 0.75rem;
    margin-top: 0.75rem; 
}

    .input-box:focus-within {
        outline: 2px solid #4152d5;
    }

textarea {
    resize: none;
    border: none;
    outline: none;
    flex-grow: 1;
}

    textarea:placeholder-shown + .tools {
        --send-button-color: #aaa;
    }

.tools {
    display: flex; 
    margin-top: 1rem; 
    align-items: center;
}

.tool-icon {
    width: 1.25rem;
    height: 1.25rem;
}

.send-button {
    color: var(--send-button-color);
    margin-left: auto;
}

    .send-button:hover {
        color: black;
    }

.attach {
    background-color: white;
    border-style: dashed;
    color: #888;
    border-color: #888;
    padding: 3px 8px;
}

    .attach:hover {
        background-color: #f0f0f0;
        color: black;
    }

```

## Pages/Chat/ChatInput.razor.js

```javascript
﻿export function init(elem) {
    elem.focus();

    // Auto-resize whenever the user types or if the value is set programmatically
    elem.addEventListener('input', () => resizeToFit(elem));
    afterPropertyWritten(elem, 'value', () => resizeToFit(elem));

    // Auto-submit the form on 'enter' keypress
    elem.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            elem.dispatchEvent(new CustomEvent('change', { bubbles: true }));
            elem.closest('form').dispatchEvent(new CustomEvent('submit', { bubbles: true }));
        }
    });
}

function resizeToFit(elem) {
    const lineHeight = parseFloat(getComputedStyle(elem).lineHeight);

    elem.rows = 1;
    const numLines = Math.ceil(elem.scrollHeight / lineHeight);
    elem.rows = Math.min(5, Math.max(1, numLines));
}

function afterPropertyWritten(target, propName, callback) {
    const descriptor = getPropertyDescriptor(target, propName);
    Object.defineProperty(target, propName, {
        get: function () {
            return descriptor.get.apply(this, arguments);
        },
        set: function () {
            const result = descriptor.set.apply(this, arguments);
            callback();
            return result;
        }
    });
}

function getPropertyDescriptor(target, propertyName) {
    return Object.getOwnPropertyDescriptor(target, propertyName)
        || getPropertyDescriptor(Object.getPrototypeOf(target), propertyName);
}

```

## Pages/Chat/ChatMessageItem.razor

```
@using System.Runtime.CompilerServices
@using System.Text.RegularExpressions
@using System.Linq

@if (Message.Role == ChatRole.User)
{
    <div class="user-message">
        @Message.Text
    </div>
}
else if (Message.Role == ChatRole.Assistant)
{
    foreach (var content in Message.Contents)
    {
        if (content is TextContent { Text: { Length: > 0 } text })
        {
            <div class="assistant-message">
                <div>
                    <div class="assistant-message-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 18v-5.25m0 0a6.01 6.01 0 0 0 1.5-.189m-1.5.189a6.01 6.01 0 0 1-1.5-.189m3.75 7.478a12.06 12.06 0 0 1-4.5 0m3.75 2.383a14.406 14.406 0 0 1-3 0M14.25 18v-.192c0-.983.658-1.823 1.508-2.316a7.5 7.5 0 1 0-7.517 0c.85.493 1.509 1.333 1.509 2.316V18" />
                        </svg>
                    </div>
                </div>
                <div class="assistant-message-header">Assistant</div>
                <div class="assistant-message-text">
                    <assistant-message markdown="@text"></assistant-message>

                    @foreach (var citation in citations ?? [])
                    {
                        <ChatCitation File="@citation.File" PageNumber="@citation.Page" Quote="@citation.Quote" />
                    }
                </div>
            </div>
        }
        else if (content is FunctionCallContent { Name: "Search" } fcc && fcc.Arguments?.TryGetValue("searchPhrase", out var searchPhrase) is true)
        {
            <div class="assistant-search">
                <div class="assistant-search-icon">
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
                    </svg>
                </div>
                <div class="assistant-search-content">
                    Searching:
                    <span class="assistant-search-phrase">@searchPhrase</span>
                    @if (fcc.Arguments?.TryGetValue("filenameFilter", out var filenameObj) is true && filenameObj is string filename && !string.IsNullOrEmpty(filename))
                    {
                        <text> in </text><span class="assistant-search-phrase">@filename</span>
                    }
                </div>
            </div>
        }
    }
}

@code {
    private static readonly ConditionalWeakTable<ChatMessage, ChatMessageItem> SubscribersLookup = new();
    private static readonly Regex CitationRegex = new(@"<citation filename='(?<file>[^']*)' page_number='(?<page>\d*)'>(?<quote>.*?)</citation>", RegexOptions.NonBacktracking);

    private List<(string File, int? Page, string Quote)>? citations;

    [Parameter, EditorRequired]
    public required ChatMessage Message { get; set; }

    [Parameter]
    public bool InProgress { get; set;}

    protected override void OnInitialized()
    {
        SubscribersLookup.AddOrUpdate(Message, this);

        if (!InProgress && Message.Role == ChatRole.Assistant && Message.Text is { Length: > 0 } text)
        {
            ParseCitations(text);
        }
    }

    public static void NotifyChanged(ChatMessage source)
    {
        if (SubscribersLookup.TryGetValue(source, out var subscriber))
        {
            subscriber.StateHasChanged();
        }
    }

    private void ParseCitations(string text)
    {
        var matches = CitationRegex.Matches(text);
        citations = matches.Any()
            ? matches.Select(m => (m.Groups["file"].Value, int.TryParse(m.Groups["page"].Value, out var page) ? page : (int?)null, m.Groups["quote"].Value)).ToList()
            : null;
    }
}

```

## Pages/Chat/ChatMessageItem.razor.css

```css
﻿.user-message {
    background: rgb(182 215 232);
    align-self: flex-end;
    min-width: 25%;
    max-width: calc(100% - 5rem);
    padding: 0.5rem 1.25rem;
    border-radius: 0.25rem; 
    color: #1F2937; 
    white-space: pre-wrap; 
}

.assistant-message, .assistant-search {
    display: grid;
    grid-template-rows: min-content;
    grid-template-columns: 2rem minmax(0, 1fr);
    gap: 0.25rem; 
}

.assistant-message-header {
    font-weight: 600;
}

.assistant-message-text {
    grid-column-start: 2;
}

.assistant-message-icon {
    display: flex; 
    justify-content: center; 
    align-items: center; 
    border-radius: 9999px; 
    width: 1.5rem; 
    height: 1.5rem; 
    color: #ffffff; 
    background: #9b72ce;
}

    .assistant-message-icon svg {
        width: 1rem; 
        height: 1rem; 
    }

.assistant-search {
    font-size: 0.875rem;
    line-height: 1.25rem; 
}

.assistant-search-icon {
    display: flex; 
    justify-content: center; 
    align-items: center; 
    width: 1.5rem; 
    height: 1.5rem; 
}

    .assistant-search-icon svg {
        width: 1rem; 
        height: 1rem; 
    }

.assistant-search-content {
    align-content: center;
}

.assistant-search-phrase {
    font-weight: 600;
}

/* Default styling for markdown-formatted assistant messages */
::deep ul {
    list-style-type: disc;
    margin-left: 1.5rem;
}

::deep ol {
    list-style-type: decimal;
    margin-left: 1.5rem;
}

::deep li {
    margin: 0.5rem 0;
}

::deep strong {
    font-weight: 600;
}

::deep h3 {
    margin: 1rem 0;
    font-weight: 600;
}

::deep p + p {
    margin-top: 1rem;
}

::deep table {
    margin: 1rem 0;
}

::deep th {
    text-align: left;
    border-bottom: 1px solid silver;
}

::deep th, ::deep td {
    padding: 0.1rem 0.5rem;
}

::deep th, ::deep tr:nth-child(even) {
    background-color: rgba(0, 0, 0, 0.05);
}

```

## Pages/Chat/ChatMessageList.razor

```
@inject IJSRuntime JS

<div class="message-list-container">
    <chat-messages class="page-width message-list" in-progress="@(InProgressMessage is not null)">
        @foreach (var message in Messages)
        {
            <ChatMessageItem @key="@message" Message="@message" />
        }

        @if (InProgressMessage is not null)
        {
            <ChatMessageItem Message="@InProgressMessage" InProgress="true" />
            <LoadingSpinner />
        }
        else if (IsEmpty)
        {
            <div class="no-messages">@NoMessagesContent</div>
        }
    </chat-messages>
</div>

@code {
    [Parameter]
    public required IEnumerable<ChatMessage> Messages { get; set; }

    [Parameter]
    public ChatMessage? InProgressMessage { get; set; }

    [Parameter]
    public RenderFragment? NoMessagesContent { get; set; }

    private bool IsEmpty => !Messages.Any(m => (m.Role == ChatRole.User || m.Role == ChatRole.Assistant) && !string.IsNullOrEmpty(m.Text));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Activates the auto-scrolling behavior
            await JS.InvokeVoidAsync("import", "./Components/Pages/Chat/ChatMessageList.razor.js");
        }
    }
}

```

## Pages/Chat/ChatMessageList.razor.css

```css
﻿.message-list-container {
    margin: 2rem 1.5rem;
    flex-grow: 1;
}

.message-list {
    display: flex; 
    flex-direction: column; 
    gap: 1.25rem; 
}

.no-messages {
    text-align: center;
    font-size: 1.25rem;
    color: #999;
    margin-top: calc(40vh - 18rem);
}

chat-messages > ::deep div:last-of-type {
    /* Adds some vertical buffer to so that suggestions don't overlap the output when they appear */
    margin-bottom: 2rem;
}

```

## Pages/Chat/ChatMessageList.razor.js

```javascript
﻿// The following logic provides auto-scroll behavior for the chat messages list.
// If you don't want that behavior, you can simply not load this module.

window.customElements.define('chat-messages', class ChatMessages extends HTMLElement {
    static _isFirstAutoScroll = true;

    connectedCallback() {
        this._observer = new MutationObserver(mutations => this._scheduleAutoScroll(mutations));
        this._observer.observe(this, { childList: true, attributes: true });
    }

    disconnectedCallback() {
        this._observer.disconnect();
    }

    _scheduleAutoScroll(mutations) {
        // Debounce the calls in case multiple DOM updates occur together
        cancelAnimationFrame(this._nextAutoScroll);
        this._nextAutoScroll = requestAnimationFrame(() => {
            const addedUserMessage = mutations.some(m => Array.from(m.addedNodes).some(n => n.parentElement === this && n.classList?.contains('user-message')));
            const elem = this.lastElementChild;
            if (ChatMessages._isFirstAutoScroll || addedUserMessage || this._elemIsNearScrollBoundary(elem, 300)) {
                elem.scrollIntoView({ behavior: ChatMessages._isFirstAutoScroll ? 'instant' : 'smooth' });
                ChatMessages._isFirstAutoScroll = false;
            }
        });
    }

    _elemIsNearScrollBoundary(elem, threshold) {
        const maxScrollPos = document.body.scrollHeight - window.innerHeight;
        const remainingScrollDistance = maxScrollPos - window.scrollY;
        return remainingScrollDistance < elem.offsetHeight + threshold;
    }
});


```

## Pages/Chat/ChatSuggestions.razor

```
@inject IChatClient ChatClient

@if (suggestions is not null)
{
    <div class="page-width suggestions">
        @foreach (var suggestion in suggestions)
        {
            <button class="btn-subtle" @onclick="@(() => AddSuggestionAsync(suggestion))">
                @suggestion
            </button>
        }
    </div>
}

@code {
    private static string Prompt = @"
        Suggest up to 3 follow-up questions that I could ask you to help me complete my task.
        Each suggestion must be a complete sentence, maximum 6 words.
        Each suggestion must be phrased as something that I (the user) would ask you (the assistant) in response to your previous message,
        for example 'How do I do that?' or 'Explain ...'.
        If there are no suggestions, reply with an empty list.
    ";

    private string[]? suggestions;
    private CancellationTokenSource? cancellation;

    [Parameter]
    public EventCallback<ChatMessage> OnSelected { get; set; }

    public void Clear()
    {
        suggestions = null;
        cancellation?.Cancel();
    }

    public void Update(IReadOnlyList<ChatMessage> messages)
    {
        // Runs in the background and handles its own cancellation/errors
        _ = UpdateSuggestionsAsync(messages);
    }

    private async Task UpdateSuggestionsAsync(IReadOnlyList<ChatMessage> messages)
    {
        cancellation?.Cancel();
        cancellation = new CancellationTokenSource();

        try
        {
            var response = await ChatClient.GetResponseAsync<string[]>(
                [.. ReduceMessages(messages), new(ChatRole.User, Prompt)],
                useNativeJsonSchema: true, cancellationToken: cancellation.Token);
            if (!response.TryGetResult(out suggestions))
            {
                suggestions = null;
            }

            StateHasChanged();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async Task AddSuggestionAsync(string text)
    {
        await OnSelected.InvokeAsync(new(ChatRole.User, text));
    }

    private IEnumerable<ChatMessage> ReduceMessages(IReadOnlyList<ChatMessage> messages)
    {
        // Get any leading system messages, plus up to 5 user/assistant messages
        // This should be enough context to generate suggestions without unnecessarily resending entire conversations when long
        var systemMessages = messages.TakeWhile(m => m.Role == ChatRole.System);
        var otherMessages = messages.Where((m, index) => m.Role == ChatRole.User || m.Role == ChatRole.Assistant).Where(m => !string.IsNullOrEmpty(m.Text)).TakeLast(5);
        return systemMessages.Concat(otherMessages);
    }
}

```

## Pages/Chat/ChatSuggestions.razor.css

```css
.suggestions {
    text-align: right;
    white-space: nowrap;
    gap: 0.5rem;
    justify-content: flex-end;
    flex-wrap: wrap;
    display: flex;
    margin-bottom: 0.75rem;
}

```

