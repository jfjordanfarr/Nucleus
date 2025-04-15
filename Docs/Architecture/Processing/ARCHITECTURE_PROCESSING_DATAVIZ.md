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

Upon receiving a response payload containing a visualization request from a Persona, the **responsible Processing component (e.g., invoked by the Orchestrator)** performs the following steps to generate the self-contained `viz.html` artifact:

1.  **Load Templates:** Reads the content of the standard template files (`dataviz_template.html`, `dataviz_styles.css`, `dataviz_script.js`, `dataviz_plotly_script.py`, `dataviz_worker.js`) located within the Processing service's resources.
2.  **Inject Python Script:**
    *   Retrieves the Python code snippet provided by the Persona.
    *   **Escapes** the snippet appropriately for embedding within a JavaScript multiline template literal (e.g., escaping backticks, backslashes, `${` sequences) within the `dataviz_script.js` content placeholder.
3.  **Inject JSON Data:**
    *   Retrieves the JSON data object provided by the Persona.
    *   Serializes the data into a JSON string if necessary.
    *   Injects the JSON string directly into the appropriate JavaScript placeholder within the `dataviz_script.js` content placeholder.
4.  **Assemble HTML:** Performs string replacements on the `dataviz_template.html` content:
    *   Injects the content of `dataviz_styles.css` into `{{CSS_STYLES}}`.
    *   Injects the modified `dataviz_script.js` content (containing the injected Python and JSON) into `{{MAIN_SCRIPT}}`.
    *   Injects the original Python script content into `{{PYTHON_SCRIPT}}` (for the code viewer modal).
    *   Injects the content of `dataviz_worker.js` into `{{WORKER_SCRIPT}}`.
    *   Injects the JSON data string into `{{JSON_DATA}}` (for the data viewer modal).
5.  **Output:** The result is a complete HTML content string (`finalHtml`) representing the `viz.html` artifact, ready to be passed to the Client Adapter for delivery.

## 4. The Dataviz Template Structure

The structure relies on several template files assembled at runtime:

*   **`dataviz_template.html`:** The main HTML structure containing placeholders for CSS, main JS, Python script, worker script, and JSON data.
*   **`dataviz_styles.css`:** Contains all the CSS rules.
*   **`dataviz_script.js`:** The main JavaScript logic. Contains placeholders for embedding the specific Python script and JSON data provided by the Persona.
*   **`dataviz_plotly_script.py`:** (Or similar) The template Python script structure, including standard imports and logic to execute code provided by the Persona.
*   **`dataviz_worker.js`:** The web worker script that initializes Pyodide and runs the Python script.

Key components within the assembled HTML:

*   **HTML Boilerplate:** Standard HTML5 structure.
*   **CDN Links:** `<script>` tags to load Pyodide and Plotly.js libraries.
*   **CSS:** Styling for loading indicators, output areas, error messages, export buttons, and modals.
*   **HTML Structure:** `div` elements for displaying loading status, the visualization output (`#output-area`), error messages (`#error-area`), export/view buttons, and modals.
*   **JavaScript Logic (`dataviz_script.js` Content):
    *   **Embedded Content:** Reads the Python script, JSON data, and worker script content from embedded `<script>` tags within the final HTML.
    *   **Pyodide Initialization:** Code to load the Pyodide runtime via a Web Worker created from a Blob URL derived from the embedded worker script content.
    *   **Worker Communication:** Uses `postMessage` to send the Python script and data to the worker and receive results.
    *   **DOM Manipulation:** Handles UI updates (loading, output, errors, modals).
    *   **Export/View Functions:** Event handlers for buttons (Export PNG/SVG/HTML, View Code/Data/Logs).
*   **Embedded Worker Script (`dataviz_worker.js` Content):
    *   Initializes Pyodide, installs required packages (like `micropip`).
    *   Receives Python code and JSON data via `onmessage`.
    *   Executes the Python code, providing access to the data.
    *   Uses `postMessage` to send results (or errors) back to the main thread.
*   **Embedded Python Script (`dataviz_plotly_script.py` Content):
    *   Provides the structure for the Python code executed by the worker.
    *   Includes standard imports (`pyodide_http`, `micropip`, `pandas`, `plotly.express`, etc.).
    *   Loads the JSON data passed from the worker's `onmessage` handler.
    *   Executes the specific visualization logic provided by the Persona.
    *   Assigns the result to a standard variable (e.g., `plotly_figure`).
    *   Renders the figure to HTML/JSON for sending back to the main thread.

## 5. Security Considerations

Security is handled through multiple layers inherent in the design:

*   **Sandboxing:** Pyodide runs within the WebAssembly sandbox, further constrained by the browser's iframe sandbox (configured by the adapter).
*   **Controlled Execution:** The Python code runs within a predefined template structure, limiting the scope of the AI-generated portion.
*   **No Direct DOM Access:** Python code communicates results back to JavaScript via `postMessage`; it doesn't directly manipulate the host page DOM.
*   **Web Worker Isolation:** Running Pyodide in a worker prevents long-running scripts from freezing the UI and allows the main thread to potentially terminate the worker if it exceeds a timeout.
*   **Content Security Policy (CSP):** The adapter serving the `viz.html` must implement a strict CSP to control resource loading (scripts, styles) and prevent unauthorized network connections (`connect-src`).

## 6. Relationship to Client Adapters

While the *request* for a visualization originates from a Persona's analysis, and the **artifact generation** (populating the templates) occurs within the **Nucleus.Processing** layer, the **delivery mechanism** (e.g., Teams Task Modules, saving/displaying a file via the Console Adapter) resides within the **specific Client Adapter** (e.g., `Nucleus.Adapters.Teams`, `Nucleus.Adapters.Console`) responsible for the user interaction context. The Adapter receives the fully formed HTML string from the Processing layer and handles its presentation according to platform capabilities and APIs (like Graph API for potential temporary storage if needed for specific platform mechanisms).