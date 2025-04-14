---
title: Architecture - CI/CD Strategy for Open Source
description: Outlines the Continuous Integration and Continuous Delivery/Deployment strategy tailored for the Nucleus OmniRAG open-source project.
version: 1.1
date: 2025-04-13
---

# Nucleus OmniRAG: CI/CD Strategy (Open Source Context)

**Version:** 1.1
**Date:** 2025-04-13

## 1. Introduction

This document details the Continuous Integration (CI) and Continuous Delivery (CD) strategy for the Nucleus OmniRAG project. As an open-source project, the focus of CI/CD is less on deploying to a specific production environment and more on:

*   **Validating Code Quality:** Ensuring code builds, passes tests, and adheres to standards.
*   **Creating Consumable Artifacts:** Building versioned Docker images and potentially example deployment configurations (like `docker-compose.yml` files) that users can easily deploy in their own environments ([Azure](./Hosting/ARCHITECTURE_HOSTING_AZURE.md), [Cloudflare](./Hosting/ARCHITECTURE_HOSTING_CLOUDFLARE.md), [Self-Hosted](./Hosting/ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md)).
*   **Automating Releases:** Streamlining the process of creating tagged releases with associated artifacts.
*   **Maintaining Security:** Ensuring the CI/CD process itself is secure and does not expose credentials or sensitive information.

## 2. Platform and Tooling

*   **CI/CD Platform:** **GitHub Actions** - Chosen due to its tight integration with GitHub repositories, generous free tier for public repositories, and extensive marketplace of actions.
*   **Container Registry:** **GitHub Container Registry (ghcr.io)** or **Docker Hub** - For hosting the publicly accessible, versioned Docker images of Nucleus components (`Nucleus.Api`, workers, etc.).
*   **Code Scanning/SAST (Optional):** GitHub Advanced Security (CodeQL), SonarCloud - For identifying potential security vulnerabilities and code quality issues.
*   **Container Scanning (Optional):** Trivy, Snyk, Dependabot - For scanning Docker images for known vulnerabilities in base images or dependencies.

## 3. Workflow Triggers

CI/CD workflows will be triggered by:

1.  **Pull Requests:** On pushes to branches associated with open pull requests targeting `main` (or other protected branches). Runs build, test, and linting stages.
2.  **Pushes to `main`:** After a PR is merged. Runs build, test, linting, and potentially builds a `-dev` or `-latest` tagged Docker image.
3.  **Git Tags (e.g., `vX.Y.Z`):** When a version tag is pushed. Runs build, test, linting, builds versioned Docker images (`X.Y.Z`), pushes images to the public registry, and creates a GitHub Release with release notes and potentially packaged example deployment files.

## 4. Key CI/CD Stages

A typical workflow (especially for PRs and `main` branch pushes) would include:

1.  **Checkout Code:** Get the latest source code.
2.  **Setup .NET:** Install the required .NET SDK version.
3.  **Restore Dependencies:** Run `dotnet restore`.
4.  **Build:** Run `dotnet build` for all relevant projects.
5.  **Linting/Formatting Check (Optional):** Run tools like `dotnet format --verify-no-changes`.
6.  **Unit Tests:** Run `dotnet test` for unit test projects.
7.  **Integration Tests (Conditional/Optional):** Run integration tests. This might require spinning up temporary services (e.g., using `docker-compose` within the action or service containers) for dependencies like databases or queues. Requires careful design to run efficiently and reliably in a CI environment.
8.  **Static Analysis (Optional):** Run CodeQL or other SAST tools.

**Release Workflow (Triggered by Git Tag):**

Includes all stages above, plus:

9.  **Build Docker Images:** Build Docker images for `Nucleus.Api`, workers, etc., using the Git tag as the image tag (e.g., `ghcr.io/your-org/nucleus-api:v1.0.0`).
10. **Log in to Container Registry:** Authenticate using a short-lived GitHub token or a dedicated secret.
11. **Push Docker Images:** Push the tagged images to the public registry.
12. **Container Vulnerability Scan (Optional):** Run Trivy/Snyk against the built images.
13. **Package Release Artifacts (Optional):** Create a zip file containing example `docker-compose.yml`, Helm charts, or configuration templates corresponding to the release version.
14. **Create GitHub Release:** Automatically create a GitHub Release associated with the Git tag, uploading any packaged artifacts and potentially auto-generating release notes.

## 5. Versioning Strategy

*   **Semantic Versioning (SemVer 2.0.0):** Adhere to `MAJOR.MINOR.PATCH` versioning.
*   **Tags Drive Releases:** Official releases correspond directly to Git tags (e.g., `v1.0.0`, `v1.1.0`).
*   **Docker Image Tags:** Docker images will be tagged with the corresponding SemVer tag (e.g., `v1.0.0`) and potentially a floating tag like `latest` or `stable` (use floating tags with caution).

## 6. Security Considerations

*   **Secrets Management:** **NEVER** commit secrets (API keys, registry passwords) directly into the repository. Use **GitHub Encrypted Secrets** for storing credentials needed by workflows (e.g., container registry login).
*   **Permissions:** Configure GitHub Actions workflows with the minimum necessary permissions. Use short-lived tokens where possible.
*   **Dependency Scanning:** Utilize Dependabot or similar tools to monitor dependencies (.NET packages, base Docker images) for known vulnerabilities.
*   **Log Sanitization:** Ensure no sensitive information is accidentally printed to workflow logs.
*   **Third-Party Actions:** Be cautious when using third-party GitHub Actions. Prefer official actions (e.g., `actions/checkout`, `actions/setup-dotnet`) or actions from reputable sources. Pin actions to specific commit SHAs for security and stability.

## 7. Consumable Artifacts

The primary outputs of the CD process for consumers will be:

*   **Versioned Docker Images:** Available on GitHub Container Registry or Docker Hub.
*   **Example Deployment Files (Optional):** Versioned `docker-compose.yml` files or potentially Helm charts attached to GitHub Releases, demonstrating how to run the Nucleus components together for different scenarios (e.g., self-hosting).

This CI/CD strategy aims to provide confidence in code quality and streamline the release process, making it easier for users to adopt and deploy Nucleus OmniRAG in their chosen environments.

---
