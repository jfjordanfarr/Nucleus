---
title: "MCP Tool: FileAccess Server Architecture"
description: "Detailed architecture for the Nucleus_FileAccess_McpServer, outlining its purpose, MCP operations for ephemeral file content retrieval, core IArtifactProvider logic, dependencies, and security model."
version: 1.1 # Incremented version
date: 2025-05-29 # Updated date
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
  - title: "MCP Tools Overview"
    link: "../01_MCP_TOOLS_OVERVIEW.md"
  - title: "Comprehensive System Architecture"
    link: "../../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md" # Corrected link
  - title: "Core Abstractions, DTOs, and Interfaces"
    link: "../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md"
  - title: "Security Overview and Governance"
    link: "../../Security/01_SECURITY_OVERVIEW_AND_GOVERNANCE.md"
  - title: "M365 Agents Overview"
    link: "../../Agents/01_M365_AGENTS_OVERVIEW.md"
  - title: "Foundations: MCP & M365 Agents SDK"
    link: "../../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md" # Corrected link
---

# MCP Tool: FileAccess Server Architecture (`Nucleus_FileAccess_McpServer`)

## 1. Purpose and Core Responsibilities

The `Nucleus_FileAccess_McpServer` (referred to as FileAccess MCP Server) is a specialized backend Model Context Protocol (MCP) tool within the Nucleus ecosystem, designed with a strong emphasis on security and ephemeral data handling.

*   **Primary Goal:** To provide a secure, standardized, and auditable mechanism for Nucleus M365 Persona Agents to ephemerally retrieve the content of files. These files are referenced by `ArtifactReference` DTOs, which contain platform-specific identifiers.
*   **Capabilities Encapsulated:** This server abstracts the complexities of interacting with various file storage platforms by utilizing a pluggable system of `IArtifactProvider` implementations (e.g., for Microsoft Graph, local file system for testing/development). Crucially, it ensures that file content is streamed directly to the calling agent and is **never persisted** by this MCP tool, upholding the "Zero Trust for User File Content" principle.
*   **Contribution to M365 Agents:** It enables M365 Persona Agents to access file content required for analysis, processing, or summarization without needing to directly implement or manage diverse platform SDKs for file retrieval. This promotes a cleaner separation of concerns and enhances the security posture of the overall system by centralizing ephemeral file access logic.

## 2. Key MCP Operations / Tools Exposed

The FileAccess MCP Server primarily exposes one core MCP operation for retrieving file content.

*   **`FileAccess.GetEphemeralContentStream`**
    *   **Description:** Retrieves the content of an artifact (file) as an ephemeral, forward-only stream. This is the primary method called by M365 Persona Agents when they need to process the actual content of a file (e.g., for LLM analysis, content extraction by a `Nucleus_ContentProcessing_McpServer`, or direct inspection by an agent's logic).
    *   **Input Parameters (DTO):** `GetEphemeralContentRequest { Nucleus.Shared.Kernel.Abstractions.Models.ArtifactReference Reference, string TenantId }`.
        *   The `Reference` object (defined in `Nucleus.Shared.Kernel.Abstractions.Models`) contains platform-specific identifiers (e.g., Graph Drive ID and Item ID), a `ProviderHint` (e.g., "MicrosoftGraph"), and any other necessary context for the relevant `IArtifactProvider` to locate and access the file.
        *   The `TenantId` is extracted from the authenticated caller's token and used for logging, auditing, and potentially routing to tenant-specific provider configurations.
    *   **Output/Return Value (DTO):** `EphemeralContentStreamResponse { System.IO.Stream? ContentStream, string? FileName, string? ContentType, long? SizeBytes, string? ErrorMessage }`.
        *   `ContentStream`: A readable stream representing the file content. It is the responsibility of the MCP client (the M365 Agent) to properly read and dispose of this stream.
        *   `FileName`, `ContentType`, `SizeBytes`: Metadata about the file, if retrievable by the `IArtifactProvider`.
        *   `ErrorMessage`: Populated if the operation fails.
    *   **Idempotency:** This is an idempotent read operation.
    *   **Error Handling:** The operation returns an appropriate error message in `ErrorMessage` (and `ContentStream` will be null) for scenarios such as:
        *   Unsupported `ArtifactReference.ProviderHint` (no matching `IArtifactProvider` registered).
        *   Access denied by the underlying file storage platform (e.g., the M365 Agent lacks permissions via Microsoft Graph).
        *   File not found on the source platform.
        *   Errors during stream retrieval from the source platform.

*Potential Future Operations:*
*   `FileAccess.GetFileMetadata`: While `ArtifactMetadata` is primarily managed by `Nucleus_KnowledgeStore_McpServer`, a lightweight operation to fetch basic, real-time file metadata (like size, modification date) directly via an `IArtifactProvider` could be considered if needed, to avoid potential staleness of stored metadata for highly dynamic files before full processing.
*   `FileAccess.ListSupportedProviders`: An operation to list the `ProviderHint` values for all registered and configured `IArtifactProvider` implementations in this MCP server instance.

## 3. Core Internal Logic & Components

The internal architecture of the FileAccess MCP Server is centered around the dynamic resolution and execution of `IArtifactProvider` implementations.

*   **`IArtifactProvider` Resolution:**
    *   The server maintains a collection of registered `IArtifactProvider` implementations (dependency injected). Each provider is typically associated with a specific `ProviderHint` string (e.g., "MicrosoftGraph", "LocalFileSystem").
    *   When an MCP request for `GetEphemeralContentStream` arrives, the server inspects the `ProviderHint` (and potentially other properties) within the `ArtifactReference` DTO to select the appropriate `IArtifactProvider` instance from its collection.
*   **`IArtifactProvider` Execution:**
    *   Once the correct provider is resolved, the server invokes its `GetArtifactContentAsync(ArtifactReference reference, CancellationToken cancellationToken)` method (or a similar method defined in the `IArtifactProvider` interface from `Nucleus.Shared.Kernel.Abstractions.Interfaces`).
    *   This method is responsible for interacting with the underlying file storage platform (e.g., calling Microsoft Graph APIs) to retrieve the file's content as a stream.
*   **Security Context Propagation (Critical):**
    *   The FileAccess MCP Server receives the calling M365 Persona Agent's security context, primarily its Azure AD token.
    *   For providers like `GraphArtifactProvider` that access user-specific M365 resources, this security context is paramount. The `GraphArtifactProvider` must use this context to make calls to Microsoft Graph *on behalf of the M365 Agent* (which itself may be acting on behalf of a user or using its own application permissions).
    *   This can be achieved by passing the agent's token to the provider, which then uses it in Graph API calls, or by initiating an On-Behalf-Of (OBO) flow if the FileAccess server needs to obtain a Graph-specific token for the agent. The choice depends on the M365 Agent SDK's capabilities and the permissions model.
    *   This ensures that file access adheres to the permissions granted to the M365 Agent (and transitively, the user) on the M365 platform.
    *   When an M365 Persona Agent calls `FileAccess.GetFileContentStream`, it provides an `ArtifactReference` which includes details about the file's location (e.g., SharePoint site ID, item ID). Crucially, the `Nucleus_FileAccess_McpServer` **must not** use its own identity or a generic service account to fetch this file content from Microsoft Graph or other sources. Instead, it **must operate within the security context of the calling M365 Persona Agent.** The `GraphArtifactProvider` (or any other provider for M365 sources) must use this context to make calls to Microsoft Graph *on behalf of the M365 Agent* that invoked the MCP operation. This can be achieved by the M365 Agent passing its own Graph-scoped access token (obtained for the necessary Graph permissions like `Files.Read.All` or `Sites.Read.All` depending on the file location) as part of the MCP request to the `FileAccess.GetFileContentStream` operation, or by this MCP tool initiating an On-Behalf-Of (OBO) flow using the M365 Agent's incoming token to obtain a new token with the required Graph permissions. This ensures that file access strictly adheres to the permissions of the M365 Agent, which in turn reflects the user's permissions or the agent's configured application permissions.
*   **Streaming Focus:** The server is designed to handle file content as streams. `IArtifactProvider` implementations should return `System.IO.Stream` objects. The MCP framework (ASP.NET Core) then streams this response back to the M365 Agent, minimizing memory footprint on the server, especially for large files.
*   **No Persistent Storage of File Content:** This server **strictly does not store any file content** it retrieves. All access is ephemeral; the content is streamed from the source, through this MCP server, to the M365 Agent. Once the stream is consumed or the request ends, the content is gone from this server's memory.
*   **`tenantId` Scoping:** While the primary authorization for file access is based on the `ArtifactReference` and the permissions enforced by the underlying platform (e.g., Microsoft Graph), the `tenantId` extracted from the calling M365 Agent's token is used for:
    *   Logging and Auditing: All access attempts are logged with the `tenantId`.
    *   Potentially routing to tenant-specific configurations of `IArtifactProvider`s if such a multi-tenant hosting model for providers is ever required (though typically, providers are configured globally, and tenant isolation happens at the source platform).

## 4. Dependencies

The FileAccess MCP Server has the following dependencies:

*   **Azure Services:**
    *   **Azure Key Vault:** Used to securely store any secrets that might be needed by specific `IArtifactProvider` implementations. For example, if an `IArtifactProvider` for a non-M365 system requires its own API key or service account credentials, those would be stored in Key Vault and accessed by this MCP server via its Managed Identity.
    *   **Azure App Configuration (Optional but Recommended):** For managing non-sensitive operational configurations, such as enabling/disabling specific providers or setting default timeouts.
*   **External Services/LLMs:**
    *   Generally, **none directly**. This server's role is to provide file content *to* other services or agents that might then use LLMs for analysis (e.g., `Nucleus_ContentProcessing_McpServer` or the M365 Persona Agents themselves).
*   **Other Nucleus MCP Tools:**
    *   Unlikely to call other Nucleus MCP tools directly. It is a specialized service consumed by M365 Persona Agents.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions`: Essential for DTOs like `ArtifactReference`, the `IArtifactProvider` interface definition, and request/response models for MCP operations.
    *   `Nucleus.Infrastructure.FileProviders.Platform` (or a similarly named project, e.g., `Nucleus.Infrastructure.ArtifactProviders`): This project would contain concrete `IArtifactProvider` implementations, such as:
        *   `GraphArtifactProvider`: For accessing files in OneDrive/SharePoint via Microsoft Graph.
    *   `Nucleus.Infrastructure.FileProviders.Local` (or similar): For a `LocalFileArtifactProvider` used during development and testing to access files from the local file system.
*   **Platform SDKs:**
    *   Specific `IArtifactProvider` implementations will depend on the SDKs for the platforms they target. For example, `GraphArtifactProvider` will depend on the `Microsoft.Graph` (or `Microsoft.Graph.Beta`) NuGet package.

## 5. Security Model

The security model of the FileAccess MCP Server is critical due to its role in accessing potentially sensitive file content.

*   **Authentication of Callers (M365 Persona Agents):**
    *   All incoming MCP requests from M365 Persona Agents **must** be authenticated using Azure AD OAuth 2.0 bearer tokens, following the same validation process as other Nucleus MCP servers (signature, audience, issuer, app ID).
*   **Authorization within the Tool:**
    *   **Tenant Isolation:** The `tid` (tenant ID) claim from the validated token is used for logging and auditing. While the FileAccess server itself doesn't typically filter files by `tenantId` (as `ArtifactReference` is specific), this ensures auditability.
    *   **Delegation of File-Level Authorization:** The core authorization for *accessing the file content itself* is **delegated to the underlying `IArtifactProvider` and the platform it interacts with** (e.g., Microsoft Graph). The FileAccess MCP Server ensures the request is from an authenticated M365 Agent. The `IArtifactProvider` (e.g., `GraphArtifactProvider`) then uses the M365 Agent's security context (passed token or OBO flow) to make the actual call to the file storage platform. That platform (e.g., Microsoft Graph) enforces whether the agent (acting for itself or a user) has the necessary permissions (e.g., `Files.Read`, `Sites.Read.All`) for the specific file identified in the `ArtifactReference`.
*   **Authentication to Dependencies:**
    *   **`GraphArtifactProvider`:** As described above, this provider primarily leverages the calling M365 Persona Agent's security context (token) to interact with Microsoft Graph. In scenarios where the M365 Agent might not have a user context (e.g., a background process with application permissions), the M365 Agent would use its own application identity for Graph calls, and these permissions would be respected.
    *   **FileAccess MCP Server's Own Identity:** The server uses its **Azure Managed Identity** to authenticate to Azure Key Vault (for retrieving secrets for `IArtifactProvider` configurations, if any) and Azure App Configuration.

## 6. Data Handling & Persistence

This is a critical and simple aspect of this MCP server:

*   **No Persistent Storage:** The `Nucleus_FileAccess_McpServer` **DOES NOT PERSIST ANY DATA**. Its sole purpose is the ephemeral retrieval and streaming of file content. It acts as a pass-through conduit for file streams.
*   **Ephemeral Nature:** File content is held in memory only for the duration necessary to stream it as part of the MCP response. Once the stream is closed or the request completes, the data is no longer held by this server.

## 7. Deployment & Configuration Considerations

The FileAccess MCP Server is deployed as a standard Azure cloud-native application.

*   **Deployment Environment:** Typically deployed as an **Azure Container App** or Azure App Service for containers.
*   **Configuration Management:** Via Azure App Configuration and Azure Key Vault.
*   **Key Configuration Parameters:**
    *   Registration and configuration for different `IArtifactProvider` implementations (e.g., which provider class maps to which `ProviderHint`).
    *   If any `IArtifactProvider` requires its own credentials (e.g., for a generic web URL fetcher, or a specific non-M365 system), these would be configured as secret names to be retrieved from Azure Key Vault.
        *   Example: `ThirdPartyStorageApiKeySecretName`.
    *   Timeout settings for upstream requests made by `IArtifactProvider`s.

## 8. Future Considerations / Potential Enhancements

*   **Support for More `IArtifactProvider` Types:** Easily extendable by adding new implementations for other cloud storage providers (e.g., Azure Blob Storage, Amazon S3, Google Cloud Storage) or on-premise systems (if a secure gateway mechanism is available).
*   **Advanced Streaming Capabilities:** For extremely large files or clients with limited bandwidth, explore features like resumable downloads or chunked streaming if direct streaming becomes problematic, though this adds complexity.
*   **Content Transformation (Minimal):** Potentially offer very basic, on-the-fly transformations if universally useful (e.g., character set conversion), but generally, complex transformations should be handled by a dedicated `Nucleus_ContentProcessing_McpServer`.
*   **Caching of File Handles/Short-Lived Tokens (Highly Sensitive):** If performance for repeated access to the *same file by the same agent in a short window* becomes an issue, explore secure caching of file access handles or short-lived platform-specific access tokens. This would need extremely careful design to not compromise the ephemeral nature and security model.
