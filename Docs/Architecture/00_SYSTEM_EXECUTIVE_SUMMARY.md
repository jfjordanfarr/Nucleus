---
title: "Nucleus System Architecture Overview (Post-M365 Agent SDK Pivot)"
description: "A high-level overview of the distributed Nucleus platform architecture, components, and deployment models, now centered on Nucleus M365 Persona Agents interacting with backend Nucleus MCP Tool/Server applications."
version: 2.1
date: 2025-05-26
---

# Nucleus: System Architecture Overview (Executive Summary)

## 1. Vision & Core Architectural Model

Nucleus is a platform for specialized AI assistants, embodied as **Nucleus M365 Persona Agent applications** built with the Microsoft 365 Agents SDK. These agents interact with users on Microsoft 365 platforms and leverage backend **Nucleus MCP Tool/Server applications** for core functionalities like data persistence, ephemeral file access, and advanced RAG.

This architecture prioritizes:
*   **M365 Integration:** Seamless user experience within existing workflows.
*   **Distributed Capabilities:** Modular M365 Agents and backend MCP Tools for clear separation of concerns and scalability.
*   **Zero Trust for User File Content:** Nucleus backend services never store raw user files; access is ephemeral and permission-bound.
*   **AI Provider Flexibility:** Configurable support for Azure OpenAI, Google Gemini, OpenRouter.AI, etc.

## 2. Key Architectural Pillars

The Nucleus architecture is built upon several key pillars, each detailed in dedicated documentation:

*   **Foundational Technologies:** Understanding the **Model Context Protocol (MCP)** and the **Microsoft 365 Agents SDK** is crucial.
    *   *For details, see: `./CoreNucleus/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md`*
*   **M365 Persona Agents:** These applications are the primary interface for user interaction and orchestrate backend capabilities.
    *   *For an overview, see: `./Agents/01_M365_AGENTS_OVERVIEW.md`*
*   **Backend MCP Tool/Servers:** These services provide the core data access, processing, and RAG logic for Nucleus.
    *   *For an overview, see: `./McpTools/01_MCP_TOOLS_OVERVIEW.md`*
*   **Core Nucleus Concepts:** This includes persona definition, data persistence strategies, ephemeral processing pipelines, AI integration, and shared abstractions.
    *   *Primary reference: Documents within `./CoreNucleus/`*
*   **Deployment, Development Lifecycle, and Security:** Strategies for deploying, testing, and securing the distributed Nucleus system.
    *   *Primary references: Documents within `./Deployment/`, `./DevelopmentLifecycle/`, and `./Security/`*

## 3. Comprehensive Architectural Guide

For a full, in-depth understanding of all components, their interactions, design principles, and advanced implementation details, please refer to the main architectural document:

*   **[Nucleus System Architecture: Comprehensive Guide](./00_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md)**

This overview serves as an entry point to the broader set of architectural documents.