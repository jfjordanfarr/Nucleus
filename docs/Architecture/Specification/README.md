---
title: "Specification Stratum Overview"
description: "Detailed specifications and implementation guidance that define how the Nucleus system works."
version: 1.0
date: 2025-12-30
---

# Specification Stratum

This stratum contains the detailed specifications for the Nucleus platform - the concepts and implementation guides that define how the system works.

## Concepts/
Architectural concepts, patterns, and designs (compositions).

- [M365AgentsOverview.MDMD.md](Concepts/M365AgentsOverview.MDMD.md) - M365 Agent architectural concepts
- [McpToolsOverview.MDMD.md](Concepts/McpToolsOverview.MDMD.md) - MCP Tool architectural concepts
- [DeploymentOverview.MDMD.md](Concepts/DeploymentOverview.MDMD.md) - Deployment concepts and patterns
- [DevelopmentLifecycleOverview.MDMD.md](Concepts/DevelopmentLifecycleOverview.MDMD.md) - Development process concepts
- [ProcessingIngestion.MDMD.md](Concepts/ProcessingIngestion.MDMD.md) - Content processing architectural concepts
- [PersonaDesignsOverview.MDMD.md](Concepts/PersonaDesignsOverview.MDMD.md) - Persona design patterns

## Implementations/
Specific implementation guides and tool documentation (units with 1:1 mappings).

### MCP Tool Implementations
- [McpToolFileAccess.MDMD.md](Implementations/McpToolFileAccess.MDMD.md)
- [McpToolKnowledgeStore.MDMD.md](Implementations/McpToolKnowledgeStore.MDMD.md)
- [McpToolContentProcessing.MDMD.md](Implementations/McpToolContentProcessing.MDMD.md)
- [McpToolRAGPipeline.MDMD.md](Implementations/McpToolRAGPipeline.MDMD.md)
- [McpToolLlmOrchestration.MDMD.md](Implementations/McpToolLlmOrchestration.MDMD.md)
- [McpToolPersonaBehaviourConfig.MDMD.md](Implementations/McpToolPersonaBehaviourConfig.MDMD.md)

### Persona Implementations  
- [PersonaEducatorDesign.MDMD.md](Implementations/PersonaEducatorDesign.MDMD.md)
- [PersonaProfessionalDesign.MDMD.md](Implementations/PersonaProfessionalDesign.MDMD.md)
- [PersonaBootstrapperDesign.MDMD.md](Implementations/PersonaBootstrapperDesign.MDMD.md)

### Development & Infrastructure
- [NamespacesFoldersStructure.MDMD.md](Implementations/NamespacesFoldersStructure.MDMD.md)
- [TestingStrategy.MDMD.md](Implementations/TestingStrategy.MDMD.md)
- [CICDStrategy.MDMD.md](Implementations/CICDStrategy.MDMD.md)
- [AbstractionsDTOsInterfaces.MDMD.md](Implementations/AbstractionsDTOsInterfaces.MDMD.md)