# EduFlow OmniEducator Data Model Proposals

## Introduction

This document outlines proposed data models for the EduFlow OmniEducator persona within the Nucleus OmniRAG framework. The primary goals driving these models are:

1.  **Capture Authentic Learning:** Model the richness of learning that occurs through diverse activities, especially digital creation, exploration, and project-based work.
2.  **Dual-Lens Classification:** Incorporate both foundational learning capabilities (the "tautological" view focused on *how* learning happens) and traditional pedagogical knowledge domains (the "pedagogical" view focused on *what* is learned) for comprehensive understanding and mapping.
3.  **Connect to Standards:** Facilitate the mapping of observed learning evidence to relevant educational benchmarks and curriculum standards.
4.  **Support Rich Interaction & Reporting:** Provide the necessary structure for insightful reporting, personalized feedback, AI-driven analysis, and interactive learning experiences (supporting both "fast path" and "slow path" services).
5.  **Flexibility:** Accommodate different ages, learning styles, cultural contexts, and the evolving nature of learning itself.

These models are designed to work together, creating a network of information that reflects a learner's journey and capabilities.

## Core Data Models

### 1. Learner & Goals

These models represent the individual learner and their objectives.

**`LearnerProfile`**

| Attribute               | Type                               | Description                                                                 | Relationships                      |
| :---------------------- | :--------------------------------- | :-------------------------------------------------------------------------- | :--------------------------------- |
| `learner_id`            | UUID (Primary Key)                 | Unique identifier for the learner.                                          |                                    |
| `name`                  | String                             | Learner's name or pseudonym.                                                |                                    |
| `age_group`             | Enum (e.g., EarlyChildhood, ...) | Broad developmental stage.                                                   |                                    |
| `learning_preferences`  | List<String> (Tags)                | Tags indicating preferred learning styles, modalities, or environments.     |                                    |
| `interests`             | List<String> (Tags)                | Topics, hobbies, or areas of passion expressed by the learner.              |                                    |
| `current_goals`         | List<UUID>                         | IDs of active `Goal` records associated with this learner.                  | -> `Goal` (Many)                   |
| `created_at`            | Timestamp                          | When the profile was created.                                               |                                    |
| `updated_at`            | Timestamp                          | When the profile was last updated.                                          |                                    |
| `portfolio_summary_uri` | String (Optional)                  | Link to a generated summary or entry point for the learner's portfolio. | (Implicitly links to Artifacts) |

**`Goal`**

| Attribute         | Type               | Description                                                                 | Relationships                     |
| :---------------- | :----------------- | :-------------------------------------------------------------------------- | :-------------------------------- |
| `goal_id`         | UUID (Primary Key) | Unique identifier for the goal.                                             |                                   |
| `learner_id`      | UUID (Foreign Key) | The learner who set or is assigned this goal.                             | -> `LearnerProfile` (One)         |
| `description`     | String             | Text describing the goal.                                                   |                                   |
| `status`          | Enum               | Current status (e.g., `Active`, `Achieved`, `Paused`, `Abandoned`).         |                                   |
| `type`            | Enum               | Type of goal (e.g., `LearningObjective`, `ProjectMilestone`, `SkillTarget`). |                                   |
| `target_date`     | Date (Optional)    | Optional target completion date.                                            |                                   |
| `related_skills`  | List<UUID>         | IDs of `Skill` records relevant to achieving this goal.                     | -> `Skill` (Many)                 |
| `related_domains` | List<UUID>         | IDs of `KnowledgeDomain` records relevant to achieving this goal.           | -> `KnowledgeDomain` (Many)       |
| `linked_activities`| List<UUID>         | IDs of `LearningActivity` records undertaken in pursuit of this goal.     | -> `LearningActivity` (Many)    |
| `created_at`      | Timestamp          | When the goal was created.                                                  |                                   |
| `updated_at`      | Timestamp          | When the goal was last updated.                                             |                                   |

### 2. Learning Process

These models capture the activities learners engage in and the evidence they produce.

**`LearningActivity` (or `Project`)**

| Attribute              | Type                | Description                                                                        | Relationships                        |
| :--------------------- | :------------------ | :--------------------------------------------------------------------------------- | :----------------------------------- |
| `activity_id`          | UUID (Primary Key)  | Unique identifier for the activity or project.                                     |                                      |
| `learner_id`           | UUID (Foreign Key)  | The primary learner associated with this activity.                                 | -> `LearnerProfile` (One)            |
| `title`                | String              | Name or title of the activity/project.                                             |                                      |
| `description`          | String              | Detailed description of the activity, goals, process.                              |                                      |
| `start_date`           | Timestamp           | When the activity began.                                                           |                                      |
| `end_date`             | Timestamp (Optional)| When the activity concluded (if applicable).                                       |                                      |
| `status`               | Enum                | Current status (e.g., `Planning`, `InProgress`, `Completed`, `Paused`).          |                                      |
| `type`                 | Enum                | Nature of the activity (e.g., `Project`, `Exploration`, `Lesson`, `GamePlay`, `Creation`). |                                      |
| `context_description`  | String (Optional)   | Notes about the context (e.g., "Collaborative session", "Self-directed at home"). |                                      |
| `related_goals`        | List<UUID>          | IDs of `Goal` records this activity contributes to.                                | -> `Goal` (Many)                     |
| `collaborator_ids`     | List<UUID> (Optional)| IDs of other `LearnerProfile`s involved.                                         | -> `LearnerProfile` (Many, Optional) |
| `associated_artifacts` | List<UUID>          | IDs of `LearningArtifact` records produced during this activity.                   | -> `LearningArtifact` (Many)         |
| `created_at`           | Timestamp           | When the activity record was created.                                              |                                      |
| `updated_at`           | Timestamp           | When the activity record was last updated.                                         |                                      |

**`LearningArtifact` (or `LearningEvidence`)**

*Note: This model describes the source artifact itself. Detailed, persona-specific analysis (e.g., identified skills, domain relevance) generated by personas like EduFlow is stored within `PersonaKnowledgeEntry` documents in the respective persona's database partition, linking back to this artifact.*

| Attribute              | Type                       | Description                                                                                                | Relationships                             |
| :--------------------- | :------------------------- | :--------------------------------------------------------------------------------------------------------- | :---------------------------------------- |
| `artifact_id`          | UUID (Primary Key)         | Unique identifier for the piece of evidence/source artifact.                                               |                                           |
| `activity_id`          | UUID (Foreign Key, Optional) | The activity during which this artifact was produced (can be null if standalone).                          | -> `LearningActivity` (One, Optional)   |
| `learner_id`           | UUID (Foreign Key)         | The learner who created or is represented by this artifact.                                                | -> `LearnerProfile` (One)                 |
| `timestamp`            | Timestamp                  | When the source artifact was originally created or captured.                                               |                                           |
| `artifact_type`        | Enum                       | Nature of the artifact (e.g., `Code`, `Image`, `Video`, `Audio`, `Text`, `URL`, `Document`, `GameSave`, `3DModel`). |                                           |
| `source_uri`           | String                     | URI pointing to the actual artifact data (e.g., blob storage path, user cloud storage link).               |                                           |
| `content_hash`         | String (Optional)          | Hash of the artifact content for integrity/uniqueness checks.                                              |                                           |
| `learner_description`  | String (Optional)          | Description provided by the learner about the artifact.                                                    |                                           |
| `extracted_text`       | String (Optional)          | Raw text content extracted by initial parsing services (used as input for persona analysis).             |                                           |
| `metadata`             | JSON (Optional)            | Type-specific metadata extracted during initial parsing (e.g., duration for video, image dimensions).      |                                           |
| `created_at`           | Timestamp                  | When this artifact metadata record was created in the Nucleus system.                                      |                                           |

### 3. Knowledge & Skills Representation

These models define the structure for classifying knowledge and capabilities, incorporating the dual-tree concept.

**`KnowledgeDomain`**

| Attribute        | Type               | Description                                                                                                                                 | Relationships                       |
| :--------------- | :----------------- | :------------------------------------------------------------------------------------------------------------------------------------------ | :---------------------------------- |
| `domain_id`      | UUID (Primary Key) | Unique identifier for the knowledge domain or capability area.                                                                              |                                     |
| `name`           | String             | Name of the domain (e.g., "Mathematics", "Scientific Inquiry", "Language Arts", "Creative Expression").                                   |                                     |
| `description`    | String             | Detailed description of the domain.                                                                                                         |                                     |
| `tree_type`      | Enum               | Indicates which conceptual tree this belongs to (`PedagogicalSubject`, `FoundationalCapability`).                                         |                                     |
| `parent_domain_id`| UUID (Optional)    | ID of the parent domain for hierarchical structure (e.g., "Algebra" under "Mathematics").                                                   | -> `KnowledgeDomain` (One, Optional) |
| `related_skills` | List<UUID>         | IDs of specific `Skill` records falling under this domain.                                                                                  | -> `Skill` (Many)                   |
| `created_at`     | Timestamp          | When the domain record was created.                                                                                                         |                                     |

*Note: The `tree_type` attribute allows filtering/grouping to represent both the "Pedagogical" and "Tautological" structures discussed previously.*

**`Skill`**

| Attribute         | Type               | Description                                                                                                         | Relationships                             |
| :---------------- | :----------------- | :------------------------------------------------------------------------------------------------------------------ | :---------------------------------------- |
| `skill_id`        | UUID (Primary Key) | Unique identifier for the skill.                                                                                    |                                           |
| `name`            | String             | Name of the skill (e.g., "Debugging", "Persuasive Writing", "Critical Thinking", "Using Variables").               |                                           |
| `description`     | String             | Detailed description of the skill.                                                                                  |                                           |
| `parent_skill_id` | UUID (Optional)    | ID of a broader skill this falls under (for hierarchy).                                                             | -> `Skill` (One, Optional)                |
| `related_domains` | List<UUID>         | IDs of `KnowledgeDomain` records this skill is primarily associated with.                                           | -> `KnowledgeDomain` (Many)             |
| `skill_level_descriptors` | JSON (Optional) | Descriptions for different proficiency levels (e.g., {"Emerging": "...", "Developing": "...", "Proficient": "..."}). |                                           |
| `created_at`      | Timestamp          | When the skill record was created.                                                                                  |                                           |

**`Standard` (Curriculum Standard / Benchmark)**

| Attribute        | Type               | Description                                                                     | Relationships                      |
| :--------------- | :----------------- | :------------------------------------------------------------------------------ | :--------------------------------- |
| `standard_id`    | UUID (Primary Key) | Unique identifier for the standard.                                             |                                    |
| `framework_name` | String             | Name of the framework (e.g., "Common Core", "NGSS", "ISTE", "State Curriculum"). |                                    |
| `code`           | String             | Official code or identifier within the framework (e.g., "CCSS.ELA-Literacy.W.5.2"). |                                    |
| `description`    | String             | Full text description of the standard.                                          |                                    |
| `grade_levels`   | List<String>       | Applicable grade level(s) or range(s).                                          |                                    |
| `subject_area`   | String (Optional)  | Primary subject area (can often be inferred from linked domains).             |                                    |
| `related_domains`| List<UUID>         | IDs of `KnowledgeDomain` records this standard falls under.                     | -> `KnowledgeDomain` (Many)        |
| `related_skills` | List<UUID>         | IDs of specific `Skill` records addressed by this standard.                     | -> `Skill` (Many, Optional)        |
| `source_url`     | String (Optional)  | URL to the official standard definition.                                        |                                    |
| `created_at`     | Timestamp          | When the standard record was ingested.                                          |                                    |

### 4. Evaluation & Interpretation

These models handle assessments, feedback, and AI-driven analysis performed by the EduFlow persona.

**`Assessment`**

| Attribute         | Type                       | Description                                                                                                    | Relationships                                |
| :---------------- | :------------------------- | :------------------------------------------------------------------------------------------------------------- | :------------------------------------------- |
| `assessment_id`   | UUID (Primary Key)         | Unique identifier for the assessment instance.                                                                 |                                              |
| `activity_id`     | UUID (Foreign Key, Optional) | The activity being assessed (if applicable).                                                                   | -> `LearningActivity` (One, Optional)      |
| `artifact_ids`    | List<UUID> (Optional)      | Specific artifact(s) being assessed.                                                                           | -> `LearningArtifact` (Many, Optional)     |
| `learner_id`      | UUID (Foreign Key)         | The learner being assessed.                                                                                    | -> `LearnerProfile` (One)                    |
| `timestamp`       | Timestamp                  | When the assessment was conducted/completed.                                                                   |                                              |
| `type`            | Enum                       | Type of assessment (e.g., `Observation`, `Quiz`, `PortfolioReview`, `SelfAssessment`, `AiGeneratedCheck`).       |                                              |
| `criteria`        | String or JSON             | Description of criteria, link to rubric, or list of targeted `Skill`/`Standard` IDs.                          | (May implicitly link Skill/Standard)       |
| `result_summary`  | String (Optional)          | Overall summary or qualitative result.                                                                         |                                              |
| `result_details`  | JSON (Optional)            | Detailed results (e.g., scores, rubric levels per criterion, specific feedback points).                      |                                              |
| `assessor_id`     | UUID                       | ID of the assessor (`LearnerProfile` ID for self/peer, User ID for teacher, Persona ID for AI).               | (Links to LearnerProfile/User/Persona) |
| `assessor_type`   | Enum                       | `Learner`, `Educator`, `AI`.                                                                                   |                                              |
| `created_at`      | Timestamp                  | When the assessment record was created.                                                                        |                                              |

**`Feedback`**

| Attribute       | Type                       | Description                                                                         | Relationships                                 |
| :-------------- | :------------------------- | :---------------------------------------------------------------------------------- | :-------------------------------------------- |
| `feedback_id`   | UUID (Primary Key)         | Unique identifier for the piece of feedback.                                        |                                               |
| `target_id`     | UUID                       | ID of the entity receiving feedback (`LearningActivity`, `LearningArtifact`, `Assessment`, `Goal`). | (Links to Activity/Artifact/Assessment/Goal) |
| `target_type`   | Enum                       | Type of the entity receiving feedback.                                              |                                               |
| `learner_id`    | UUID (Foreign Key)         | The learner the feedback is directed towards.                                       | -> `LearnerProfile` (One)                     |
| `timestamp`     | Timestamp                  | When the feedback was given.                                                        |                                               |
| `provider_id`   | UUID                       | ID of the feedback provider (User ID, `LearnerProfile` ID, Persona ID).             | (Links to User/LearnerProfile/Persona)      |
| `provider_type` | Enum                       | `Educator`, `Learner` (Self/Peer), `AI`.                                            |                                               |
| `content`       | String                     | The textual content of the feedback.                                                |                                               |
| `type`          | Enum (Optional)            | Nature of feedback (e.g., `Encouragement`, `Guidance`, `Correction`, `Question`).   |                                               |
| `created_at`    | Timestamp                  | When the feedback record was created.                                               |                                               |

**`EduFlowAnalysis`**

*Note: This defines the structure of the analysis object generated by the EduFlow persona. An instance of this object will be stored within the `analysis` field of a `PersonaKnowledgeEntry` document in EduFlow's Cosmos DB container.*

| Attribute                | Type                      | Description                                                                                                         | Relationships                         |
| :----------------------- | :------------------------ | :------------------------------------------------------------------------------------------------------------------ | :------------------------------------ |
| `analysis_timestamp`     | Timestamp                 | When this specific analysis was generated by the persona.                                                           |                                       |
| `model_version`          | String                    | Identifier of the AI model version used for this analysis (e.g., "gemini-1.5-pro-latest").                          |                                       |
| `confidence_score`       | Float (0.0-1.0)           | Persona's confidence in the accuracy and completeness of this analysis.                                            |                                       |
| `summary`                | String                    | Persona's concise summary of the source artifact's content from an educational perspective.                       |                                       |
| `interpretation_type`    | Enum (Optional)           | Primary focus of this specific analysis instance (e.g., `SkillEvidence`, `DomainClassification`, `StandardsAlignment`). |                                       |
| `suggested_skill_ids`    | List<UUID>                | IDs of `Skill` records the AI identified as relevant or demonstrated in the source artifact.                      | -> `Skill` (Many, Optional)           |
| `suggested_domain_ids`   | List<UUID>                | IDs of `KnowledgeDomain` records the AI classified the source artifact under.                                     | -> `KnowledgeDomain` (Many, Optional) |
| `suggested_standard_ids` | List<UUID>                | IDs of `Standard` records the AI suggests alignment with.                                                       | -> `Standard` (Many, Optional)        |
| `potential_misconceptions`| String (Optional)         | AI notes on potential misunderstandings evident in the source artifact.                                           |                                       |
| `suggested_next_steps`   | String (Optional)         | Ideas for follow-up activities or further learning based on the analysis.                                         |                                       |
| `extracted_keywords`     | List<String> (Optional)   | Key terms or concepts extracted/identified by the AI.                                                             |                                       |
| `explanation`            | String (Optional)         | AI's reasoning or justification for the analysis (potentially linking specific parts of artifact).               |                                       |
| `additional_context`     | Dictionary<String, String> | Other relevant structured details extracted by the AI (optional).                                                 |                                       |

## Relationships Summary

*   A **Learner** engages in **Activities**, producing **Artifacts** and pursuing **Goals**.
*   **Activities** generate **Artifacts** and relate to **Goals**.
*   **Artifacts** are the core evidence, linked to the **Activity**, **Learner**, and potentially classified against **Skills**, **Domains**, and **Standards**.
*   **Skills** and **KnowledgeDomains** form hierarchical structures (representing both pedagogical subjects and foundational capabilities via `tree_type`) and relate to **Standards**.
*   **Assessments** evaluate **Activities** or **Artifacts** against criteria (often related to **Skills** or **Standards**).
*   **Feedback** can be provided on **Activities**, **Artifacts**, or **Assessments**.
*   **EduFlowAnalysis** provides automated analysis of **Artifacts**, suggesting links to **Skills**, **Domains**, and **Standards**.

## Usage Scenarios Example: Documenting a Scratch Project

1.  **Learner** starts a new **LearningActivity** (Type: `Project`, Title: "Water Cycle Simulation").
2.  As they work, **LearningArtifacts** are captured (Type: `Code` for Scratch JSON, Type: `Video` for screencast, Type: `Text` for project description).
3.  The EduFlow persona (via **EduFlowAnalysis**) analyzes the `Code` artifact:
    *   Extracts `keywords`: "loop", "variable", "if/else", "evaporation", "coordinates".
    *   Suggests `Skill` IDs: "Algorithmic Thinking", "Debugging", "Using Variables".
    *   Suggests `KnowledgeDomain` IDs: "Mathematics" (Pedagogical), "Technology" (Pedagogical), "Mathematical & Logical Reasoning" (Foundational), "Creative Expression & Design" (Foundational), "Sciences" (Pedagogical - due to topic).
    *   Suggests `Standard` alignment based on keywords and detected skills/domains.
4.  Learner/Educator adds manual tags or confirms AI suggestions on the **LearningArtifact**.
5.  Later, an **Assessment** (Type: `PortfolioReview`) might look at multiple **LearningArtifacts** (referencing their metadata) from this activity. The reviewer (human or AI) could retrieve the corresponding `PersonaKnowledgeEntry` documents containing the **EduFlowAnalysis** for each artifact to evaluate specific **Skills** or progress towards a **Goal**.
6.  **Feedback** might be given by the AI (based on its **EduFlowAnalysis**) or an educator.

## Future Considerations

*   **Collaboration:** More detailed modeling of collaborative roles and contributions within activities.
*   **Context:** Richer context capture (location, tools used beyond artifact type, emotional state).
*   **External Tool Integration:** Specific models for data ingested from platforms like Scratch, Minecraft, Khan Academy, etc.
*   **Learning Pathways:** Modeling sequences of activities or suggested learning paths.
*   **Granularity:** Finer-grained representation of artifact components (e.g., specific code blocks, video segments).

This proposed structure provides a robust foundation for capturing and interpreting learning within the EduFlow OmniEducator persona, balancing the need for structured data with the flexibility required for authentic, project-based education.