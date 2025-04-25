# Current Plan

## Goal

Our current aim is to get the Nucleus project building and running with a clear, maintainable API-First architecture and demonstrable core functionality:
- [X] Ability for client adapters (Console, Teams) to interact *exclusively* through the `Nucleus.Services.Api` (Architecture Defined).
- [X] Ability for the `ApiService` to orchestrate core tasks (Ingestion, Query) based on API requests (Basic non-activated flow tested).
- [ ] Ability to generate basic data visualizations using the Dataviz HtmlBuilder (triggered via API call).

## Notes on Current State

- **Build Status:** Build **SUCCEEDED** (with warnings: `ASPIRE004`, `CA2000` in tests).
- **Architectural Principle:** **API-First Principle Adopted:** All external interactions (Console, Teams, future clients, tests) must go exclusively through the `Nucleus.Services.Api`. Adapters are pure translators. Core logic, security, and sensitive operations are centralized in the `ApiService`.
- **API Interaction:** `ApiService` performs Activation Checks and supports Hybrid Execution (Sync/Async). Async tasks are placed onto an internal Service Bus **Queue** (not using pub/sub topics for this flow) for background processing. Non-activated interactions return `200 OK` with no body.
- **Documentation:** Key architecture documents aligned with API-First.
- **Testing:** Basic integration tests for non-activated API endpoints (`/ingest`, `/query`) are **PASSING**. Tests correctly use `WebApplicationFactory` and a null `IArtifactMetadataRepository`.

## Prioritized To-Do List

**Phase 0: Foundational Structure Definition (Highest Priority)**

1.  **Define Core Namespaces & Folder Structure:**
    *   [X] Analyze current `src/` structure (folders, projects, namespaces).
    *   [X] Propose a clear, consistent, and extensible structure based on .NET best practices and project needs.
    *   [X] Document the agreed-upon structure (e.g., in a new `ARCHITECTURE_NAMESPACES_FOLDERS.md`).
    *   [X] Plan and execute necessary refactoring (renaming, moving files/folders, updating namespaces/references) - *This might become its own Phase*. 

**Phase 0.5: Codebase Structure Refactoring**

1.  **Establish New Top-Level Folders:**
    *   [X] Create `Aspire/` folder.
    *   [X] Create `src/` folder (if it doesn't fully exist in the desired root structure).
    *   [X] Create `tests/` folder.
2.  **Move and Organize Projects:**
    *   [X] Move `Nucleus.ServiceDefaults/` to `Aspire/Nucleus.ServiceDefaults/`.
    *   [X] Move `Nucleus.Abstractions/` to `src/Nucleus.Abstractions/`.
    *   [X] Create `src/Nucleus.Domain/` and move `Nucleus.Domain.Processing/` into it. Consider if a `Nucleus.Domain.Core/` is needed immediately.
    *   [X] Create `src/Nucleus.Domain/Personas/` and move `src/Personas/Nucleus.Personas.Core/` into it.
    *   [X] Create `src/Nucleus.Application/` (Project(s) like `Nucleus.Application.Core` to be created within).
    *   [X] Create `src/Nucleus.Infrastructure/` and move existing infra projects (like `Nucleus.Infrastructure.Persistence`, Adapters like `Console`, `Teams`) into appropriate subfolders (e.g., `Data/CosmosDb`, `Adapters/Teams`).
    *   [X] Create `src/Nucleus.Services/` and move `Nucleus.Services.Api/` into it.
    *   [X] Move existing test projects (e.g., `Nucleus.Services.Api.IntegrationTests`) into `tests/`. Create standard subfolders like `Unit`, `Integration`, `EndToEnd` if needed.
3.  **Update Namespaces:**
    *   [X] Systematically update C# namespaces in all `.cs` files within the moved projects to reflect the new `Nucleus.<Layer>.<SubArea>` structure. **(Manual Task: Use IDE Refactoring - see guide)**
4.  **Update Project References:**
    *   [X] Update `<ProjectReference>` paths in all `.csproj` files to point to the new locations.
5.  **Update Solution File:**
    *   [X] Update project paths in `Nucleus.sln`.
6.  **Verification:**
    *   [X] Ensure the entire solution builds successfully (`dotnet build`).

**Phase 0.6: Post-Refactor Build Fixes (High Priority)**

1.  **Fix Project Reference Typo (Gemini Item 2.1):**
    *   [X] Correct the typo in the `<ProjectReference>` path for `Nucleus.Infrastructure.Adapters.Console` within `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj` (change `Infrastrucutre` to `Infrastructure`).
2.  **Fix AppHost Build Errors:**
    *   [X] Resolve CS0234/CS0246/CS0311 errors related to `AddProject` reference in `Nucleus.AppHost`.
    *   [ ] Address `ASPIRE004` warnings related to non-executable project references (Low Priority).

**Phase 1: API-First Code Refactoring & Core API Logic**

1.  **Refactor Console Adapter (`Nucleus.Adapters.Console`):**
    *   [X] Major rewrite to act as a pure API client.
    *   [X] Enhance/Centralize `NucleusApiServiceAgent` (or similar `HttpClient` based service) for *all* API interactions. (`NucleusApiServiceAgent` verified as suitable client).
    *   [ ] Address potential context loss in `NucleusIngestionRequest` mapping (Gemini Item 3.3):
        *   [ ] Review `OrchestrationService` (async path) and `HandleQueuedInteractionAsync` to ensure necessary fields (`ReplyToMessageId`, `Metadata`, etc.) from `AdapterRequest` are preserved in `NucleusIngestionRequest` and correctly mapped back.
        *   [ ] Correct hardcoded values like `TenantId = "UnknownTenant"` if necessary.
    *   [X] Remove `ConsoleArtifactProvider` and replace direct file handling logic with calls to the API's ingestion endpoint. (`ConsoleArtifactProvider` deleted).
    *   [X] Update `Program.cs` to orchestrate console input/output and API calls via the service agent.

2.  **Verify/Refactor Teams Adapter (`Nucleus.Adapters.Teams`): [Blocked by Domain Processing Errors]**
    *   [X] Confirm `TeamsAdapterBot` translates `Activity` to API request DTOs (`AdapterRequest`) and calls `ApiService` via an agent/client (`HttpClient`).
    *   [ ] Remove `TeamsGraphFileFetcher` and ensure file reference info (`ArtifactReference`) is passed to the API. (**Reason:** Align code with documentation (`ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md`) stating fetching logic belongs in `ApiService` per API-First model - Gemini Item 3.1).
    *   [ ] Review `GraphClientService` usage; ideally, Graph operations for *content* retrieval happen within `ApiService`, triggered by adapter-provided references. (**Reason:** Align with API-First model - Gemini Item 3.1).

3.  **Refactor Core API & Domain Logic (`Nucleus.Services.Api`, `Nucleus.Domain.Processing`):**
    *   [X] Adapt `OrchestrationService` (within `ApiService`) to handle incoming API requests (`InteractionController`).
        *   [X] Define `IActivationChecker` interface.
        *   [X] Implement basic `ActivationChecker`.
        *   [X] Integrate `IActivationChecker` into `OrchestrationService`.
        *   [X] Define `IBackgroundTaskQueue` interface.
        *   [X] Implement basic `InMemoryBackgroundTaskQueue`.
        *   [X] Integrate `IBackgroundTaskQueue` into `OrchestrationService`.
        *   [X] Implement basic Sync/Async decision logic.
        *   [X] Implement basic queuing for Async path.
        *   [X] Implement background worker service (`QueuedInteractionProcessorService`) to process queued items.
        *   [X] Register orchestration services (`IActivationChecker`, `IBackgroundTaskQueue`, `QueuedInteractionProcessorService`) in `ApiService`.
        *   [X] Refine Sync/Async decision logic (using `PersonaConfiguration.ProcessingPreference`).
        *   [X] Update `IPersonaManager` methods (`ProcessSalientInteractionAsync`, `InitiateNewSessionAsync`) to return meaningful `AdapterResponse` for synchronous path.
        *   [X] Implement API endpoints (in `InteractionController`) for core actions: Ingestion, Query, Dataviz request (Placeholders added).
        *   [X] Ensure `ApiService` uses appropriate `IArtifactProvider` implementation (Registered `LocalFileArtifactProvider`).
        *   [X] Implement basic API request/response handling in `InteractionController`.
        *   [X] Register core domain services (`IPersonaManager`, `IDatavizHtmlBuilder`) in `ApiService`.
        *   [ ] Resolve Build Errors in `Nucleus.Services.Api`.
            *   [X] Refactor `Program.cs` to use `AddNucleusServices`/`MapNucleusEndpoints` extension methods.
            *   [X] Fix build errors in `Program.cs` (CS0305, CS1061, CS7036, CS0246 - old persona code; CS1061 - missing using for MapNucleusEndpoints; CS4033 - await issues).
            *   [X] Verify `using` statements in `WebApplicationBuilderExtensions.cs` (CS0234 errors - likely already resolved).
        *   [X] Resolve Warnings in `LocalFileArtifactProvider` (CS0162 / CS1998 addressed).
        *   [ ] Implement Final Response Notification (Gemini Item 3.2):
            *   [ ] Ensure `OrchestrationService` or `QueuedInteractionProcessorService` correctly resolves and uses `IPlatformNotifier` to send the final `AdapterResponse` back to the originating platform, especially for asynchronous completions, aligning with `ARCHITECTURE_PROCESSING_ORCHESTRATION.md`.

4.  **Update/Refine Testing Strategy:**
    *   [X] Update `Docs/Architecture/09_ARCHITECTURE_TESTING.md` to reflect API-centric testing (integration tests via `HttpClient`, PS scripts, refactored Console.Tests).
    *   [X] Create new C# project `tests/Services/Nucleus.Services.Api.IntegrationTests` for API integration tests (using MSTest).
    *   [X] Move `test_*.ps1` scripts and `TestData` directory from `Adapters.Console.Tests` to the new `Api.IntegrationTests` project.
    *   [X] Implement initial API integration tests using `HttpClient` within the new project.
    *   [X] Adapt PowerShell scripts (`test_*.ps1`) to target the new API endpoints (`/ingest`, `/query`).
    *   [X] **Milestone:** Executed integration test suite (`dotnet test`) for the first time!
    *   [X] **Fix Test Failure (HttpClient BaseAddress):** Configure `HttpClient` created via `IHttpClientFactory` in `ApiIntegrationTests` with the correct `BaseAddress` from the `WebApplicationFactory` server.
    *   [X] **Fix Test Failure (TestData Copy):** Ensure the `TestData/test_context.txt` file is copied to the output directory during the build process for `Nucleus.Services.Api.IntegrationTests`.
    *   [X] **Fix Test Failure (InteractionController Response):** Modified `InteractionController` to return `Ok()` (200 OK, no body) for `NotActivated` status, resolving JSON serialization errors in tests.
    *   [X] **Fix Test Failure (Assertion Logic):** Updated test assertions for non-activated scenarios to check only for success status code, not deserialize the (now empty) body.
    *   [X] **Fix Test Failure (Metadata Check):** Removed invalid metadata persistence check from non-activated ingestion test.
    *   [ ] Update `Docs/Architecture/11_ARCHITECTURE_NAMESPACES_FOLDERS.md` (Gemini Item 1.1):
        *   [ ] Ensure this document accurately reflects the current refactored project structure in `src/`, `tests/`, `Aspire/`.
    *   [ ] Clarify Storage Architecture Diagram (Gemini Item 1.2):
        *   [ ] Review the Mermaid diagram in `Docs/Architecture/03_ARCHITECTURE_STORAGE.md`.
        *   [ ] Modify or annotate the "SourceStore" element to clarify it represents external user storage accessed via adapters, not a Nucleus-managed store, aligning with the "No Intermediate Storage" principle.
    *   [ ] Update Client Architecture Document (Gemini Item 1.3):
        *   [ ] Revise `Docs/Architecture/05_ARCHITECTURE_CLIENTS.md` to remove outdated references to `IPlatformAdapter` and fully align with the API-First model where adapters are pure API clients (referencing `ARCHITECTURE_API_CLIENT_INTERACTION.md` might be more appropriate).

6.  **Address Code Quality & Consistency:**
    *   [X] Resolve remaining static analysis warnings (StyleCop, etc.).
    *   [X] Refactor `ApiIntegrationTests` to use `IHttpClientFactory` resolved from `WebApplicationFactory.Services` to create its `HttpClient`, resolving the `CA2213/CA2000` warning using DI best practices.
    *   [X] Refactor shared interaction processing logic between `OrchestrationService` and `QueuedInteractionProcessorService`.
    *   [X] Search for and address "TODO", "Temporary", "Placeholder" comments.
    *   [X] Resolve Warnings in `LocalFileArtifactProvider`.
    *   [X] **Fix Test Warning (CA2000):** Suppressed warning for `WebApplicationFactory` creation in `ApiIntegrationTests`.
    *   [X] **Fix Build Warning (CS0618):** Removed obsolete `ICredentialProvider` usage in `Nucleus.Services.Api` DI setup.
    *   [X] **Fix Build Warnings (CA1062):** Added null checks to extension methods in `WebApplicationBuilderExtensions.cs`.

**Phase 1.5: Core Logic Refinement & Activation Testing (Current Focus)**

**Note:** Build succeeded. Remaining warnings: `ASPIRE004` (Low Priority), `CA1822` (Null Repo - Minor).

1.  **Implement Activation Tests:**
    *   [ ] Add new integration test cases in `ApiIntegrationTests` that *do* include an activation trigger (e.g., mentioning `@Nucleus`).
    *   [ ] Verify correct response bodies (e.g., `AdapterResponse`) are returned for `ProcessedSync` status.
    *   [ ] Verify metadata *is* persisted correctly during ingestion when activated (requires replacing `NullArtifactMetadataRepository` with a testable mock or the real one if feasible).
    *   [ ] Verify correct status codes (e.g., `202 Accepted`) are returned for `AcceptedAsync` status.

2.  **Finalize Async Notifications:**
    *   [X] Implement final response notification via `IPlatformNotifier` within `QueuedInteractionProcessorService`, triggered by the `OrchestrationResult` from `OrchestrationService.HandleQueuedInteractionAsync`. (Corresponds to Checkpoint Item 1 / Gemini Item 3.2).
3.  **Review Ingestion Request Mapping:**
    *   [X] Analyze the mapping from `AdapterRequest` to `NucleusIngestionRequest` within `OrchestrationService` to ensure no critical context (like `ReplyToMessageId`) is lost. (Corresponds to Checkpoint Item 2 / Gemini Item 3.3).
4.  **Implement Artifact Metadata Persistence:**
    *   [ ] Implement a concrete `IArtifactMetadataRepository` (e.g., using Azure Cosmos DB) and replace the `InMemoryArtifactMetadataRepository` placeholder. Update DI registration. (Corresponds to Checkpoint Item 4 / Gemini Item 3.5).
5.  **Update Architecture Documentation:**
    *   [ ] Review and update relevant architecture documents (e.g., `ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md`, `ARCHITECTURE_ADAPTERS_*.md`, `ARCHITECTURE_API_*.md`) to reflect the implemented API-First model, hybrid execution, notification mechanisms, and artifact handling. (Corresponds to Checkpoint Item 3 / Gemini Items 1.3, 3.6).

7.  **Implement Artifact Content Retrieval (`IArtifactProvider` Strategy):**
    *   [ ] **Design Provider Selection:** Implement a mechanism within the `ApiService` (e.g., a factory or strategy pattern in `OrchestrationService`) to select the appropriate `IArtifactProvider` implementation based on the `ArtifactReference.ReferenceType` (e.g., `"local_file_path"`, `"msgraph"`, `"url"`).
    *   [ ] **Register Providers:** Register all necessary `IArtifactProvider` implementations (currently only `LocalFileArtifactProvider`) with the DI container. The selection mechanism will resolve the correct one at runtime.
    *   [ ] **Implement `GraphArtifactProvider`:** Create a new provider (`Nucleus.Infrastructure.GraphApi.GraphArtifactProvider` or similar, consider placing within `Nucleus.Services.Api.Infrastructure` per Gemini Item 3.4) that implements `IArtifactProvider` to retrieve file content from Teams/SharePoint using the Microsoft Graph SDK based on a `msgraph` `ReferenceType` and appropriate `ReferenceId`. (Requires adding Graph SDK dependencies).
    *   [ ] **Implement `UrlArtifactProvider` (Optional/Future):** Consider a provider to fetch content directly from public URLs based on a `"url"` `ReferenceType`.

8.  **Implement Basic Query Handling:**
    *   [ ] Ensure the `/query` API endpoint receives requests.
    *   [ ] Implement basic passthrough to an LLM (e.g., using `GoogleAiChatClientAdapter`) within `ApiService` orchestration.

9.  **Validate Dataviz Functionality (via API):**
    *   [ ] Test the `/dataviz` (or similar) API endpoint.
    *   [ ] Verify `DatavizHtmlBuilder` is correctly invoked and returns an HTML artifact reference/content as defined in the API docs.

## Key Discoveries & Decisions Log

*   Adopted **API-First** principle: Adapters translate, `ApiService` centralizes logic/security.
*   Implemented **Hybrid Execution Model** (Sync/Async) via API.
*   API performs **Activation Check** based on rules (including implicit activation for replies).
*   Client Adapters must extract and send **platform-specific context** (reply IDs, user info, file references, etc.) to the API.
*   Removed obsolete Teams-specific registrations (`GraphClientService`, `TeamsGraphFileFetcher`, `TeamsNotifier`) from `ApiService` DI setup as part of API-First cleanup.
*   Removed obsolete `/ingest` API endpoint and `OrchestrationService.ProcessIngestionRequestAsync` method; ingestion is handled implicitly by the main interaction processing pipeline when artifacts are present.
*   Documentation phase largely complete, focus shifts to code refactoring.
*   Resolved build errors in `Nucleus.Services.Api` and `Nucleus.AppHost`.
*   **Integration Tests Executed & Fixed:** First run of `dotnet test` on `ApiIntegrationTests` initially failed but issues related to HttpClient setup, TestData copying, API response handling for `NotActivated` status, and test assertion logic were resolved. Basic non-activated API endpoints are now passing.

## Known Issues / Placeholders

*   **Build Warnings:** Remaining warnings: `ASPIRE004` (Non-executable AppHost references), `CA1822` (Null repo method can be static).
*   **InMemoryArtifactMetadataRepository:** Currently a placeholder to allow the build. Needs replacement with a real implementation (e.g., Cosmos DB) for actual data persistence.
*   **IPersona<object> Injection in Program.cs:** The factory method for `DefaultPersonaManager` currently injects a basic placeholder `Persona<object>` instance. A proper strategy is needed to resolve/load the correct `IPersona<>` implementation corresponding to the specific persona configuration being registered.
*   **Error Handling:** Robust error handling needs to be added throughout the API and background services.
*   **Configuration Validation:** Implement validation for configuration sections (ServiceBus, Personas, etc.).

## Future Considerations / Backlog

*   Sophisticated workflow engine (Durable Functions, Dapr).
*   Detailed configuration for Activation Rules.
*   LLM Function Calling integration.
*   Knowledge Storage & Retrieval mechanisms (Vector DB integration).
*   Implement other Adapters (Slack, Email, Discord).
*   Console Adapter Message Persistence (for async results).
*   Re-enable/test Aspire Dashboard.
*   Proper error handling and logging throughout API and Adapters.
*   Security hardening (Authentication/Authorization for API).
