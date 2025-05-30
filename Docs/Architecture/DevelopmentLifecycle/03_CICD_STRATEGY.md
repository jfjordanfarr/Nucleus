---
title: "Architecture - CI/CD Strategy"
description: "Outlines the CI/CD strategy for the Nucleus open-source project, focusing on building and packaging M365 Persona Agents and backend MCP Tool/Server applications as part of the development lifecycle."
version: 3.2
date: 2025-05-29
parent: ./00_DEVELOPMENT_LIFECYCLE_OVERVIEW.md
see_also:
    - title: "Development Lifecycle Overview"
      path: ./00_DEVELOPMENT_LIFECYCLE_OVERVIEW.md
    - title: "Testing Strategy"
      path: ./02_TESTING_STRATEGY.md
    - title: "Comprehensive System Architecture"
      path: ../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md
    - title: "Foundations: MCP & M365 Agents SDK Primer"
      path: ../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md
    - title: "Azure Deployment Strategy"
      path: ../Deployment/Hosting/ARCHITECTURE_HOSTING_AZURE.md
---

# Nucleus: CI/CD Strategy

## 1. Introduction

This document details the Continuous Integration (CI) and Continuous Delivery (CD) strategy for the Nucleus project, reflecting its distributed architecture comprising **Nucleus M365 Persona Agent applications** and backend **Nucleus MCP Tool/Server applications**. This document is part of the overall [Development Lifecycle Overview](./00_DEVELOPMENT_LIFECYCLE_OVERVIEW.md).

As an open-source project, the focus of CI/CD is on:
*   **Validating Code Quality:** Ensuring all components build, pass tests (unit, integration, and system-level via .NET Aspire orchestration), and adhere to standards.
*   **Creating Consumable Artifacts:**
    *   Building versioned **Docker images for each distinct Nucleus M365 Persona Agent application**.
    *   Building versioned **Docker images for each distinct backend Nucleus MCP Tool/Server application**.
    *   Providing example deployment configurations (e.g., .NET Aspire AppHost manifests, `docker-compose.yml` files for local multi-container orchestration, example Bicep/Terraform snippets for Azure) that users can adapt for their own environments. Refer to the [Azure Deployment Strategy](../../Deployment/Hosting/ARCHITECTURE_HOSTING_AZURE.md) and [Self-Hosted Strategy](../../Deployment/Hosting/ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md) for deployment details.
*   **Automating Releases:** Streamlining the process of creating tagged GitHub releases with associated Docker image references and deployment examples.
*   **Maintaining Security:** Ensuring the CI/CD process itself is secure.

## 2. Platform and Tooling

*   **CI/CD Platform:** **GitHub Actions** - Chosen for its tight GitHub integration and generous free tier.
    *   **Workflow Definitions:** Reside in [`.github/workflows/`](../../../.github/workflows/).
        *   Pull Request Validation: [`pr-validation.yml`](../../../.github/workflows/pr-validation.yml)
        *   Release Process: [`release.yml`](../../../.github/workflows/release.yml)
*   **Container Registry:** **GitHub Container Registry (ghcr.io)** or **Docker Hub** - For hosting publicly accessible, versioned Docker images for *each* Nucleus M365 Agent and *each* backend Nucleus MCP Tool/Server (e.g., `ghcr.io/jfjordanfarr/nucleus-agent-eduflow:vX.Y.Z`, `ghcr.io/jfjordanfarr/nucleus-mcptool-knowledgestore:vX.Y.Z`).
*   **Code Scanning/SAST (Optional):** GitHub Advanced Security (CodeQL), SonarCloud.
*   **Container Scanning (Optional):** Trivy, Snyk, Dependabot (applied to all built images).

## 3. Workflow Triggers and Branching Strategy

CI/CD workflows are primarily triggered by actions related to two key branches:

*   **`develop` Branch:** This is the main integration branch. All feature branches are merged into `develop` via Pull Requests.
    *   **Pull Requests targeting `develop`:** The [`pr-validation.yml`](../../../.github/workflows/pr-validation.yml) workflow is triggered. It runs build, test (unit and conditionally integration), and linting stages to ensure code quality before merging.
    *   **Pushes to `develop` (post-merge):** The [`pr-validation.yml`](../../../.github/workflows/pr-validation.yml) workflow also runs on direct pushes to `develop` to confirm the integrity of the branch after merges.
*   **`main` Branch:** This branch represents stable releases.
    *   **Git Tags (e.g., `vX.Y.Z`) on `main`:** When a version tag is pushed to `main`, the [`release.yml`](../../../.github/workflows/release.yml) workflow is triggered. This workflow handles building versioned Docker images, pushing them to the public registry, and creating a GitHub Release with associated artifacts.

## 4. Key CI/CD Stages

### 4.1. Pull Request Validation Workflow (`pr-validation.yml`)

This workflow, defined in [`pr-validation.yml`](../../../.github/workflows/pr-validation.yml), typically includes:

1.  **Checkout Code:** Get the latest source code from the PR branch.
2.  **Setup .NET (targeting .NET 8.0 as per Microsoft 365 Agents SDK requirements).**
3.  **Restore Dependencies** for the entire solution.
4.  **Build All Projects:** Run `dotnet build` for all M365 Agent application projects, all MCP Tool/Server application projects, and all shared library projects.
5.  **Linting/Formatting Check (Optional):** Run tools like `dotnet format --verify-no-changes`.
6.  **Unit Tests:** Run `dotnet test --filter "Category!=Integration&Category!=SystemIntegration"` for all relevant unit test projects (`Nucleus.Agents.UnitTests`, `Nucleus.McpTools.UnitTests`, `Nucleus.Domain.Tests`).
7.  **Service Integration Tests (Conditional):** Run `dotnet test --filter "Category=Integration"` for integration tests of individual M365 Agents (mocking MCP calls) and individual MCP Tools (with mocked or Testcontainer-managed dependencies).
8.  **System Integration Tests (Conditional - using .NET Aspire `Aspire.Hosting.Testing`):** Run `dotnet test --filter "Category=SystemIntegration"` from the `Nucleus.System.IntegrationTests` project. This will orchestrate a subset of M365 Agents, MCP Tools, and emulated services (Cosmos DB, Service Bus) via the `Nucleus.AppHost` to test key end-to-end flows. Requires secrets for any real external service calls (e.g., LLMs).
9.  **Static Analysis (Optional):** Run CodeQL or other SAST tools.

### 4.2. Release Workflow (`release.yml`)

This workflow, defined in [`release.yml`](../../../.github/workflows/release.yml) and triggered by Git tags on `main` (matching `v*.*.*`), includes stages similar to the PR validation, plus release-specific steps:

1.  **Checkout Code:** Get the code corresponding to the Git tag.
2.  **Setup .NET (targeting .NET 8.0 for release builds).**
3.  **Restore Dependencies.**
4.  **Build Solution (Release Configuration).**
5.  **Run All Tests:** Execute all unit, service integration, and system integration tests (`dotnet test --configuration Release`). Upload test results.
6.  **Log in to Container Registry (e.g., GHCR).**
7.  **For EACH Nucleus M365 Persona Agent application project (e.g., `Nucleus.Agent.EduFlow`, `Nucleus.Agent.Helpdesk`):**
    *   **Extract Docker Metadata:** Use `docker/metadata-action` to generate tags based on the Git tag and the specific agent name (e.g., `eduflow-agent:vX.Y.Z`, `eduflow-agent:X.Y`, `eduflow-agent:latest`).
    *   **Build and Push Docker Image:** Build the Docker image for this specific M365 Agent (e.g., `Nucleus.Agent.EduFlow`) and push it with all generated tags, following the pattern `ghcr.io/jfjordanfarr/nucleus-agent-<agentname>:<tag>` (e.g., `ghcr.io/jfjordanfarr/nucleus-agent-eduflow:vX.Y.Z`).
    *   **(Future) Container Vulnerability Scan.**
8.  **For EACH backend Nucleus MCP Tool/Server application project (e.g., `Nucleus.McpTools.KnowledgeStore`, `Nucleus.McpTools.FileAccess`):**
    *   **Extract Docker Metadata:** Generate tags specific to this MCP Tool (e.g., `knowledgestore-mcptool:vX.Y.Z`).
    *   **Build and Push Docker Image:** Build and push the Docker image for this MCP Tool (e.g., `Nucleus.McpTools.KnowledgeStore`) and push it with all generated tags, following the pattern `ghcr.io/jfjordanfarr/nucleus-mcptool-<toolname>:<tag>` (e.g., `ghcr.io/jfjordanfarr/nucleus-mcptool-knowledgestore:vX.Y.Z`).
    *   **(Future) Container Vulnerability Scan.**
9.  **Package Release Artifacts (Revised):** Creates a `nucleus-vX.Y.Z-deployment-examples.zip` file containing:
    *   An updated `docker-compose.yml` file suitable for local multi-container orchestration, intended to be used with the **`Nucleus.AppHost` project from .NET Aspire. This `docker-compose.yml` would typically be configured by the AppHost (or serve as an example for it) to launch the pre-built, versioned Nucleus M365 Agent and MCP Tool images from the container registry** for end-user local deployment testing or exploration.
    *   Example Bicep/Terraform snippets for deploying the distributed system (M365 Agents, MCP Tools, Azure Cosmos DB, Service Bus) to Azure.
    *   `README.md` (from the root of the repository).
10. **Generate Release Notes** (Ideally automated via Conventional Commits, see Section 5.2).
11. **Create GitHub Release:** Upload `nucleus-vX.Y.Z-deployment-examples.zip` and potentially standalone example IaC files. The release notes should clearly list all Docker images produced for this version and their GHCR paths.

## 5. Versioning Strategy

*   **Semantic Versioning (SemVer 2.0.0):** Adhere to `MAJOR.MINOR.PATCH` versioning (e.g., `v0.1.0`, `v1.0.0`, `v1.0.1-alpha.1`).
*   **Source of Truth for Version:** The primary source of truth for a release version is the **Git tag** (e.g., `v0.1.0`) applied to a commit on the `main` branch. This tag triggers the release workflow. All components (M365 Agents, MCP Tools, shared libraries) in a release will share this version number for simplicity, signifying a cohesive release of the Nucleus platform.
*   **Assembly Versioning (`Directory.Build.props`):**
    *   A `Directory.Build.props` file at the root of the solution sets a common `<VersionPrefix>` (e.g., `0.1.0`) for all projects. This provides a baseline version for local builds and development.
    *   For NuGet packages (if any are produced in the future), the version from the Git tag would be used to pack them. The `Directory.Build.props` file also specifies metadata like `<PackageIcon>icon.png</PackageIcon>` and `<PackageReadmeFile>README.md</PackageReadmeFile>`. Assuming `Directory.Build.props`, `icon.png` (e.g., a 128x128 PNG), and `README.md` are all located at the solution root, these paths will resolve correctly for SDK-style projects during packaging.
*   **Docker Image Tags:** Each Docker image (for each M365 Agent and each MCP Tool) will be tagged with:
    *   The full corresponding SemVer tag (e.g., `ghcr.io/jfjordanfarr/nucleus-agent-eduflow:v1.0.0`).
    *   Major.minor (e.g., `ghcr.io/jfjordanfarr/nucleus-agent-eduflow:1.0`).
    *   `latest` for the most recent non-prerelease of that specific component.
*   **Epoch Versioning:** While considered, standard SemVer 2.0.0 is adopted for simplicity and broad tooling compatibility, especially for AI agent understanding and conventional open-source practices. Complex versioning schemes can be harder to manage and automate reliably.

### 5.1. How to Make a Release (Manual Steps for Maintainers)

1.  **Ensure `develop` is Stable:** All desired features and fixes for the release should be merged into the `develop` branch and validated by the `pr-validation.yml` workflow.
2.  **Merge `develop` into `main`:**
    *   Checkout the `main` branch: `git checkout main`
    *   Pull the latest changes: `git pull origin main`
    *   Merge `develop`: `git merge develop`
    *   Push the updated `main` branch: `git push origin main`
3.  **Create and Push a Version Tag:**
    *   Decide on the new version number following SemVer (e.g., `v0.1.0`, `v0.2.0`, `v1.0.0-alpha.1`).
    *   Create the tag: `git tag vX.Y.Z` (e.g., `git tag v0.1.0`)
    *   Push the tag to the remote repository: `git push origin vX.Y.Z` (e.g., `git push origin v0.1.0`).
4.  **Monitor Release Workflow:** The push of the new tag will trigger the `release.yml` workflow in GitHub Actions. Monitor its progress to ensure all steps complete successfully.
5.  **Verify Release:** Once the workflow completes, check the GitHub Releases page for the new release and associated artifacts. Verify the Docker image is available on GHCR with the correct tags.

### 5.2. Changelog and Release Notes Strategy

Currently, the `release.yml` workflow generates basic placeholder release notes. For more robust and informative changelogs, the following strategy is **strongly recommended** for future implementation:

1.  **Adopt Conventional Commits:** All commit messages should adhere to the [Conventional Commits specification](https://www.conventionalcommits.org/). This practice involves prefixing commit messages with types like `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`, `build:`, `ci:`, etc. This structure makes commit history more readable and allows for automated changelog generation.
    *   This is beneficial for human understanding and for AI agents that may analyze or contribute to the codebase.
    *   *Example Commit Message:* `feat(api): Add endpoint for user preferences`

2.  **Automated Release Note Generation (Future Implementation):**
    *   **Tooling:** Consider using a GitHub Action like **`release-drafter/release-drafter`** or a CLI tool such as `conventional-changelog-cli` or `standard-version`.
    *   **CLI Tools:** Tools like `conventional-changelog-cli` can be used to generate or update a `CHANGELOG.md` file in the repository. This can be run manually before a release or integrated as a step in the `release.yml` workflow to update `CHANGELOG.md` and then use that file for the release notes.

3.  **Initial Approach (Current):** Until automated tooling is implemented, release notes are basic placeholders generated by the `release.yml` workflow. Maintainers can manually edit these notes on the GitHub Release page after creation if more detail is desired for a specific release.

Adopting Conventional Commits is the most impactful first step, as it lays the groundwork for any automated changelog tool. `release-drafter` is a popular and effective choice for GitHub-centric workflows.

## 6. Security Considerations

*   **Secrets Management:** **NEVER** commit secrets (API keys, registry passwords) directly into the repository.
    *   **GitHub Environment Secrets:** For secrets required by workflows, such as the `GOOGLE_AI_API_KEY_FOR_TESTS` used by integration tests, **GitHub Environments** are utilized. A specific environment (e.g., `ci_tests`) is configured in the repository settings with the necessary encrypted secrets. Workflows jobs that need these secrets specify the `environment` property to gain access.
*   **Permissions:** Configure GitHub Actions workflows with the minimum necessary permissions. Use short-lived tokens where possible.
*   **Dependency Scanning:** Utilize Dependabot or similar tools to monitor dependencies (.NET packages, base Docker images) for known vulnerabilities.
*   **Log Sanitization:** Ensure no sensitive information is accidentally printed to workflow logs.
*   **Third-Party Actions:** Be cautious when using third-party GitHub Actions. Prefer official actions (e.g., `actions/checkout`, `actions/setup-dotnet`) or actions from reputable sources. Pin actions to specific commit SHAs for security and stability.

## 7. Consumable Artifacts

The primary outputs of the CD process for consumers will be:

*   **Versioned Docker Images for each Nucleus M365 Persona Agent:** Available on GHCR (e.g., `ghcr.io/jfjordanfarr/nucleus-agent-eduflow`, `ghcr.io/jfjordanfarr/nucleus-agent-helpdesk`).
*   **Versioned Docker Images for each backend Nucleus MCP Tool/Server:** Available on GHCR (e.g., `ghcr.io/jfjordanfarr/nucleus-mcptool-knowledgestore`, `ghcr.io/jfjordanfarr/nucleus-mcptool-fileaccess`).
*   **GitHub Releases:** Each release will have:
    *   A `nucleus-vX.Y.Z-deployment-examples.zip` file containing:
        *   An updated `docker-compose.yml` file suitable for local multi-container orchestration, intended to be used with the **`Nucleus.AppHost` project from .NET Aspire. This `docker-compose.yml` would typically be configured by the AppHost (or serve as an example for it) to launch the pre-built, versioned Nucleus M365 Agent and MCP Tool images from the container registry** for end-user local deployment testing or exploration.
        *   Example IaC snippets (Bicep/Terraform) for Azure deployment.
        *   Overall `README.md`.
    *   Release notes detailing changes and listing all produced Docker images and their tags.

This CI/CD strategy, as part of the overall development lifecycle, aims to provide confidence in the quality of each component and make it easier for users to deploy and utilize the full Nucleus platform or selected parts of it.
