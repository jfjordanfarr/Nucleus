---
title: Namespace - Nucleus.Infrastructure.Messaging
description: Describes the infrastructure project responsible for messaging components, including in-memory queues and future message broker integrations.
version: 1.0
date: 2025-05-12
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Messaging

**Relative Path:** `src/Nucleus.Infrastructure/Messaging/Nucleus.Infrastructure.Messaging.csproj`

## 1. Purpose

This project provides infrastructure components related to message queuing and asynchronous task processing. It is designed to host implementations of messaging abstractions defined in `Nucleus.Abstractions`.

Initially, it will house an `InMemoryBackgroundTaskQueue` for local development and testing. In the future, it can be expanded to include client implementations or wrappers for external message brokers like Azure Service Bus, RabbitMQ, or Cloudflare Queues, ensuring that the core application logic remains decoupled from specific messaging technologies.

## 2. Key Components

*   **`InMemoryBackgroundTaskQueue.cs`**: An in-memory implementation of `IBackgroundTaskQueue` for queuing `NucleusIngestionRequest` objects. This allows for asynchronous processing in development environments without external dependencies.
*   **(Future) Broker-Specific Implementations:** Potential future classes that implement `IBackgroundTaskQueue` or other messaging interfaces for specific message brokers.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References interfaces like `IBackgroundTaskQueue` and shared models like `NucleusIngestionRequest`, `DequeuedMessage<T>`).
*   `Microsoft.Extensions.Logging.Abstractions` (For logging within queue implementations).

## 4. Dependents

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (Likely to register and inject `IBackgroundTaskQueue` implementations via DI).
*   `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj` (May reference this project if it needs to configure or directly manage messaging resources, or for wiring up DI).

## 5. Related Documents

*   [../11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [`IBackgroundTaskQueue` Interface](../../../src/Nucleus.Abstractions/Orchestration/IBackgroundTaskQueue.cs)
*   (Consider linking to a future high-level messaging architecture document if created)
