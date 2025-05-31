---
title: "Nucleus Testing Strategy"
description: "Outlines the comprehensive testing strategy for the Nucleus platform, encompassing M365 Persona Agents, backend MCP Tools, and .NET Aspire orchestration."
version: 1.2
date: 2025-05-29
parent: ./00_DEVELOPMENT_LIFECYCLE_OVERVIEW.md
see_also:
    - title: "Comprehensive System Architecture"
      link: "../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md"
    - title: "Namespaces and Folder Structure"
      link: ./01_NAMESPACES_FOLDERS.md
    - title: "Foundations: Technology Primer"
      link: "../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md"
---

# Nucleus Testing Strategy

## 1. Introduction

A robust testing strategy is paramount to the success and reliability of the Nucleus platform. Given the distributed nature of Nucleus, which comprises **Microsoft 365 Persona Agent applications** and backend **Model Context Protocol (MCP) Tool/Server applications**, orchestrated and developed using **.NET Aspire**, our testing approach must be comprehensive and adaptable.

This document outlines the multi-layered testing strategy for Nucleus, emphasizing **Test-Driven Development (TDD)** as a core practice. Our goal is to ensure quality, maintainability, and confidence in every component and their interactions.

## 2. Testing Philosophy

Our testing philosophy is pragmatic, focusing on validating core logic, ensuring seamless integration between components, and verifying end-to-end system behavior. Key tenets include:

1.  **Test-Driven Development (TDD):** Write tests before writing code. This applies to unit, service integration, and even system integration tests where appropriate, to clearly define contracts and expected behaviors.
2.  **Focus on Core Logic:** Prioritize robust unit tests for fundamental abstractions (`Nucleus.Abstractions`), domain logic within M365 Agents (e.g., `IPersonaRuntime` strategies, `IAgenticStrategyHandler` implementations), and within MCP Tools (e.g., RAG pipeline logic, specific content processors, data access).
3.  **Component Interaction Confidence:**
    *   **M365 Agent Applications:** Test interactions between an agent's internal components and its immediate dependencies (e.g., mocked MCP clients or platform SDK interfaces).
    *   **Nucleus MCP Tool/Server Applications:** Test each tool's logic with its direct dependencies (e.g., `KnowledgeStore_McpServer` with an in-memory repository or emulated/Testcontainer-managed Cosmos DB).
4.  **System Integration Confidence with .NET Aspire:** Utilize **.NET Aspire's testing support (`Aspire.Hosting.Testing`)** as the primary mechanism for System Integration Testing. This orchestrates the relevant distributed application components defined in the `Nucleus.AppHost` project, including M365 Agents, MCP Tools, and shared emulated resources (Cosmos DB, Service Bus).
5.  **Security and Data Governance Validation:** Actively design tests to verify tenant isolation, persona-specific data boundaries, and correct permission enforcement by M365 Agents when interacting with MCP Tools.
6.  **Clarity and Maintainability:** Strive for clear, concise, and maintainable tests. Tests serve as living documentation.
7.  **Avoid Brittle UI Tests (for M365 Platforms):** Automated UI testing of the M365 platforms themselves (Teams, Outlook) is out of scope. Focus is on the agent and MCP tool backend logic. If a dedicated Nucleus Admin UI is developed, Playwright would be considered for its E2E tests.

## 3. Testing Layers & Tools

We adopt a layered testing strategy using **xUnit.net** as the primary test framework.

### 3.1. Layer 1: Unit Tests

*   **Scope:** Individual classes, methods, and functions in isolation. Focus on business logic within M365 Agent projects (e.g., specific `IAgenticStrategyHandler` logic, prompt generation) and MCP Tool/Server projects (e.g., content extraction algorithms, data transformation, repository method logic).
*   **Tools:** xUnit.net, Moq (or NSubstitute/FakeItEasy).
*   **Goal:** Verify correctness of individual components.

### 3.2. Layer 2: Service Integration Tests

*   **Scope (M365 Agents):** Test an M365 Agent's internal orchestration and its interaction with direct, out-of-process dependencies that are mocked or faked. For example, testing an `IPersonaRuntime` implementation with a mocked `IChatCompletionService` (from `Microsoft.Extensions.AI`) or a mocked MCP client interface.
*   **Scope (MCP Tools):** Test an MCP Tool/Server with its direct, real dependencies where feasible (e.g., an MCP Tool interacting with an in-memory database, or a Testcontainer-managed instance of Cosmos DB). For ASP.NET Core based MCP Tools, `WebApplicationFactory` is used to test the service in-process.
*   **Tools:** xUnit.net, Moq, `WebApplicationFactory`, Testcontainers.
*   **Goal:** Verify interactions between an agent/tool and its immediate, direct dependencies.

### 3.3. Layer 3: System Integration Tests (.NET Aspire AppHost)

*   **Scope:** Testing interactions between multiple orchestrated components of the Nucleus system as defined in the `.NET Aspire AppHost`. This includes one or more M365 Persona Agent(s) (e.g., `Nucleus.Agent.EduFlow`), backend Nucleus MCP Tool/Server(s) (e.g., `Nucleus.McpTool.KnowledgeStore`), and emulated or Testcontainer-managed shared resources (e.g., Azure Cosmos DB emulator, Azure Service Bus emulator).
*   **Purpose:** Verify end-to-end flows and contracts between distributed services. Example:
    1.  A test simulates an M365 `Activity` (e.g., a message in Teams) being sent to an M365 Agent's messaging endpoint.
    2.  The M365 Agent processes the activity and makes one or more calls to orchestrated backend MCP Tools via the `Nucleus.Mcp.Client`.
    3.  The test verifies the expected outcomes, such as data being written to the emulated Cosmos DB, messages being sent to the emulated Service Bus, or specific responses from the MCP Tools.
*   **Key Technology:** `Aspire.Hosting.Testing`.
    *   **Caution:** While `Aspire.Hosting.Testing` is powerful for orchestrating a distributed application for testing, it's crucial to recognize its primary role is setting up the *environment* and providing access to service endpoints and configurations. The actual test logic (sending requests, making assertions) still resides within standard xUnit tests. Be mindful that complex setups can lead to slower test execution, so focus on testing critical inter-service contracts rather than exhaustive component-level logic, which should be covered by Layer 1 and 2 tests.
*   **Goal:** Ensure the distributed system components work together correctly.

### 3.4. Layer 4: End-to-End (E2E) User Interaction Tests

*   **Scope:** Testing user interaction flows within the actual M365 platforms (e.g., sending a message in Microsoft Teams and verifying the M365 Persona Agent's response and behavior).
*   **Purpose:** Validates the complete loop from the user's perspective, through the M365 platform, to the Nucleus M365 Agent, potentially through MCP Tools, and back to the user in the platform.
*   **Tools:** Platform-specific automation tools (e.g., Playwright for Teams web client if feasible), or specialized bot testing frameworks if provided by Microsoft for M365 Agents. This layer is complex and may involve more manual or semi-automated testing initially.
*   **Goal:** Verify the real user experience.

## 4. Test-Driven Development (TDD) in Nucleus

TDD is a foundational practice in Nucleus development:

1.  **Red-Green-Refactor:** Follow the classic TDD cycle.
2.  **M365 Agent Development:**
    *   Before implementing an `IAgenticStrategyHandler`, write unit tests defining its expected behavior for various inputs.
    *   Write service integration tests for `IPersonaRuntime` to define how it should orchestrate different strategy handlers and interact with (mocked) MCP clients or platform services.
3.  **MCP Tool Development:**
    *   Before implementing an MCP Tool endpoint, write service integration tests (using `WebApplicationFactory` if applicable) to define its contract (request/response, side effects).
    *   Write unit tests for the underlying business logic (e.g., RAG pipeline steps, data processors).
4.  **System Integration First:** For new cross-component features, consider writing a high-level System Integration Test first to define the expected interaction and outcome across the distributed system. This helps solidify contracts early.

## 5. Testing Environments

*   **Local Development:** All layers (Unit, Service Integration, System Integration via Aspire AppHost with emulators/Testcontainers) are run locally by developers and in pre-commit hooks.
*   **CI/CD Pipeline (e.g., GitHub Actions):** Automated execution of Unit, Service Integration, and System Integration tests on every push and pull request.
*   **Staging Environment:** Deployment of the full system for more extensive E2E testing (potentially manual or semi-automated initially) and smoke testing.
*   **Production Environment:** Health checks and monitoring.

## 6. Testing Specific Components & Flows

*   **M365 Agent Logic (`IPersonaRuntime`, `IAgenticStrategyHandler` within `Nucleus.Agent.<PersonaName>`):** Primarily Unit Tests (Layer 1), Service Integration Tests (Layer 2).
*   **MCP Tool Endpoints & Logic (within `Nucleus.McpTool.<ToolName>`):** Primarily Unit Tests (Layer 1 for internal logic), Service Integration Tests (Layer 2 using `WebApplicationFactory`).
*   **Core Shared Logic (`Nucleus.Shared.Kernel`):** Primarily Unit Tests (Layer 1).
*   **Domain Logic (`Nucleus.Domain.RAGLogic`):** Primarily Unit Tests (Layer 1 for algorithms), Service Integration tests (Layer 2 for interactions if any internal components are complex enough to warrant it, though most dependencies would be mocked).
*   **Infrastructure Components (`Nucleus.Infrastructure.*`):** Unit tests for any logic within the provider (e.g., mapping, query construction). Service Integration tests against Testcontainers or emulators for the actual external service (e.g., `Nucleus.Infrastructure.CosmosDb` against a Cosmos DB emulator).
*   **Agent-MCP Tool Interactions (via `Nucleus.Mcp.Client`):** System Integration Tests (Layer 3 via Aspire).
*   **Data Persistence (e.g., `Nucleus.McpTool.KnowledgeStore` with `Nucleus.Infrastructure.CosmosDb`):**
    *   Unit tests for repository logic with in-memory fakes.
    *   Service Integration Tests for the MCP Tool using Testcontainers for Cosmos DB.
    *   System Integration Tests for end-to-end flows involving data persistence.
*   **Asynchronous Processing (`Nucleus.BackgroundWorker.ServiceBus` with `Nucleus.Infrastructure.ServiceBus`):** System Integration Tests (Layer 3) to verify message production and consumption, and resulting side effects.
*   **AI Integration (`Microsoft.Extensions.AI.IChatCompletionService`, etc., consumed by `Nucleus.Domain.RAGLogic` or `Nucleus.McpTool.LlmOrchestration`):
    *   **Unit/Service Integration:** Mock the `IChatCompletionService` or other AI interfaces to verify the logic that consumes them.
    *   **System Integration (Optional/Controlled):** For certain System Integration tests, configure the orchestrated services to use real (sandboxed/test-tier) AI service endpoints. This requires careful management of API keys (e.g., via .NET user secrets for local Aspire runs, and secure configuration in CI/Staging). These tests validate actual AI model compatibility but should be limited to avoid excessive cost and flakiness.

## 7. System Integration Testing Workflow (Layer 3 with .NET Aspire)

The typical workflow for tests using `Aspire.Hosting.Testing`:

1.  **Test Project Setup:** Create a dedicated xUnit test project (e.g., `Nucleus.Tests.SystemIntegration`). Reference `Aspire.Hosting.Testing`.
2.  **Test Fixture / Test Class:**
    *   Use `DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>()` to initialize the Aspire application.
    *   In the builder, you can override configurations, replace services with test doubles if needed for specific scenarios, or add test-specific resources.
    *   Build and start the application: `await using var app = await builder.Build().StartAsync();`.
        *   **Note on Complexity:** For very large systems, consider if testing the *entire* `Nucleus_AppHost` is always necessary for every system integration test. It might be more efficient to create smaller, focused AppHost configurations for specific integration scenarios if tests become too slow or unwieldy. However, the default approach is to test against the main AppHost to ensure genuine integration. The key is to ensure that tests focus on a minimal viable set of interacting services to validate a specific cross-component flow rather than trying to test all possible interactions in one go.
3.  **Interacting with Services:**
    *   Obtain `HttpClient` instances for M365 Agent messaging endpoints or MCP Tool endpoints: `var agentClient = app.CreateHttpClient("nucleus.agent.eduflow");`, `var mcpToolClient = app.CreateHttpClient("nucleus.mcptool.knowledgestore");` (using project names as defined in `Nucleus.AppHost`).
    *   Obtain connection strings or client instances for emulated resources (e.g., Cosmos DB, Service Bus) to prepare state or verify outcomes.
4.  **Executing Test Scenarios:**
    *   Simulate an M365 `Activity` by POSTing to the M365 Agent's messaging endpoint.
    *   Directly call MCP Tool endpoints.
5.  **Verifying Outcomes:**
    *   Assert responses from Agent or MCP Tool endpoints.
    *   Query emulated Cosmos DB to verify data changes.
    *   Inspect messages on emulated Service Bus queues/topics.
    *   Check logs from orchestrated services.
6.  **Teardown:** The `app` instance is disposed of at the end of the test (or test fixture lifetime), shutting down the orchestrated application.

## 8. Future Considerations

*   **Advanced E2E User Interaction Testing (Layer 4):** Mature the automation for testing M365 Persona Agents within platforms like Teams.
*   **Observability in Tests:** Leverage .NET Aspire's integrated logging, tracing, and metrics (accessible via the Aspire Dashboard during test execution) for enhanced diagnostics of test failures.
*   **Architectural Enforcement:** Introduce tests using libraries like **NetArchTest** to programmatically enforce architectural rules and prevent undesirable dependencies.
*   **Performance Testing:** Develop strategies for measuring API response times and processing throughput under load, potentially leveraging the Layer 3 setup.
*   **Security Testing:** Incorporate penetration testing and vulnerability scanning into the development lifecycle.
*   **Chaos Testing:** Explore injecting failures into the AppHost-managed resources to test system resilience.

## 9. Mermaid Diagram: Testing Layers

```mermaid
graph TD
    subgraph User Interaction (M365 Platform)
        UI[M365 Platform UI - Teams, Outlook, etc.]
    end

    subgraph Nucleus System
        subgraph M365 Persona Agent App (e.g., Nucleus.Agent.EduFlow)
            A_EP[Agent Endpoint]
            A_PR[IPersonaRuntime]
            A_SH[IAgenticStrategyHandler]
            A_MCP_Client[Nucleus.Mcp.Client (Interface)]
            A_AI_Client[AI Client (IChatCompletionService)]
        end

        subgraph MCP Tool/Server Apps (e.g., Nucleus.McpTool.KnowledgeStore)
            MCP_EP[MCP Tool Endpoint]
            MCP_Logic[Tool Business Logic]
            MCP_Repo[Repository/Data Access]
            MCP_AI_Client_Tool[AI Client (IChatCompletionService in LlmOrchestration)]
        end

        subgraph Core Libraries
            SharedKernel[Nucleus.Shared.Kernel]
            DomainRAG[Nucleus.Domain.RAGLogic]
        end

        subgraph Infrastructure Libraries (e.g., Nucleus.Infrastructure.CosmosDb)
            InfraCosmos[Nucleus.Infrastructure.CosmosDb]
            InfraServiceBus[Nucleus.Infrastructure.ServiceBus]
            InfraAi[Nucleus.Infrastructure.Ai.Google]
        end

        subgraph Shared Resources (Emulated/Testcontainer via Aspire)
            DB[Cosmos DB Emulator/Testcontainer]
            SB[Service Bus Emulator/Testcontainer]
        end
    end

    subgraph Testing Layers
        L4[Layer 4: E2E User Interaction Tests]
        L3[Layer 3: System Integration Tests (.NET Aspire)]
        L2[Layer 2: Service Integration Tests]
        L1[Layer 1: Unit Tests]
    end

    UI -- User Action --> A_EP
    A_EP --> A_PR
    A_PR --> A_SH
    A_PR --> A_MCP_Client
    A_PR --> A_AI_Client
    A_MCP_Client -- HTTP --> MCP_EP
    MCP_EP --> MCP_Logic
    MCP_Logic --> MCP_Repo
    MCP_Logic --> DomainRAG
    MCP_Logic --> MCP_AI_Client_Tool % If tool directly uses AI, e.g. LlmOrchestration
    MCP_Repo -- CRUD --> DB
    DomainRAG --> SharedKernel
    A_SH --> SharedKernel
    MCP_Logic --> SharedKernel
    MCP_Repo --> SharedKernel
    A_MCP_Client --> SharedKernel % MCP Client uses shared models
    InfraCosmos -.-> SharedKernel
    InfraServiceBus -.-> SharedKernel
    InfraAi -.-> SharedKernel

    A_AI_Client --> InfraAi % Agent uses AI infra for its own needs
    MCP_AI_Client_Tool --> InfraAi % MCP Tool uses AI infra
    MCP_Repo --> InfraCosmos % MCP Repo uses Cosmos infra

    L4 -.-> UI
    L3 -.-> A_EP
    L3 -.-> MCP_EP
    L3 -.-> DB
    L3 -.-> SB

    L2 -.-> A_PR
    L2 -.-> A_MCP_Client
    L2 -.-> MCP_Logic
    L2 -.-> MCP_Repo
    L2 -.-> A_AI_Client
    L2 -.-> MCP_AI_Client_Tool
    L2 -.-> DomainRAG
    L2 -.-> InfraCosmos

    L1 -.-> A_SH
    L1 -.-> MCP_Logic
    L1 -.-> MCP_Repo
    L1 -.-> DomainRAG
    L1 -.-> SharedKernel

    classDef testLayer fill:#D6EAF8,stroke:#2E86C1,stroke-width:2px;
    class L1,L2,L3,L4 testLayer;
```

This comprehensive testing strategy, centered around TDD and leveraging .NET Aspire for distributed system testing, will ensure the development of a high-quality, reliable, and maintainable Nucleus platform.
