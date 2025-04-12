# Phase 5: Public Knowledge Repository Tasks

**Epic:** [`EPIC-PUBLIC-GOOD`](./00_ROADMAP.md#phase-5-public-knowledge-repository-public-good)
**Requirements:** [`05_REQUIREMENTS_PHASE5_PUBLIC_GOOD.md`](../Requirements/05_REQUIREMENTS_PHASE5_PUBLIC_GOOD.md)

This document details the specific tasks required to complete Phase 5.

---

## `ISSUE-PUB-CONTRIB-01`: Implement UI Flow for Nomination

*   [ ] **TASK-P5-NOM-01:** Design UI component in Web App (`Nucleus.WebApp`) for displaying user's `PersonaKnowledgeEntry` items.
*   [ ] **TASK-P5-NOM-02:** Add "Nominate for Public Repository" button/action to the UI component.
*   [ ] **TASK-P5-NOM-03:** Implement UI for displaying contribution terms/licensing and capturing user agreement.
*   [ ] **TASK-P5-NOM-04:** Implement API endpoint in `Nucleus.Api` to receive nomination requests.
*   [ ] **TASK-P5-NOM-05:** Implement logic to flag the nominated `PersonaKnowledgeEntry` or create a separate nomination record.
*   [ ] **TASK-P5-NOM-06:** Implement notification system (email/in-app) to inform user nomination is received/pending review.

## `ISSUE-PUB-VET-01`: Develop Automated Vetting Pipeline

*   [ ] **TASK-P5-VET-AUTO-01:** Research and select PII detection service/library (e.g., Azure AI Language Service - PII Detection).
*   [ ] **TASK-P5-VET-AUTO-02:** Research and select sensitivity/confidentiality pattern detection methods (e.g., regex, keyword lists, potentially custom models).
*   [ ] **TASK-P5-VET-AUTO-03:** Research and select content safety service/library (e.g., Azure AI Content Safety).
*   [ ] **TASK-P5-VET-AUTO-04:** Implement Azure Function or step in orchestration triggered by nomination.
*   [ ] **TASK-P5-VET-AUTO-05:** Integrate calls to selected vetting services within the function.
*   [ ] **TASK-P5-VET-AUTO-06:** Define logic/thresholds for automated pass/fail/needs-review status based on vetting results.
*   [ ] **TASK-P5-VET-AUTO-07:** Update nomination record status based on automated vetting outcome.

## `ISSUE-PUB-ADMIN-01`: Create Admin UI for Manual Review

*   [ ] **TASK-P5-ADMIN-01:** Design Admin UI page in Web App (`Nucleus.WebApp`) to list nominations pending review.
*   [ ] **TASK-P5-ADMIN-02:** Display nominated content and automated vetting results to the reviewer.
*   [ ] **TASK-P5-ADMIN-03:** Implement Approve/Reject actions for reviewers.
*   [ ] **TASK-P5-ADMIN-04:** Implement API endpoints in `Nucleus.Api` for:
    *   Querying nominations pending review.
    *   Updating nomination status (Approved/Rejected).
*   [ ] **TASK-P5-ADMIN-05:** Implement role-based access control for the review interface.

## `ISSUE-PUB-REPO-01`: Set Up Public Repository Infrastructure

*   [ ] **TASK-P5-INFRA-01:** Decide on storage mechanism for Public Knowledge (e.g., separate Cosmos DB container/database, separate Azure AI Search index).
*   [ ] **TASK-P5-INFRA-02:** Update Bicep/Terraform templates to include public repository resources.
*   [ ] **TASK-P5-INFRA-03:** Define data model/schema for public knowledge entries (including attribution, license info).
*   [ ] **TASK-P5-INFRA-04:** Implement `IPublicKnowledgeRepository` interface and adapter for chosen storage.

## `ISSUE-PUB-WORKFLOW-01`: Implement Contribution Workflow

*   [ ] **TASK-P5-WF-01:** Implement Azure Function or orchestration step triggered by manual approval.
*   [ ] **TASK-P5-WF-02:** Logic to copy/transform the approved `PersonaKnowledgeEntry` data to the public format.
*   [ ] **TASK-P5-WF-03:** Add attribution (user alias/ID) and license information.
*   [ ] **TASK-P5-WF-04:** Save the entry to the Public Knowledge Repository using `IPublicKnowledgeRepository`.
*   [ ] **TASK-P5-WF-05:** Update nomination record status to "Published".
*   [ ] **TASK-P5-WF-06:** Implement notification to user upon approval/rejection.

## `ISSUE-PUB-ACCESS-01`: Integrate Public Repository Querying

*   [ ] **TASK-P5-ACC-01:** Update `IRetrievalService` interface and implementation(s) to optionally query the Public Repository.
*   [ ] **TASK-P5-ACC-02:** Add parameter to API query endpoints to specify whether to include public results.
*   [ ] **TASK-P5-ACC-03:** Update Web App UI query interface with option (e.g., checkbox) to include public knowledge.
*   [ ] **TASK-P5-ACC-04:** Update Platform Bot query handlers (Teams, Slack, Discord) to potentially include public results based on command/context.
*   [ ] **TASK-P5-ACC-05:** Ensure public results are clearly distinguishable from private results in all interfaces.

## `ISSUE-PUB-ACCESS-02`: Implement Attribution & Licensing Display

*   [ ] **TASK-P5-ATTR-01:** Ensure public knowledge data model includes attribution and license fields.
*   [ ] **TASK-P5-ATTR-02:** Update UI components (Web App, Platform Bot responses) to display attribution and license when showing public results.
*   [ ] **TASK-P5-ATTR-03:** Define user alias/profile system if real usernames are not desired for attribution.

## `ISSUE-PUB-GOV-01`: Finalize Governance Documentation

*   [ ] **TASK-P5-GOV-01:** Write clear Contribution Guidelines document.
*   [ ] **TASK-P5-GOV-02:** Write Content Policies document (what is/isn't acceptable).
*   [ ] **TASK-P5-GOV-03:** Finalize and document the chosen standard license (e.g., CC BY-SA 4.0).
*   [ ] **TASK-P5-GOV-04:** Publish these documents within the Web App or other accessible location.

## `ISSUE-PUB-API-01`: (Optional) Design Public API Endpoint

*   [ ] **TASK-P5-API-01:** Define scope and requirements for a public read-only API.
*   [ ] **TASK-P5-API-02:** Design API endpoints, request/response formats, and authentication/rate limiting strategy.
*   [ ] **TASK-P5-API-03:** Implement the public API endpoints in `Nucleus.Api` (or a separate API project).
*   [ ] **TASK-P5-API-04:** Implement required security measures (API keys, rate limiting).
*   [ ] **TASK-P5-API-05:** Document the public API.

---
