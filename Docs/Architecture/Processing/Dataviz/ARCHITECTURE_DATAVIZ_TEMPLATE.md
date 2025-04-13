---
title: Data Visualization Template
description: Specifies a skill that can be performed by personas, which involves writing structured data and simple visualization code snippets into a template pyodide-based static HTML page.
version: 1.0
date: 2025-04-13
---

# Delivering AI-Generated Pyodide Visualizations in Teams via Self-Contained HTML Artifacts

## 1. Introduction

This report details an architecture for generating and delivering interactive, AI-driven data visualizations within Microsoft Teams. It builds upon the previous analysis, incorporating the requirements for:

1. **Self-Contained Artifact:** Generating a single `viz.html` file containing all necessary HTML, CSS (branded), JavaScript, the AI-generated Python script, and the associated data.
2. **Teams/SharePoint Storage:** Storing this `viz.html` file as a persistent artifact within the relevant Team's SharePoint document library (specifically in a `.Nucleus/Artifacts/` folder).
3. **Task Module Delivery:** Using a Teams Task Module (Dialog) to present the interactive visualization to the user, launched from a bot message.
4. **Integrated Hosting:** Minimizing external dependencies by integrating the necessary components for delivering the HTML content to the Task Module within the C# Bot Framework application itself, aiming for a single deployable unit.

The core idea is to leverage the C# bot to orchestrate the creation and storage of a fully functional, interactive HTML visualization file, and then reliably deliver its content for rendering within the Teams UI.

## 2. Core Artifact: The `viz.html` Template

The foundation of this approach is a well-structured HTML template (`template.html`) that the C# bot will populate. This template includes placeholders for branding, the Python script, and the data, along with the necessary JavaScript to load Pyodide and render the visualization.

**`template.html` Structure:**

HTML

```
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Interactive Visualization</title>

    <script src="https://cdn.jsdelivr.net/pyodide/v0.25.1/full/pyodide.js"></script>
    <script src="https://cdn.plot.ly/plotly-latest.min.js"></script>

    <style>
        body { font-family: sans-serif; margin: 15px; background-color: #f4f4f4; }
        #loading-indicator { text-align: center; padding: 20px; font-style: italic; color: #555; }
        #output-area { background-color: #fff; padding: 10px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); min-height: 300px; }
        #error-area { color: red; margin-top: 10px; white-space: pre-wrap; font-family: monospace; background-color: #ffebeb; padding: 10px; border-radius: 3px; }
        #export-buttons { margin-top: 15px; text-align: right; }
        #export-buttons button { margin-left: 5px; padding: 8px 12px; cursor: pointer; }

        /* BRANDING_CSS - Placeholder for custom styles */
        </style>
</head>
<body>
    <div id="loading-indicator">Loading Visualization Engine...</div>
    <div id="output-area" style="display: none;">
        </div>
    <div id="error-area" style="display: none;">
        </div>
    <div id="export-buttons" style="display: none;">
        <button id="export-png" style="display: none;">Export PNG</button>
        <button id="export-svg" style="display: none;">Export SVG</button>
        <button id="export-html" style="display: none;">Export HTML</button>
    </div>

    <script>
        // --- Data and Python Script Placeholders ---
        // These will be replaced by the C# backend during generation.
        // IMPORTANT: Ensure proper escaping when injecting Python code into the JS string literal.
        // IMPORTANT: Ensure JSON data forms a valid JavaScript object.

        const pythonScript = `
# === PYTHON SCRIPT TEMPLATE (Embedded in JS) ===
# --- Fixed Preamble ---
import js # For JS interop
import pyodide # Or specific modules like pyodide.http if needed
import micropip # For dynamic package loading if needed
import json
import io # For Matplotlib SVG capture
import sys
import traceback
import pandas as pd
import numpy as np
import plotly
import plotly.express as px
import matplotlib
matplotlib.use('AGG') # Use non-interactive backend suitable for WASM/SVG export
import matplotlib.pyplot as plt
import seaborn as sns
# Try importing UMAP - requires scikit-learn to be loaded
try:
    from sklearn.manifold import UMAP
    _UMAP_AVAILABLE = True
except ImportError:
    js.console.warn("scikit-learn (for UMAP) not loaded or UMAP unavailable.")
    _UMAP_AVAILABLE = False

# --- Communication Hooks (Provided by Host JavaScript via Pyodide Worker) ---
# These functions are called via js.<function_name>(...)
# js.pyodideWorker.postMessage({ type: 'completion', payload: { output: output_content_string, type: output_type_string } });
# js.pyodideWorker.postMessage({ type: 'error', payload: error_message_string });
# js.pyodideWorker.postMessage({ type: 'progress', payload: progress_float_0_to_1 }); # Optional

# --- Helper Functions ---
def render_plotly_to_div_string(fig):
    """Renders a Plotly figure to an HTML div string."""
    if not fig:
        raise ValueError("Plotly figure object is null or invalid.")
    try:
        # Ensure Plotly.js is loaded externally in the host page
        return fig.to_html(full_html=False, include_plotlyjs=False, post_script=None)
    except Exception as e:
        raise RuntimeError(f"Plotly rendering failed: {e}")

def render_matplotlib_to_svg_string(fig):
    """Renders a Matplotlib figure to an SVG string and closes the figure."""
    if not fig:
        raise ValueError("Matplotlib figure object is null or invalid.")
    svg_buffer = io.StringIO()
    try:
        fig.savefig(svg_buffer, format='svg', bbox_inches='tight')
        plt.close(fig) # CRITICAL: Close figure to release WASM memory
        js.console.log("Matplotlib figure closed successfully.")
        return svg_buffer.getvalue()
    except Exception as e:
        try:
            plt.close(fig)
        except:
            pass # Ignore errors during cleanup
        raise RuntimeError(f"Matplotlib SVG rendering failed: {e}")
    finally:
        svg_buffer.close()

# --- Data Loading ---
# Data is expected to be available in the JavaScript global scope as 'jsonData'
data_frame = None
raw_data_py = None
try:
    js.pyodideWorker.postMessage({ type: 'progress', payload: 0.1 });
    # Access data passed from JavaScript
    raw_data_js = js.jsonData
    if not raw_data_js:
        raise ValueError("'jsonData' object not found in JavaScript scope.")

    # Convert JS object to Python object
    raw_data_py = raw_data_js.to_py()

    # Create DataFrame (assuming common structure like {'data': [...]})
    if isinstance(raw_data_py, dict) and 'data' in raw_data_py:
         data_frame = pd.DataFrame(raw_data_py['data'])
    elif isinstance(raw_data_py, list): # Handle list of dicts directly
         data_frame = pd.DataFrame(raw_data_py)
    else:
         raise TypeError("Unsupported data structure in jsonData. Expected dict with 'data' key or list of dicts.")

    js.console.log("Data loaded and parsed successfully into DataFrame.")
    js.pyodideWorker.postMessage({ type: 'progress', payload: 0.2 });
except Exception as e:
    error_msg = f"Data loading/parsing error: {e}\\n{traceback.format_exc()}"
    js.console.error(error_msg)
    js.pyodideWorker.postMessage({ type: 'error', payload: error_msg });
    raise # Halt execution

# === AI-GENERATED CODE PLACEHOLDER ===
# Instructions for AI:
# 1. Write Python code ONLY between the START and END markers below.
# 2. Assume common libraries (pandas as pd, numpy as np, plotly.express as px,
#    matplotlib.pyplot as plt, seaborn as sns) are imported.
# 3. Assume data is in a pandas DataFrame named 'data_frame'.
# 4. Perform visualization logic ONLY. Avoid file I/O, network calls, direct DOM manipulation.
# 5. Assign the final Plotly figure object to 'final_fig_plotly'.
# 6. OR assign the final Matplotlib/Seaborn figure object to 'final_fig_matplotlib'.
# 7. DO NOT assign both figure variables.
# 8. UMAP is available via sklearn.manifold.UMAP if _UMAP_AVAILABLE is True.
# 9. Optionally call js.pyodideWorker.postMessage({ type: 'progress', payload: <value> }) for long steps.

final_fig_plotly = None
final_fig_matplotlib = None

try:
    js.pyodideWorker.postMessage({ type: 'progress', payload: 0.3 });

    ### START AI GENERATED CODE ###

    # Example: Simple Plotly scatter plot
    final_fig_plotly = px.scatter(data_frame, x=data_frame.columns, y=data_frame.columns[1],
                                  title='AI Generated Plot')

    # Example: Simple Seaborn histogram (requires uncommenting matplotlib/seaborn imports above)
    # final_fig_matplotlib, ax = plt.subplots(figsize=(6, 4))
    # sns.histplot(data=data_frame, x=data_frame.columns, ax=ax)
    # ax.set_title('AI Generated Histogram')

    # Example: UMAP (requires scikit-learn loaded, check _UMAP_AVAILABLE)
    # if _UMAP_AVAILABLE and len(data_frame.columns) >= 2:
    #     reducer = UMAP(n_components=2)
    #     embedding = reducer.fit_transform(data_frame)
    #     embedding_df = pd.DataFrame(embedding, columns=['UMAP_1', 'UMAP_2'])
    #     # Add labels if available, e.g., from raw_data_py['labels']
    #     # embedding_df['label'] = raw_data_py.get('labels', ['default']*len(embedding_df))
    #     final_fig_plotly = px.scatter(embedding_df, x='UMAP_1', y='UMAP_2', title='UMAP Projection')
    # else:
    #     js.console.warn("UMAP not available or insufficient data columns for UMAP example.")
    #     # Fallback plot maybe?
    #     final_fig_plotly = px.scatter(data_frame, x=data_frame.columns, y=data_frame.columns[1], title='Fallback Scatter')


    ### END AI GENERATED CODE ###

    js.console.log("AI-generated code block executed successfully.")
    js.pyodideWorker.postMessage({ type: 'progress', payload: 0.8 });

except Exception as e:
    error_msg = f"Error during AI code execution: {e}\\n{traceback.format_exc()}"
    js.console.error(error_msg)
    js.pyodideWorker.postMessage({ type: 'error', payload: error_msg });
    raise # Halt execution

# --- Fixed Postamble ---
try:
    output_content = None
    output_type = None # 'plotly' or 'svg'

    if final_fig_plotly is not None:
        js.console.log("Rendering Plotly figure...")
        output_content = render_plotly_to_div_string(final_fig_plotly)
        output_type = 'plotly'
        js.console.log("Plotly rendering complete.")
    elif final_fig_matplotlib is not None:
        js.console.log("Rendering Matplotlib/Seaborn figure to SVG...")
        output_content = render_matplotlib_to_svg_string(final_fig_matplotlib)
        output_type = 'svg'
        js.console.log("Matplotlib/Seaborn SVG rendering complete.")
    else:
        raise ValueError("AI code executed but did not produce a 'final_fig_plotly' or 'final_fig_matplotlib' object.")

    # Signal completion to JavaScript host (via worker message)
    js.pyodideWorker.postMessage({ type: 'completion', payload: { output: output_content, type: output_type } });
    js.pyodideWorker.postMessage({ type: 'progress', payload: 1.0 });
    js.console.log("Execution finished successfully.")

except Exception as e:
    # Catch errors from rendering or the ValueError above, or re-raised errors
    error_msg = f"Finalization/Rendering error: {e}\\n{traceback.format_exc()}"
    js.console.error(error_msg)
    js.pyodideWorker.postMessage({ type: 'error', payload: error_msg }); # Report error to host

finally:
    # Final cleanup
    del data_frame
    del raw_data_py
    del final_fig_plotly
    del final_fig_matplotlib
    js.console.log("Pyodide script execution sequence finished.");

# === END OF PYTHON SCRIPT ===
`; // End of pythonScript template literal

        const jsonData = /* JSON_DATA_PLACEHOLDER */ {}; // Placeholder for data object

        // --- Pyodide Initialization and Execution (using Web Worker) ---
        const loadingIndicator = document.getElementById('loading-indicator');
        const outputArea = document.getElementById('output-area');
        const errorArea = document.getElementById('error-area');
        const exportButtonsDiv = document.getElementById('export-buttons');
        const exportPngButton = document.getElementById('export-png');
        const exportSvgButton = document.getElementById('export-svg');
        const exportHtmlButton = document.getElementById('export-html');

        let pyodideWorker = null;
        let currentOutputType = null; // 'plotly' or 'svg'
        let currentOutputContent = null; // HTML or SVG string

        function displayError(message) {
            console.error("Pyodide/Python Error:", message);
            loadingIndicator.style.display = 'none';
            outputArea.style.display = 'none';
            exportButtonsDiv.style.display = 'none';
            errorArea.textContent = `Error:\n${message}`;
            errorArea.style.display = 'block';
        }

        function displayOutput(output, type) {
            currentOutputType = type;
            currentOutputContent = output;
            loadingIndicator.style.display = 'none';
            errorArea.style.display = 'none';
            outputArea.innerHTML = output; // Inject the HTML/SVG
            outputArea.style.display = 'block';
            exportButtonsDiv.style.display = 'block';

            // Show relevant export buttons
            exportSvgButton.style.display = (type === 'plotly' |
| type === 'svg')? 'inline-block' : 'none';
            exportPngButton.style.display = (type === 'plotly')? 'inline-block' : 'none'; // PNG export easier with Plotly.js
            exportHtmlButton.style.display = (type === 'plotly')? 'inline-block' : 'none'; // HTML export makes sense for interactive Plotly
        }

        function updateProgress(value) {
            // Basic progress update - could be enhanced with a progress bar
            if (value < 1.0) {
                loadingIndicator.textContent = `Executing Python (${Math.round(value * 100)}%)...`;
            } else {
                 loadingIndicator.textContent = `Rendering...`;
            }
        }

        // --- Export Functions ---
        function triggerDownload(blob, filename) {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }

        exportPngButton.onclick = () => {
            if (currentOutputType === 'plotly') {
                const plotDiv = outputArea.querySelector('.plotly-graph-div'); // Find the Plotly div
                if (plotDiv) {
                    Plotly.downloadImage(plotDiv, { format: 'png', filename: 'visualization.png', width: plotDiv.clientWidth, height: plotDiv.clientHeight });
                } else {
                    console.error("Could not find Plotly graph div for PNG export.");
                }
            }
        };

        exportSvgButton.onclick = () => {
            let svgContent = '';
            if (currentOutputType === 'svg') {
                svgContent = currentOutputContent; // We already have the SVG string
            } else if (currentOutputType === 'plotly') {
                 const plotDiv = outputArea.querySelector('.plotly-graph-div');
                 if (plotDiv) {
                    // Use Plotly's SVG export
                    Plotly.downloadImage(plotDiv, { format: 'svg', filename: 'visualization.svg', width: plotDiv.clientWidth, height: plotDiv.clientHeight });
                    return; // Plotly handles the download directly
                 } else {
                     console.error("Could not find Plotly graph div for SVG export.");
                     return;
                 }
            }

            if (svgContent) {
                const blob = new Blob([svgContent], { type: 'image/svg+xml;charset=utf-8' });
                triggerDownload(blob, 'visualization.svg');
            }
        };

        exportHtmlButton.onclick = () => {
            if (currentOutputType === 'plotly' && currentOutputContent) {
                // Reconstruct a minimal self-contained HTML for the Plotly chart
                const plotDataScript = currentOutputContent.match(/<script type="text\/javascript">window\.PLOTLYENV=.*Plotly\.newPlot\((.*)\);<\/script>/s);
                if (plotDataScript && plotDataScript[1]) {
                    const plotArgs = plotDataScript[1]; // Extracts the arguments to Plotly.newPlot
                    const plotDivId = currentOutputContent.match(/<div id="([^"]*)" class="plotly-graph-div"/)[1];

                    const htmlContent = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Interactive Plot</title>
    <script src="https://cdn.plot.ly/plotly-latest.min.js"><\/script>
    <style>body { margin: 0; }</style>
</head>
<body>
    <div id="${plotDivId}" style="width:100%; height:100vh;"></div>
    <script type="text/javascript">
        Plotly.newPlot(${plotArgs});
    <\/script>
</body>
</html>`;
                    const blob = new Blob([htmlContent], { type: 'text/html;charset=utf-8' });
                    triggerDownload(blob, 'interactive_visualization.html');
                } else {
                    console.error("Could not extract Plotly data for HTML export.");
                }
            }
        };


        // --- Worker Setup and Main Execution ---
        async function main() {
            loadingIndicator.textContent = 'Loading Pyodide Environment...';
            try {
                // Create a worker
                const workerCode = `
                    importScripts("https://cdn.jsdelivr.net/pyodide/v0.25.1/full/pyodide.js");

                    let pyodideInstance = null;

                    async function loadPyodideAndPackages() {
                        self.pyodideInstance = await loadPyodide();
                        // Pre-load common/heavy packages
                        await self.pyodideInstance.loadPackage(['pandas', 'numpy', 'plotly', 'matplotlib', 'seaborn', 'scikit-learn']);
                        // Make worker available to Python via js module
                        self.pyodideInstance.globals.set('pyodideWorker', self);
                        return true;
                    }

                    let pyodideReadyPromise = loadPyodideAndPackages();

                    self.onmessage = async (event) => {
                        await pyodideReadyPromise; // Ensure Pyodide is loaded

                        const { pythonCode, data } = event.data;

                        // Make data available to Python
                        self.pyodideInstance.globals.set('jsonData', data);

                        try {
                            // Execute the Python script
                            await self.pyodideInstance.runPythonAsync(pythonCode);
                        } catch (error) {
                            // Catch Python execution errors not handled internally
                            console.error("Error in pyodide.runPythonAsync:", error);
                            self.postMessage({ type: 'error', payload: \`Worker Execution Error: \${error}\` });
                        } finally {
                            // Clean up global scope in Pyodide if needed
                            self.pyodideInstance.globals.delete('jsonData');
                        }
                    };
                `;

                const blob = new Blob([workerCode], { type: 'application/javascript' });
                pyodideWorker = new Worker(URL.createObjectURL(blob));

                pyodideWorker.onmessage = (event) => {
                    const { type, payload } = event.data;
                    if (type === 'completion') {
                        displayOutput(payload.output, payload.type);
                    } else if (type === 'error') {
                        displayError(payload);
                    } else if (type === 'progress') {
                        updateProgress(payload);
                    } else {
                        console.warn("Received unknown message type from worker:", type);
                    }
                };

                pyodideWorker.onerror = (error) => {
                    displayError(`Worker error: ${error.message}`);
                    console.error("Worker Error Event:", error);
                };

                // Start execution by sending code and data to worker
                loadingIndicator.textContent = 'Sending script to execution engine...';
                pyodideWorker.postMessage({ pythonCode: pythonScript, data: jsonData });

            } catch (error) {
                displayError(`Initialization failed: ${error}`);
            }
        }

        main(); // Start the process

    </script>
</body>
</html>
```

**Key Features of the Template:**

- **Single File:** Designed to be self-contained once placeholders are filled.
- **CDN Dependencies:** Loads Pyodide and Plotly.js from CDNs.
- **Placeholders:** Clear markers (``, `/* PYTHON_SCRIPT_PLACEHOLDER */`, `/* JSON_DATA_PLACEHOLDER */`) for backend injection.
- **Branding:** Allows injection of custom CSS.
- **Web Worker:** Runs Pyodide and Python execution in a background thread to keep the UI responsive.
- **Embedded Python Template:** Includes the secure Python structure within a JS string literal.
    - **Pre-approved Imports:** Includes `pandas`, `numpy`, `plotly`, `matplotlib`, `seaborn`, and `sklearn.manifold` (for UMAP). AI cannot add other imports easily.
    - **Data Access:** Python code accesses data via `js.jsonData.to_py()`.
    - **Clear AI Section:** Confines AI generation.
    - **Error Handling & Communication:** Uses `postMessage` to communicate completion, errors, or progress back to the main thread.
    - **Resource Management:** Includes `plt.close(fig)` for Matplotlib.
- **JS Logic:** Handles worker communication, DOM updates for output/errors, and client-side export functionality.

## 3. Backend Implementation (C# Bot Framework Application)

The C# bot application (deployable as a single Azure App Service or Container App) handles the orchestration.

**3.1. Artifact Generation:**

C#

```
using System.Text;
using System.Text.Json; // Requires System.Text.Json nuget package
using System.Text.RegularExpressions; // For escaping

//... within your bot logic (e.g., inside a Dialog or Activity Handler)

// 1. Load the HTML template content
string templateHtml = await File.ReadAllTextAsync("./path/to/your/template.html"); // Adjust path

// 2. Get Branding CSS (e.g., from config)
string brandingCss = "/* Your custom CSS rules here */\n.plotly-graph-div { border: 1px solid #ccc; }"; // Example

// 3. Get AI-generated Python code snippet and JSON data
string aiPythonSnippet = @"
# Example AI code:
final_fig_plotly = px.histogram(data_frame, x=data_frame.columns, title='AI Histogram')
js.pyodideWorker.postMessage({ type: 'progress', payload: 0.6 });
"; // Replace with actual AI output

object aiDataObject = new { // Replace with actual data structure
    data = new {
        new { category = "A", value = 10 },
        new { category = "B", value = 25 },
        new { category = "C", value = 15 }
    },
    metadata = new { title = "Sample Data" }
};
string aiJsonDataString = JsonSerializer.Serialize(aiDataObject, new JsonSerializerOptions { WriteIndented = true });

// 4. Inject into template
string finalHtml = templateHtml;

// Inject CSS
finalHtml = finalHtml.Replace("", brandingCss);

// Inject Python (escape backticks, backslashes, ${} for JS template literals)
string escapedPythonSnippet = aiPythonSnippet
   .Replace("\\", "\\\\") // Escape backslashes
   .Replace("`", "\\`")  // Escape backticks
   .Replace("${", "\\${"); // Escape ${ sequence
finalHtml = finalHtml.Replace("### START AI GENERATED CODE ###", "### START AI GENERATED CODE ###\n" + escapedPythonSnippet); // Inject within markers

// Inject JSON data
finalHtml = finalHtml.Replace("/* JSON_DATA_PLACEHOLDER */ {}", $"/* JSON_DATA_PLACEHOLDER */ {aiJsonDataString}");

// 'finalHtml' string now contains the complete, ready-to-use HTML content
```

**3.2. SharePoint Storage (using Microsoft.Graph SDK):**

- **Permissions:** Ensure your bot's Azure AD App Registration has `Sites.Selected` Graph API permission (Application type) granted by an admin. Then, grant specific Write permissions to the bot's service principal for the target SharePoint site(s) using the `/sites/{site-id}/permissions` endpoint (this usually requires a separate script or process run by an admin).
- **C# Code Snippet:**

C#

```
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity; // For authentication
using System.IO;
using System.Text;

//... (Assuming you have GraphServiceClient initialized, e.g., 'graphClient')
//... (Assuming you have 'teamId' or 'groupId' for the context)

async Task StoreArtifactInSharePoint(GraphServiceClient graphClient, string groupId, string uniqueId, string htmlContent)
{
    try
    {
        // 1. Get the Team's root site
        var site = await graphClient.Groups[groupId].Sites["root"].GetAsync();
        if (site?.Id == null) throw new Exception("Could not find root site for group.");

        // 2. Get the default document library (Drive)
        var drive = await graphClient.Sites[site.Id].Drive.GetAsync();
        if (drive?.Id == null) throw new Exception("Could not find default drive for site.");

        // 3. Define the target path and filename
        string fileName = $"viz-{uniqueId}.html";
        string targetFolderPath = ".Nucleus/Artifacts"; // Ensure this folder exists or handle creation
        string targetPath = $"{targetFolderPath}/{fileName}"; // Relative to drive root

        // 4. Prepare the content stream
        byte htmlBytes = Encoding.UTF8.GetBytes(htmlContent);
        using var contentStream = new MemoryStream(htmlBytes);

        // 5. Upload the file
        // PUT /drives/{drive.Id}/items/root:/{targetPath}:/content
        var uploadedItem = await graphClient.Drives[drive.Id].Items["root"].ItemWithPath(targetPath).Content.PutAsync(contentStream);

        if (uploadedItem!= null)
        {
            Console.WriteLine($"Artifact stored successfully: {uploadedItem.WebUrl}");
            // Optionally return uploadedItem.WebUrl or ID
        }
        else
        {
            Console.WriteLine("Failed to store artifact in SharePoint.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error storing artifact in SharePoint: {ex.Message}");
        // Handle error appropriately
    }
}

// --- Usage ---
// string uniqueVizId = Guid.NewGuid().ToString();
// await StoreArtifactInSharePoint(graphClient, teamId, uniqueVizId, finalHtml);
```

**3.3. Task Module Delivery & Integrated Hosting:**

- **Caching:** Use `IMemoryCache` for simplicity in a single-instance deployment or Redis for multi-instance.

C#

```
using Microsoft.Extensions.Caching.Memory; // Add NuGet package

//... inject IMemoryCache cache...

// Store HTML in cache
string vizId = Guid.NewGuid().ToString();
cache.Set(vizId, finalHtml, TimeSpan.FromMinutes(15)); // Cache for 15 mins

// --- In Bot Activity Handler (e.g., OnMessageActivityAsync) ---
// Send card with button like this:
var card = new HeroCard
{
    Title = "Visualization Ready",
    Text = "Click below to view the interactive visualization.",
    Buttons = new List<CardAction> {
        new TaskModuleAction("View Interactive", new { msteams = new { type = "task/fetch" }, data = new { vizId = vizId } })
        // Optionally add a direct link to the SharePoint file if stored
        // new CardAction(ActionTypes.OpenUrl, "View Artifact File", value: sharePointFileWebUrl)
    }
};
await turnContext.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);


// --- In Bot Adapter or Controller (handle task/fetch) ---
// Implement OnTeamsTaskModuleFetchAsync or similar logic
protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
{
    var data = JObject.FromObject(taskModuleRequest.Data);
    string fetchedVizId = data["vizId"]?.ToString();

    if (!string.IsNullOrEmpty(fetchedVizId) && cache.TryGetValue(fetchedVizId, out string _)) // Check if ID is valid and in cache
    {
        // Construct URL to the endpoint within *this* bot application
        // Ensure your bot's base URL is correctly configured (e.g., from settings)
        string renderUrl = $"{_botBaseUrl}/api/renderViz?id={fetchedVizId}";

        return new TaskModuleResponse
        {
            Task = new TaskModuleContinueResponse
            {
                Value = new TaskModuleTaskInfo
                {
                    Url = renderUrl,
                    Height = 600, // Adjust size as needed
                    Width = 800,
                    Title = "Interactive Visualization"
                }
            }
        };
    }
    // Handle error: vizId not found or invalid
    //... return error task module...
}

// --- Add Minimal API Endpoint in Program.cs (or a Controller) ---
// In your Bot's Program.cs or Startup.cs
//... other services...
builder.Services.AddMemoryCache(); // Add memory cache service

var app = builder.Build();

//... other middleware...

// Endpoint to serve the cached HTML
app.MapGet("/api/renderViz", (string id, IMemoryCache cache, ILogger<Program> logger) =>
{
    if (!string.IsNullOrEmpty(id) && cache.TryGetValue(id, out string htmlContent))
    {
        logger.LogInformation($"Serving visualization content for ID: {id}");
        // Set CSP Header - IMPORTANT!
        // Adjust directives as needed based on libraries and security posture
        var cspHeader = "default-src 'none'; " +
                        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net/pyodide/ https://cdn.plot.ly/; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data:; " +
                        "font-src * data:; " + // Allow fonts from anywhere + data URIs
                        "connect-src 'self'; " + // Only allow connections back to self (if needed by JS, unlikely here)
                        "worker-src 'self' blob:; " + // Allow worker scripts from self and blob URLs
                        "frame-ancestors https://teams.microsoft.com https://*.teams.microsoft.com https://*.cloud.microsoft;"; // Allow embedding in Teams

        return Results.Content(htmlContent, "text/html", System.Text.Encoding.UTF8)
                     .WithHeader("Content-Security-Policy", cspHeader);
    }
    else
    {
        logger.LogWarning($"Visualization content not found or expired for ID: {id}");
        return Results.NotFound("Visualization not found or has expired.");
    }
});

// Map bot framework adapter endpoint (e.g., /api/messages)
//...

app.Run();
```

## 4. Security Considerations

- **Python Template:** The strict structure, pre-defined imports, and clear AI code boundaries are crucial.
- **CSP Header:** The `Content-Security-Policy` header set by the `/api/renderViz` endpoint is vital for mitigating XSS, restricting network connections, and ensuring the content can only be framed by Teams.
- **Web Worker:** Isolates Pyodide execution, preventing UI freezes and enabling potential timeouts (though explicit timeout logic isn't shown in the snippet, it can be added around the worker interaction).
- **Graph Permissions:** Use `Sites.Selected` for least privilege access to SharePoint.
- **Input Validation:** The bot backend should ideally perform basic validation on the AI-generated Python snippet (e.g., checking for obviously malicious patterns, though this is hard) and the size/structure of the JSON data before generating the final HTML.

## 5. Deployment

The C# Bot Framework application, now containing the bot logic (`/api/messages`), artifact generation, SharePoint upload logic, caching, and the HTML serving endpoint (`/api/renderViz`), can be deployed as a single unit to Azure App Service or Azure Container Apps. Ensure the service has network access to Microsoft Graph and potentially Redis (if used).

## 6. Conclusion

This architecture provides a robust method for delivering AI-generated, interactive Python visualizations within Teams. It creates a self-contained, branded HTML artifact stored persistently in SharePoint, while ensuring reliable interactive delivery via a Task Module powered by an integrated endpoint within the bot application itself. This approach balances functionality, security, and deployment simplicity.