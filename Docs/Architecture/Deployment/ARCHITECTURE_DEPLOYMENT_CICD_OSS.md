---
title: Architecture - CI/CD Strategy for Open Source
description: Outlines the Continuous Integration and Continuous Delivery/Deployment strategy tailored for the Nucleus open-source project, utilizing GitHub Actions.
version: 1.7
date: 2025-05-22
parent: ../07_ARCHITECTURE_DEPLOYMENT.md
---

# Nucleus: CI/CD Strategy (Open Source Context)

## 1. Introduction

This document details the Continuous Integration (CI) and Continuous Delivery (CD) strategy for the Nucleus project, as part of the overall [Deployment Architecture](../07_ARCHITECTURE_DEPLOYMENT.md). As an open-source project, the focus of CI/CD is less on deploying to a specific production environment and more on:

*   **Validating Code Quality:** Ensuring code builds, passes tests, and adheres to standards.
*   **Creating Consumable Artifacts:** Building versioned Docker images and potentially example deployment configurations (like `docker-compose.yml` files) that users can easily deploy in their own environments ([Azure](./Hosting/ARCHITECTURE_HOSTING_AZURE.md), [Cloudflare](./Hosting/ARCHITECTURE_HOSTING_CLOUDFLARE.md), [Self-Hosted](./Hosting/ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md)).
*   **Automating Releases:** Streamlining the process of creating tagged releases with associated artifacts.
*   **Maintaining Security:** Ensuring the CI/CD process itself is secure and does not expose credentials or sensitive information.

## 2. Platform and Tooling

*   **CI/CD Platform:** **GitHub Actions** - Chosen due to its tight integration with GitHub repositories, generous free tier for public repositories, and extensive marketplace of actions.
    *   **Workflow Definitions:** Reside in the [`.github/workflows/`](../../../../.github/workflows/) directory.
        *   Pull Request Validation: [`pr-validation.yml`](../../../../.github/workflows/pr-validation.yml)
        *   Release Process: [`release.yml`](../../../../.github/workflows/release.yml)
*   **Container Registry:** **GitHub Container Registry (ghcr.io)** or **Docker Hub** - For hosting the publicly accessible, versioned Docker images of Nucleus components (`Nucleus.Api`, workers, etc.).
*   **Code Scanning/SAST (Optional):** GitHub Advanced Security (CodeQL), SonarCloud - For identifying potential security vulnerabilities and code quality issues.
*   **Container Scanning (Optional):** Trivy, Snyk, Dependabot - For scanning Docker images for known vulnerabilities in base images or dependencies.

## 3. Workflow Triggers and Branching Strategy

CI/CD workflows are primarily triggered by actions related to two key branches:

*   **`develop` Branch:** This is the main integration branch. All feature branches are merged into `develop` via Pull Requests.
    *   **Pull Requests targeting `develop`:** The [`pr-validation.yml`](../../../../.github/workflows/pr-validation.yml) workflow is triggered. It runs build, test (unit and conditionally integration), and linting stages to ensure code quality before merging.
    *   **Pushes to `develop` (post-merge):** The [`pr-validation.yml`](../../../../.github/workflows/pr-validation.yml) workflow also runs on direct pushes to `develop` to confirm the integrity of the branch after merges.
*   **`main` Branch:** This branch represents stable releases.
    *   **Git Tags (e.g., `vX.Y.Z`) on `main`:** When a version tag is pushed to `main`, the [`release.yml`](../../../../.github/workflows/release.yml) workflow is triggered. This workflow handles building versioned Docker images, pushing them to the public registry, and creating a GitHub Release with associated artifacts.

## 4. Key CI/CD Stages

### 4.1. Pull Request Validation Workflow (`pr-validation.yml`)

This workflow, defined in [`pr-validation.yml`](../../../../.github/workflows/pr-validation.yml), typically includes:

1.  **Checkout Code:** Get the latest source code from the PR branch.
2.  **Setup .NET:** Install the required .NET SDK version (currently .NET 9).
3.  **Restore Dependencies:** Run `dotnet restore`.
4.  **Build:** Run `dotnet build` for all relevant projects.
5.  **Linting/Formatting Check (Optional):** Run tools like `dotnet format --verify-no-changes`.
6.  **Unit Tests:** Run `dotnet test --filter "Category!=Integration"` for unit test projects.
7.  **Integration Tests (Conditional):** Run `dotnet test --filter "Category=Integration"`. These tests are currently conditional and depend on the `INTEGRATION_TESTS_ENABLED` environment variable (see `Nucleus.Abstractions.NucleusConstants`). This variable is set to `"false"` by default in the workflow due to ongoing test harness pathing considerations. When enabled, these tests may require specific secrets (like API keys) managed via GitHub Environment Secrets (see Section 6).
8.  **Static Analysis (Optional):** Run CodeQL or other SAST tools.

### 4.2. Release Workflow (`release.yml`)

This workflow, defined in [`release.yml`](../../../../.github/workflows/release.yml) and triggered by Git tags on `main` (matching `v*.*.*`), includes stages similar to the PR validation, plus release-specific steps:

1.  **Checkout Code:** Get the code corresponding to the Git tag.
2.  **Setup .NET:** Install .NET SDK (currently .NET 9 for release builds).
3.  **Restore Dependencies.**
4.  **Build Solution (Release Configuration).**
5.  **Run All Tests:** Execute all tests (unit and integration) by running `dotnet test --configuration Release`. For integration tests requiring secrets, the `INTEGRATION_TESTS_ENABLED` variable is set to `"true"`, and necessary secrets are accessed from the configured GitHub Environment (`ci_tests`). Test results (`.trx` file) are uploaded as a workflow artifact.
6.  **Log in to GitHub Container Registry (GHCR).**
7.  **Extract Docker Metadata:** Uses `docker/metadata-action` to generate appropriate tags for the Docker image based on the Git tag. This includes:
    *   Full semantic version (e.g., `v1.2.3`).
    *   Major.minor version (e.g., `1.2`).
    *   `latest` tag if the Git tag is not a pre-release (e.g., does not contain `-alpha`, `-beta`, `-rc`).
8.  **Build and Push Docker Image:** Build the primary Docker image for `Nucleus.Api` using the Git tag and other derived tags (e.g., `ghcr.io/your-org/nucleus-api:vX.Y.Z`, `ghcr.io/your-org/nucleus-api:X.Y`, `ghcr.io/your-org/nucleus-api:latest`).
9.  **Container Vulnerability Scan (Future):** Placeholder for Trivy/Snyk scanning against the built images.
10. **Package Release Artifacts:** Creates a `nucleus-release-assets.zip` file containing:
    *   `docker-compose.yml` (from `deploy/docker-compose.yml` - note: image tags within this file are not dynamically updated by the workflow).
    *   `README.md` (from the root of the repository).
11. **Generate Release Notes:** Currently, basic placeholder notes are generated. (See Section 5.2 for future enhancements).
12. **Create GitHub Release:** Automatically creates a GitHub Release associated with the Git tag. Uploads `nucleus-release-assets.zip` and the standalone `docker-compose.yml` as release assets. The release is marked as a pre-release if the tag indicates it (e.g., `v1.0.0-beta.1`). These assets are hosted directly by GitHub on the release page.

## 5. Versioning Strategy

*   **Semantic Versioning (SemVer 2.0.0):** Adhere to `MAJOR.MINOR.PATCH` versioning (e.g., `v0.1.0`, `v1.0.0`, `v1.0.1-alpha.1`).
*   **Source of Truth for Version:** The primary source of truth for a release version is the **Git tag** (e.g., `v0.1.0`) applied to a commit on the `main` branch. This tag triggers the release workflow.
*   **Assembly Versioning (`Directory.Build.props`):**
    *   A `Directory.Build.props` file at the root of the solution sets a common `<VersionPrefix>` (e.g., `0.1.0`) for all projects. This provides a baseline version for local builds and development.
    *   During the CI/CD release workflow, the version derived from the Git tag is the definitive version for the release artifacts (Docker images, GitHub Release name).
    *   The `.csproj` files typically do not specify the version directly, inheriting it from `Directory.Build.props` or allowing the SDK to manage it. This simplifies version management across multiple projects.
    *   For NuGet packages (if any are produced in the future), the version from the Git tag would be used to pack them. The `Directory.Build.props` file also specifies metadata like `<PackageIcon>icon.png</PackageIcon>` and `<PackageReadmeFile>README.md</PackageReadmeFile>`. For these to be effective for NuGet packaging, an `icon.png` file (e.g., a 128x128 PNG) should be placed in the root of the repository (e.g., `/icon.png`), and the main `README.md` will be used. The `icon.png` path in `Directory.Build.props` is relative to the project file, so for a root `icon.png`, it might need to be specified as `$(MSBuildThisFileDirectory)../icon.png` if `Directory.Build.props` is in a subdirectory, or simply `icon.png` if it's at the solution root and projects can find it relative to their own path or the solution path during packaging. Given it's at the solution root, `icon.png` as the value for `<PackageIcon>` is generally correct for SDK-style projects when the icon file is also at the root.
*   **Docker Image Tags:** Docker images will be tagged with:
    *   The full corresponding SemVer tag (e.g., `v1.0.0`, `v1.0.1-beta.2`).
    *   The major.minor version (e.g., `1.0`) for non-prereleases.
    *   The `latest` tag for the most recent non-prerelease version.
*   **Epoch Versioning:** While considered, standard SemVer 2.0.0 is adopted for simplicity and broad tooling compatibility, especially for AI agent understanding and conventional open-source practices. Complex versioning schemes can be harder to manage and automate reliably.

### 5.1. How to Make a Release (Manual Steps for Maintainers)

1.  **Ensure `develop` is Stable:** All desired features and fixes for the release should be merged into the `develop` branch and validated by the `pr-validation.yml` workflow.
2.  **Merge `develop` into `main`:**
    *   Checkout the `main` branch: `git checkout main`
    *   Pull the latest changes: `git pull origin main`
    *   Merge `develop` into `main`: `git merge develop` (resolve any conflicts if they occur).
    *   Push the updated `main` branch: `git push origin main`
3.  **Create and Push a Version Tag:**
    *   Decide on the new version number following SemVer (e.g., `v0.1.0`, `v0.2.0`, `v1.0.0-alpha.1`).
    *   Tag the latest commit on `main`: `git tag vX.Y.Z` (e.g., `git tag v0.1.0`).
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
    *   **`release-drafter`:** This action can automatically draft GitHub Release notes as Pull Requests are merged into the `develop` or `main` branch. It categorizes changes based on PR labels (which can align with conventional commit types) or directly from conventional commit messages. Maintainers can then review and publish these drafted notes when creating a new release tag.
        *   *Configuration:* A `.github/release-drafter.yml` configuration file would be needed to define categories and templates.
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

*   **Versioned Docker Images:** Available on GitHub Container Registry (`ghcr.io/jfjordanfarr/nucleus-api`) tagged with SemVer (e.g., `v0.1.0`, `0.1`, `latest`).
*   **GitHub Releases:** Each release will have:
    *   A `nucleus-vX.Y.Z-assets.zip` file containing the `docker-compose.yml` and `README.md` for that version.
    *   A standalone `docker-compose.yml` file.
    *   Release notes (currently basic, potentially auto-generated in the future).

This CI/CD strategy aims to provide confidence in code quality and streamline the release process, making it easier for users to adopt and deploy Nucleus in their chosen environments.

---
