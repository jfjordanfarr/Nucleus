/*
Nucleus Dataviz Frontend Script

- See architecture: [ARCHITECTURE_PROCESSING_DATAVIZ.md](../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md)
- Related: [dataviz_plotly_script.py](./dataviz_plotly_script.py), [DatavizHtmlBuilder.cs](../Services/DatavizHtmlBuilder.cs)
*/

// --- Global State Variables ---
let pyodideWorker = null;
let currentOutputContent = null;
let currentOutputType = null; // 'plotly', 'svg', 'message', or 'error'
let workerLogs = []; // Array to store logs from the worker
let currentPlotData = null; // Store Plotly data for export
let currentPlotLayout = null; // Store Plotly layout for export
let actualJsonData = null; // Store the actual JSON data received

// Global element references - declared here, assigned in initialization
let loadingIndicator = null;
let errorArea = null;
let plotArea = null; // Added reference for direct plot area targeting
let exportButtonsDiv = null;

// --- DOM Element References ---
const outputArea = document.getElementById('output-area');
const exportPngButton = document.getElementById('export-png');
const exportSvgButton = document.getElementById('export-svg');
const exportHtmlButton = document.getElementById('export-html');

// Add references for the modal
const viewCodeButton = document.getElementById('view-python-code');
const codeModal = document.getElementById('code-modal');
const closeModalButton = document.getElementById('close-modal');
const pythonCodeDisplay = document.getElementById('python-code-display');

// Add references for the data modal
const viewDataButton = document.getElementById('view-data');
const dataModal = document.getElementById('data-modal');
const closeDataModalButton = document.getElementById('close-data-modal');
const jsonDataDisplay = document.getElementById('json-data-display');

// Add references for the worker log modal
const viewWorkerLogButton = document.getElementById('view-worker-log');
const workerLogModal = document.getElementById('worker-log-modal');
const closeWorkerLogModalButton = document.getElementById('close-worker-log-modal');
const workerLogDisplay = document.getElementById('worker-log-display');
const copyWorkerLogButton = document.getElementById('copy-worker-log-button');

// References for copy buttons
const copyPythonButton = document.getElementById('copy-python-button');
const copyDataButton = document.getElementById('copy-data-button');

// --- Helper Functions ---
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        console.log('Content copied to clipboard');
        // Optional: Add visual feedback (e.g., change button text briefly)
    } catch (err) {
        console.error('Failed to copy content: ', err);
        alert('Failed to copy content to clipboard.');
    }
}

// --- HTML Entity Decoding --- (Added)
function decodeHtmlEntities(text) {
  var textArea = document.createElement('textarea');
  textArea.innerHTML = text;
  return textArea.value;
}

// --- Robust JS String Escaping --- (Restored)
function escapeJsString(str) {
  if (!str) return '';
  // Escape backslashes first, then other characters that could break JS strings or JSON.parse
  return str.replace(/\\/g, '\\\\') // Backslashes
            .replace(/'/g, "\\'")  // Single quotes
            .replace(/"/g, '\\"') // Double quotes (important for JSON parsing within the string)
            .replace(/\n/g, "\\n") // Newlines
            .replace(/\r/g, "\\r") // Carriage returns
            .replace(/\t/g, "\\t") // Tabs
            .replace(/\f/g, "\\f") // Form feeds
            .replace(/\b/g, "\\b") // Backspaces
            .replace(/\u2028/g, "\\u2028") // Line separator
            .replace(/\u2029/g, "\\u2029"); // Paragraph separator
}

// --- UI Update Functions ---
function displayError(errorMessage) {
    console.error("Main Thread Error:", errorMessage); // Log the raw error
    if (errorArea) {
        errorArea.style.display = 'block';
        // Ensure errorMessage is a string before using string methods
        let displayMessage = "An unexpected error occurred.";
        if (typeof errorMessage === 'string') {
            displayMessage = errorMessage.substring(0, 500) + (errorMessage.length > 500 ? '...' : '');
        } else if (errorMessage && typeof errorMessage.toString === 'function') {
            displayMessage = errorMessage.toString();
        }
        errorArea.textContent = displayMessage;
    }
    if (loadingIndicator) loadingIndicator.style.display = 'none';
    // Ensure plotArea exists before trying to hide it
    if (plotArea) plotArea.style.display = 'none'; // Hide plot area on error
    if (exportButtonsDiv) exportButtonsDiv.style.display = 'none'; // Hide export buttons
}

// --- Plot Rendering and Responsiveness ---
// Plotly is rendered with {responsive: true} (see ARCHITECTURE_PROCESSING_DATAVIZ.md, "Plotly Responsiveness: Lessons Learned").
// This ensures robust resizing on all container/window changes. Manual relayout is fallback only.
// Working example: [EXAMPLE_OUTPUT_nucleus_dataviz_20250416145545.html]
function displayOutput(type, content) {
    console.log(`Main: displayOutput called with type: ${type}`);
    loadingIndicator.style.display = 'none';
    // Ensure the main output area is visible first
    outputArea.style.display = 'block'; 
    errorArea.style.display = 'none';
    exportButtonsDiv.style.display = 'flex';

    // Find the dedicated plot area within the output area
    const plotDiv = document.getElementById('plot-area'); 
    if (!plotDiv) { // Add check for plot area existence
        displayError("Plot area element ('plot-area') not found.");
        return;
    }

    // Clear previous content and ensure visibility
    plotDiv.innerHTML = ''; 
    plotDiv.style.display = 'block'; 

    if (type === 'plotly_json') {
        let plotJson;
        try {
            plotJson = (typeof content === 'string') ? JSON.parse(content) : content;
        } catch (e) {
            displayError('Failed to parse plotly JSON: ' + e.message);
            return;
        }
        // Store for export
        currentPlotData = plotJson.data;
        currentPlotLayout = plotJson.layout || {};
        currentOutputType = 'plotly_json';
        // Store raw string content too, if needed elsewhere (though exports use data/layout)
        currentOutputContent = content; 
        
        // Render plot inside the specific plotDiv
        try {
            Plotly.newPlot(plotDiv, currentPlotData, currentPlotLayout, {responsive: true}).then(() => {
                // Defer layout update slightly to allow rendering
                setTimeout(() => {
                    updatePlotLayout(); // Call resize logic after plot renders
                }, 0);
            });
        } catch (e) {
            displayError('Failed to render Plotly chart: ' + e.message);
            return; // Stop if rendering fails
        }
        updateExportButtonStates('plotly_json');
    } else if (type === 'svg') {
        currentOutputType = 'svg';
        currentOutputContent = content;
        plotDiv.innerHTML = content; // Put SVG content directly in plotDiv
        updateExportButtonStates('svg');
    } else if (type === 'message') {
        currentOutputType = 'message';
        currentOutputContent = content;
        plotDiv.textContent = content; // Show text message in plotDiv
        updateExportButtonStates('message');
    } else if (type === 'error') {
        displayError(content);
        currentOutputType = 'error';
        currentOutputContent = content;
        plotDiv.style.display = 'none'; // Hide plot area specifically on error display
        updateExportButtonStates('error');
    } else {
        // Handle unknown type in the plotDiv
        plotDiv.textContent = `Unknown output type: ${type}. Content: ${content}`;
        currentOutputType = null;
        currentOutputContent = null;
        updateExportButtonStates(null);
    }
    // Worker Log button visibility check - Moved outside specific types
    if (workerLogs.length > 0) {
        viewWorkerLogButton.style.display = 'inline-block';
    }
}

// --- ResizeObserver and Plotly Interaction ---
function updatePlotLayout() {
    // Ensure plotArea is valid, visible, Plotly exists, and the plot has been rendered inside
    if (plotArea && plotArea.offsetParent !== null && typeof Plotly !== 'undefined' && plotArea.querySelector('.plotly-graph-div')) {
        try {
            const newWidth = plotArea.clientWidth;
            const newHeight = plotArea.clientHeight;
            setTimeout(() => {
                if (newWidth > 0 && newHeight > 0) {
                    Plotly.relayout(plotArea, {
                        width: newWidth,
                        height: newHeight,
                        'xaxis.autorange': true,
                        'yaxis.autorange': true
                    });
                    Plotly.Plots.resize(plotArea);
                }
            }, 0); // Use setTimeout with 0 delay
        } catch (e) {
            console.error('Error during Plotly relayout:', e);
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // --- Assign global element references --- 
    loadingIndicator = document.getElementById('loading-indicator');
    errorArea = document.getElementById('error-area');
    plotArea = document.getElementById('plot-area');
    exportButtonsDiv = document.getElementById('export-buttons');

    // --- Initialization --- (Modified)
    let jsonData = '{{SAFE_JSON_DATA_PLACEHOLDER}}'; // Placeholder replaced by C#

    // Capture the globally defined jsonData (after C# replacement) for the modal
    actualJsonData = jsonData; 

    // Read embedded content
    let pythonScriptContent = null;
    let workerScriptContent = null;

    try {
        // Read worker script from its designated template tag
        const workerScriptElement = document.getElementById('worker-script-template');
        if (!workerScriptElement) throw new Error("Worker script template element not found.");
        workerScriptContent = decodeHtmlEntities(workerScriptElement.innerHTML);

        // Read python script from its designated template tag
        const pythonScriptElement = document.getElementById('python-script-template');
        if (!pythonScriptElement) throw new Error("Python script template element not found.");
        pythonScriptContent = decodeHtmlEntities(pythonScriptElement.innerHTML);

        if (!pythonScriptContent || !workerScriptContent) { 
             throw new Error("Embedded worker script or Python script content is empty.");
        }

        // Start the worker initialization process
        initializePyodideWorker(workerScriptContent, pythonScriptContent, jsonData);

    } catch (e) {
        displayError(`Initialization failed: ${e.message}`);
        return; // Stop further setup if essential parts are missing
    }

    // --- Export Button Handlers (Modified for Plotly JSON) ---
    exportPngButton.addEventListener('click', () => {
        if (currentOutputType === 'plotly_json' && typeof Plotly !== 'undefined' && currentPlotData && currentPlotLayout) {
            const plotDiv = document.getElementById('plot-area');
            Plotly.toImage(plotDiv, { format: 'png', width: plotDiv.clientWidth, height: plotDiv.clientHeight })
                .then(dataUrl => {
                    // Convert data URL to Blob for download
                    fetch(dataUrl)
                        .then(res => res.blob())
                        .then(blob => {
                            const url = URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;
                            a.download = 'plot.png';
                            document.body.appendChild(a);
                            a.click();
                            document.body.removeChild(a);
                            URL.revokeObjectURL(url);
                        });
                })
                .catch(err => {
                    alert('PNG export failed: ' + err.message);
                });
        } else {
            alert('PNG export requires a rendered Plotly chart.');
        }
    });

    exportSvgButton.addEventListener('click', () => {
        if (currentOutputType === 'plotly_json' && typeof Plotly !== 'undefined' && currentPlotData && currentPlotLayout) {
            const plotDiv = document.getElementById('plot-area');
            Plotly.toImage(plotDiv, { format: 'svg', width: plotDiv.clientWidth, height: plotDiv.clientHeight })
                .then(dataUrl => {
                    // Convert data URL to SVG text for download
                    fetch(dataUrl)
                        .then(res => res.text())
                        .then(svgText => {
                            downloadFile('plot.svg', svgText, 'image/svg+xml');
                        });
                })
                .catch(err => {
                    alert('SVG export failed: ' + err.message);
                });
        } else {
            alert('SVG export requires a rendered Plotly chart.');
        }
    });

    // --- Export Logic ---
    // HTML export embeds Plotly JSON and layout directly, escaping only </script> tags.
    // See rationale and details in ARCHITECTURE_PROCESSING_DATAVIZ.md, section "Export Logic & Robustness".
    exportHtmlButton.addEventListener('click', () => {
        if (currentOutputType === 'plotly_json' && currentOutputContent) {
            try {
                // Parse the stored JSON string to get data/layout objects
                const plotJson = JSON.parse(currentOutputContent);
                const plotData = plotJson.data;
                const plotLayout = plotJson.layout;

                // Convert data/layout objects back to JSON strings
                let plotDataString = JSON.stringify(plotData);
                let plotLayoutString = JSON.stringify(plotLayout);

                // Escape any closing script tags in the JSON (prevents HTML parse errors)
                plotDataString = plotDataString.replace(/<\/script>/gi, '<\\/script>');
                plotLayoutString = plotLayoutString.replace(/<\/script>/gi, '<\\/script>');

                // Build HTML content
                const htmlContent = "<!DOCTYPE html>\n"
                    + "<html>\n"
                    + "<head>\n"
                    + "    <meta charset=\"utf-8\" />\n"
                    + "    <title>Exported Plotly Chart</title>\n"
                    + "    <script src='https://cdn.plot.ly/plotly-2.32.0.min.js'><\/script>\n"
                    + "</head>\n"
                    + "<body>\n"
                    + "    <div id='plotly-div'></div>\n"
                    + "    <script>\n"
                    + "        try {\n"
                    + "            var data = " + plotDataString + ";\n"
                    + "            var layout = " + plotLayoutString + ";\n"
                    + "            Plotly.newPlot('plotly-div', data, layout);\n"
                    + "        } catch (e) { \n"
                    + "            console.error('Error rendering exported plot:', e); \n"
                    + "            document.body.innerHTML = '<pre>Error rendering plot: ' + e.message + '</pre>'; \n"
                    + "        } \n"
                    + "    <\/script>\n"
                    + "</body>\n"
                    + "</html>";

                downloadFile('visualization.html', htmlContent, 'text/html');
            } catch (e) {
                console.error("Error creating export HTML:", e);
                alert('Failed to create HTML export from plot data.');
            }
        } else {
            alert('HTML export requires rendered Plotly chart data.');
        }
    });

    // --- View Code Button Handler ---
    viewCodeButton.addEventListener('click', () => {
        if (pythonScriptContent) {
            pythonCodeDisplay.textContent = pythonScriptContent.trim(); // Display the script
            Prism.highlightElement(pythonCodeDisplay); // Apply syntax highlighting
            codeModal.style.display = "block"; // Show the modal
        } else {
            alert("No Python script content found to display.");
        }
    });

    // --- View Data Button Handler ---
    viewDataButton.addEventListener('click', () => {
        try {
            if (actualJsonData) {
                const jsonDataString = JSON.stringify(actualJsonData, null, 2);
                jsonDataDisplay.textContent = jsonDataString;
                Prism.highlightElement(jsonDataDisplay);
                dataModal.style.display = "block";
            } else {
                alert("JSON data is not available.");
            }
        } catch (e) {
            alert(`Error formatting JSON data: ${e.message}`);
        }
    });

    // --- View Worker Log Button Handler ---
    viewWorkerLogButton.addEventListener('click', () => {
        if (workerLogs.length > 0) { // Add check if logs exist
            workerLogDisplay.textContent = workerLogs.join('\n'); // Display logs
            Prism.highlightElement(workerLogDisplay); // Apply syntax highlighting
            workerLogModal.style.display = "block"; // Show the modal
        } else {
            alert("No worker logs available.");
        }
    });

    // --- Modal Close Button Handlers ---
    closeModalButton.addEventListener('click', () => {
        codeModal.style.display = "none"; // Hide the modal
    });

    closeDataModalButton.addEventListener('click', () => {
        dataModal.style.display = "none"; // Hide the modal
    });

    closeWorkerLogModalButton.addEventListener('click', () => {
        workerLogModal.style.display = "none"; // Hide the modal
    });

    // --- Copy Button Handlers ---
    copyPythonButton.addEventListener('click', () => {
        if (pythonScriptContent) copyToClipboard(pythonScriptContent.trim());
    });

    copyDataButton.addEventListener('click', () => {
        try {
            if (actualJsonData) {
                const jsonDataString = JSON.stringify(actualJsonData, null, 2);
                copyToClipboard(jsonDataString);
            } else {
                alert("JSON data is not available.");
            }
        } catch (e) { alert('Failed to copy JSON data.'); }
    });

    copyWorkerLogButton.addEventListener('click', () => {
        if(workerLogs.length > 0) copyToClipboard(workerLogs.join('\n'));
    });

    // Initialize Prism for syntax highlighting if modals are used
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }

    // --- Robust Plot Resizing Logic ---
    window.addEventListener('resize', updatePlotLayout);

    if (window.ResizeObserver && plotArea) {
        const resizeObserver = new ResizeObserver(() => {
            updatePlotLayout();
        });
        resizeObserver.observe(plotArea);
    } else {
        console.warn('ResizeObserver not supported or plotArea not found.');
    }
});

// --- Worker Setup and Communication --- ---
function initializePyodideWorker(workerScriptContent, pythonScriptContent, jsonData) {
    console.log("Main: Initializing Pyodide Worker...");
    if (!workerScriptContent) {
        displayError("Worker script content is missing.");
        return;
    }
    const workerBlob = new Blob([workerScriptContent], { type: 'application/javascript' });
    const workerUrl = URL.createObjectURL(workerBlob);
    pyodideWorker = new Worker(workerUrl);
    pyodideWorker.onmessage = (event) => {
        // --- Capture ALL messages for logging (Verbose) ---
        const messageString = JSON.stringify(event.data); // Log the full data
        workerLogs.push(messageString); 
        console.log(`Main: Received from worker: ${messageString}`);
        // --- Original message parsing for actions ---
        const { type, message, data } = event.data;

        // Show log button if there are logs
        if (workerLogs.length > 0) {
            viewWorkerLogButton.style.display = 'inline-block';
        }

        // --- Handle message actions ---
        switch (type) {
            case 'pyodide_loaded':
                console.log("Main: Pyodide loaded in worker. Sending script and data.");
                loadingIndicator.textContent = 'Executing Python script...';
                pyodideWorker.postMessage({
                    type: 'execute_script',
                    pythonScript: pythonScriptContent,
                    jsonData: jsonData // Use global jsonData here
                });
                break;
            case 'status':
                console.log(`Main: Worker status update: ${message}`);
                loadingIndicator.textContent = message; // Update loading message
                break;
            case 'result':
                console.log("Main: Received plot data from worker.");
                loadingIndicator.style.display = 'none'; // Hide loading indicator
                displayOutput('plotly_json', data);
                break;
            case 'error':
                console.error("Main: Received error from worker:", message);
                displayError(message);
                // workerLogs already captured above
                // viewWorkerLogButton display already handled above
                loadingIndicator.style.display = 'none';
                break;
            default:
                // workerLogs already captured above
                console.warn("Main: Received unknown message type from worker:", type);
        }
    };

    // --- Worker Error Handling ---
    pyodideWorker.onerror = (error) => {
        console.error('Main: Error in Pyodide Worker:', error);
        workerLogs.push(`[ERROR] Worker Error: ${error.message}`); // Log error
        displayError(`Worker error: ${error.message}`);
        if (pyodideWorker) {
            pyodideWorker.terminate(); // Clean up the worker on error
            pyodideWorker = null;
        }
    };

    // Clean up worker URL
    URL.revokeObjectURL(workerUrl);

    // Send initialization message (optional, if worker needs setup before loading pyodide)
    // pyodideWorker.postMessage({ type: 'init' }); 
    console.log("Main: Worker created and message listener attached.");
}

function updateExportButtonStates(outputType) {
    // Enable/disable based on output type and library capabilities (match working example)
    const hasPlotly = typeof Plotly !== 'undefined' && typeof Plotly.toImage === 'function';
    if (exportPngButton) exportPngButton.disabled = !(outputType === 'plotly_json' && hasPlotly);
    if (exportSvgButton) exportSvgButton.disabled = !(outputType === 'plotly_json' && hasPlotly);
    if (exportHtmlButton) exportHtmlButton.disabled = !(outputType === 'plotly_json');
    console.log(`Main: Button states updated - PNG: ${exportPngButton ? !exportPngButton.disabled : 'N/A'}, SVG: ${exportSvgButton ? !exportSvgButton.disabled : 'N/A'}, HTML: ${exportHtmlButton ? !exportHtmlButton.disabled : 'N/A'}`);
}

function downloadFile(filename, content, mimeType) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
