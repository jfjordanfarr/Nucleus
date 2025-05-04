---
title: "Story 04: Taming the Aspire Beast - The Integration Testing Saga"
description: "Chronicles the journey of implementing and debugging .NET Aspire integration tests using Aspire.Hosting.Testing, overcoming emulator issues, DI complexities, and configuration challenges."
version: 1.0
date: 2025-05-02
---

## The Goal: True Integration Testing

As defined in our [Testing Architecture (`09_ARCHITECTURE_TESTING.md`)](../Architecture/09_ARCHITECTURE_TESTING.md), the goal was clear: implement robust Layer 3 System Integration tests for `Nucleus.Services.Api` using the official `Aspire.Hosting.Testing` library. This approach promised automated orchestration of the API service and its dependencies (Cosmos DB emulator, Service Bus emulator) within the test execution context, providing a high-fidelity testing environment.

## The Initial Hurdles: Duplicate Resources and Hanging Tests

Our first attempts were met with immediate obstacles:

1.  **Duplicate Resource Error:** An `Aspire.Hosting.DistributedApplicationException` complaining about a duplicate `servicebus-sqledge` container resource.
    *   **Diagnosis:** We realized that explicitly adding the `servicebus-sqledge` container via `builder.AddContainer(...)` in `Aspire/Nucleus.AppHost/Program.cs` conflicted with the container implicitly managed by `builder.AddAzureServiceBus(...).RunAsEmulator(...)`.
    *   **Fix:** We removed the explicit `AddContainer` call and configured the emulator settings (like bind mounts and environment variables) directly within the `RunAsEmulator` lambda.

2.  **The Infinite Wait:** After fixing the duplicate resource error, the tests entered a new frustrating phase: hanging indefinitely after logging "Waiting for resources to be running...". The test runner never proceeded to the actual test logic.
    *   **Diagnosis:** This pointed towards one or more resources managed by the `DistributedApplicationTestingBuilder` failing to reach the `KnownResourceStates.Running` state. Deep dives into the logs (facilitated by setting `ASPNETCORE_LOGGING__CONSOLE__FORMATTERNAME=Json` via `WithEnvironment` on the emulators) revealed the culprit: the **Service Bus emulator (backed by SQL Edge)** was experiencing internal startup failures. Errors like "Failed to load /var/opt/mssql/mssql.conf" and "dial tcp [::1]:1431: getsockopt: connection refused" indicated problems within the SQL Edge container itself, likely related to the `mssql.conf` bind mount or internal process communication.
    *   **Troubleshooting:**
        *   Verified the `mssql.conf` file content and path relative to the AppHost project.
        *   Simplified the `RunAsEmulator` configuration, temporarily removing bind mounts and environment variables to isolate the issue.
        *   Ensured Docker had sufficient resources.
        *   Added explicit `await _app.ResourceNotifications.WaitForResourceAsync(...)` calls with generous timeouts in `ApiIntegrationTests.InitializeAsync` to make failures explicit rather than silent hangs.

## Navigating DI and Configuration in the Test Host

While debugging the emulator hangs, we also uncovered and addressed several Dependency Injection (DI) and configuration complexities inherent in the `Aspire.Hosting.Testing` model:

*   **`HttpClient` Creation:** Initially, we tried reusing the `HttpClient` from the test host's `WebApplicationFactory` pattern, which doesn't apply directly here. We needed to create a new `HttpClient` pointing to the endpoint exposed by the Aspire-managed API service (`_app.GetEndpoint("nucleusapi")`).
*   **Service Resolution Scope:** A critical error involved resolving application-scoped services (like `IArtifactMetadataRepository`) directly from the test application's root service provider (`_app.Services`) instead of obtaining the API project's specific `IServiceProvider`.
    *   **Fix:** We retrieved the API's specific service provider using `var apiProjectResource = _app.Services.GetRequiredService<Projects.Nucleus_Services_Api>(); _apiServiceProvider = apiProjectResource.ServiceProvider;`. This enabled correct "white-box" testing by allowing tests to resolve services *exactly* as the running API service would.
*   **Configuration Injection:** Ensuring the correct configuration (especially the `CosmosDb__DatabaseName` for the emulator) was available to the API service when launched by the test host required setting environment variables appropriately in the `Nucleus.AppHost/Program.cs` based on an `isTestEnvironment` flag.

## Achieving Stability and White-Box Testing

Through iterative debugging, log analysis, and careful adjustments to both the `Nucleus.AppHost/Program.cs` resource definitions and the `ApiIntegrationTests.cs` setup/teardown logic (`IAsyncLifetime`), we resolved the emulator startup issues, corrected the DI misconfigurations, and ensured proper configuration flow.

**Key Successes:**

*   The `BasicHealthCheck_ShouldReturnOk` test now passes reliably.
*   The build produces zero warnings after cleaning up unused fields identified during the process.
*   We established a working pattern for "white-box" integration testing, allowing tests to directly resolve and interact with services inside the running API application via its `IServiceProvider`.

This journey highlighted the power of `Aspire.Hosting.Testing` but also underscored the debugging complexities involved, particularly when dealing with nested emulator configurations and the nuances of DI within the test host environment. Careful log analysis and methodical isolation of configuration issues were paramount to success.
