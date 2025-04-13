---
title: Processing Architecture - Data Visualization
description: Describes a skill that can be performed by personas, which involves writing structured data and simple visualization code snippets into a template pyodide-based static HTML page.
version: 1.0
date: 2025-04-13
---

# Processing Architecture: Data Visualization ('Dataviz')

## 1. Overview

The Data Visualization (Dataviz) capability represents a "skill" that Nucleus OmniRAG Personas can invoke to present data-driven insights interactively to users within their client platform (e.g., Teams, Slack). It allows a Persona to dynamically generate small, interactive data visualizations powered by Python libraries (like Plotly, Seaborn, Matplotlib) running client-side via Pyodide.

This architecture emphasizes separation of concerns:

*   **Persona Responsibility:** Determines *that* a visualization is needed, *what* data to show, and *how* (via a Python code snippet) to visualize it. The Persona provides the semantic understanding and the core visualization logic.
*   **Platform Adapter Responsibility:** Handles the *mechanics* of generating the final visualization artifact (a self-contained HTML file) and delivering it to the user within the specific client platform's UI constraints (e.g., using a Teams Task Module).

The goal is to create rich, interactive, and contextually relevant data presentations directly within the user's workflow, leveraging the power of the Python data science ecosystem in a secure, sandboxed environment.

## 2. Triggering the Dataviz Skill

A Persona triggers the Dataviz skill by including specific instructions and data within its response payload after processing a user request or analyzing content (e.g., during `HandleQueryAsync` or `AnalyzeContentAsync`).

The Persona's response payload should indicate a desire to generate a visualization and must include:

1.  **Python Code Snippet:** A string containing Python code designed to run within the designated `### START AI GENERATED CODE ###` section of the Pyodide template. This snippet should perform the visualization logic using pre-imported libraries (e.g., `pandas as pd`, `plotly.express as px`) and assign the resulting figure object to a predefined variable (`final_fig_plotly` or `final_fig_matplotlib`).
2.  **JSON Data:** The data required by the Python snippet, formatted as a valid JSON object structure. This data will be made available to the Python script via the `js.jsonData` object in the template.
3.  **(Optional) Type Hint:** A hint indicating the primary library used (e.g., 'plotly', 'matplotlib') to assist the adapter in downstream processing or UI decisions.
4.  **(Optional) Title/Description:** A suggested title or description for the visualization.

## 3. Artifact Generation Process (`viz.html`)

Upon receiving a response payload containing a visualization request from a Persona, the responsible **Platform Adapter** (e.g., `Nucleus.Adapters.Teams`) performs the following steps to generate the self-contained `viz.html` artifact:

1.  **Load Template:** Reads the content of the standard `template.html` file. This template contains the necessary HTML structure, CSS, Pyodide/Plotly CDN links, and JavaScript logic.
2.  **Inject Branding:** Replaces a designated CSS placeholder (e.g., `/* BRANDING_CSS */`) in the template with platform-specific or configured branding styles.
3.  **Inject Python Script:**
    *   Retrieves the Python code snippet provided by the Persona.
    *   **Escapes** the snippet appropriately for embedding within a JavaScript multiline template literal (e.g., escaping backticks, backslashes, `${` sequences).
    *   Injects the escaped snippet into the corresponding placeholder within the JavaScript section of the template (e.g., replacing `### START AI GENERATED CODE ###`).
4.  **Inject JSON Data:**
    *   Retrieves the JSON data object provided by the Persona.
    *   Serializes the data into a JSON string if necessary.
    *   Injects the JSON string directly into the JavaScript placeholder (e.g., replacing `/* JSON_DATA_PLACEHOLDER */ {}` with `/* JSON_DATA_PLACEHOLDER */ { ... actual data ... }`).
5.  **Output:** The result is a complete HTML content string (`finalHtml`) representing the `viz.html` artifact, ready for storage and delivery.

## 4. The `viz.html` Template Structure

The `template.html` is the core foundation for the interactive visualization. Its key components include:

*   **HTML Boilerplate:** Standard HTML5 structure.
*   **CDN Links:** `<script>` tags to load Pyodide and Plotly.js libraries.
*   **CSS:** Basic styling for loading indicators, output areas, error messages, and export buttons, plus a placeholder for branding injection.
*   **HTML Structure:** `div` elements for displaying loading status, the visualization output (`#output-area`), error messages (`#error-area`), and export buttons (`#export-buttons`).
*   **JavaScript Logic:**
    *   **Placeholders:** Variables (`pythonScript`, `jsonData`) where the backend injects the Persona-provided code and data.
    *   **Pyodide Initialization:** Code to load the Pyodide runtime, potentially loading core Python packages (`pandas`, `plotly`, etc.). This initialization runs within a **Web Worker** to avoid blocking the UI thread.
    *   **Worker Communication:** Uses `postMessage` to send the Python script and data to the worker and receive results (completion payload, error messages, progress updates) back.
    *   **DOM Manipulation:** Handles displaying loading indicators, injecting the rendered HTML/SVG output into `#output-area`, showing errors in `#error-area`, and managing the visibility of export buttons.
    *   **Export Functions:** Event handlers for export buttons (PNG, SVG, HTML) that leverage Plotly.js capabilities or Blob/download techniques.
*   **Embedded Python Template:** The `pythonScript` JavaScript variable contains the multi-line string literal holding the Python code structure:
    *   **Fixed Preamble:** Imports common libraries (`js`, `json`, `io`, `pandas`, `plotly`, `matplotlib`, `seaborn`, etc.). Defines helper functions (`render_plotly_to_div_string`, `render_matplotlib_to_svg_string`).
    *   **Data Loading:** Code to access the `js.jsonData` object passed from JavaScript and convert it into a usable Python format (e.g., Pandas DataFrame).
    *   **AI Code Section:** The placeholder (`### START AI GENERATED CODE ###` ... `### END AI GENERATED CODE ###`) where the Persona's specific visualization logic is inserted.
    *   **Fixed Postamble:** Code to check which figure variable (`final_fig_plotly` or `final_fig_matplotlib`) was assigned by the AI code, call the appropriate rendering helper function, handle errors, and send the result (or error) back to the main JavaScript thread via `js.pyodideWorker.postMessage`. Includes critical resource cleanup like `plt.close(fig)`.

## 5. Security Considerations

Security is handled through multiple layers inherent in the design:

*   **Sandboxing:** Pyodide runs within the WebAssembly sandbox, further constrained by the browser's iframe sandbox (configured by the adapter).
*   **Controlled Execution:** The Python code runs within a predefined template structure, limiting the scope of the AI-generated portion.
*   **No Direct DOM Access:** Python code communicates results back to JavaScript via `postMessage`; it doesn't directly manipulate the host page DOM.
*   **Web Worker Isolation:** Running Pyodide in a worker prevents long-running scripts from freezing the UI and allows the main thread to potentially terminate the worker if it exceeds a timeout.
*   **Content Security Policy (CSP):** The adapter serving the `viz.html` must implement a strict CSP to control resource loading (scripts, styles) and prevent unauthorized network connections (`connect-src`).

## 6. Relationship to Client Adapters

While the *request* for a visualization originates from a Persona's analysis, the **implementation** of artifact generation (populating the `template.html`) and the delivery mechanism (e.g., Task Modules, file storage) resides within the **specific Client Adapter** (e.g., `Nucleus.Adapters.Teams`) responsible for the user interaction context. This ensures that platform-specific UI capabilities and APIs (like Graph API for SharePoint storage) are handled appropriately.