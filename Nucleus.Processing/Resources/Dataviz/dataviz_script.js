// --- Global State Variables ---
let pyodideWorker = null;
let currentOutputContent = null;
let currentOutputType = null; // 'plotly', 'svg', 'message', or 'error'
let workerLogs = []; // Array to store logs from the worker

// Global element references - declared here, assigned in DOMContentLoaded
let loadingIndicator = null;
let errorArea = null;
let plotArea = null;
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
function displayError(message) {
    console.error("Main Thread Error:", message);
    workerLogs.push(`[ERROR] ${message}`); // Add to logs
    if (!loadingIndicator) {
        console.error("Main: Loading indicator not found!");
        return;
    }
    loadingIndicator.style.display = 'none';
    outputArea.style.display = 'none';
    if (!exportButtonsDiv) {
        console.error("Main: Export buttons div not found!");
        return;
    }
    exportButtonsDiv.style.display = 'none';
    // Sanitize or limit error message length if necessary
    if (!errorArea) {
        console.error("Main: Error area not found!");
        return;
    }
    errorArea.textContent = `Error processing visualization:\n${message.substring(0, 1000)}${message.length > 1000 ? '...' : ''}`;
    errorArea.style.display = 'block';
    if (pyodideWorker) {
        pyodideWorker.terminate(); // Clean up worker on error
        pyodideWorker = null;
    }
    viewWorkerLogButton.style.display = 'inline-block'; // Show log button on error
}

function displayOutput(output, type) {
    currentOutputType = type;
    currentOutputContent = output;
    console.log('DisplayOutput received type:', type);
    console.log('DisplayOutput received output content:', output); // Log the received content
    if (!loadingIndicator) {
        console.error("Main: Loading indicator not found!");
        return;
    }
    loadingIndicator.style.display = 'none';
    if (!errorArea) {
        console.error("Main: Error area not found!");
        return;
    }
    errorArea.style.display = 'none';
    outputArea.innerHTML = ''; // Clear previous output/scripts
    try {
        if (type === 'plotly') {
            console.log('Injecting Plotly HTML snippet into outputArea.');
            outputArea.innerHTML = output;

            console.log('Processing scripts within injected HTML to ensure load order...');
            const scripts = outputArea.querySelectorAll('script');
            let plotlyLibraryScriptElement = null;
            const plotlyCallingScriptElements = [];
            const otherScriptElements = [];

            // Create new script elements and categorize them
            scripts.forEach(oldScript => {
                const newScript = document.createElement('script');
                Array.from(oldScript.attributes).forEach(attr => {
                    newScript.setAttribute(attr.name, attr.value);
                });
                if (oldScript.textContent) {
                    newScript.textContent = oldScript.textContent;
                }

                if (newScript.src && newScript.src.includes('cdn.plot.ly')) {
                    plotlyLibraryScriptElement = newScript; // Assume only one library script
                } else if (newScript.textContent && newScript.textContent.includes('Plotly.newPlot')) {
                    plotlyCallingScriptElements.push(newScript);
                } else {
                    otherScriptElements.push(newScript);
                }
                // Remove the original non-executed script node (for cleanup, avoids double execution if browser behaves unexpectedly)
                oldScript.parentNode.removeChild(oldScript);
            });

            // Execute non-Plotly scripts first
            otherScriptElements.forEach(script => {
                 console.log('Executing non-Plotly script:', script.src || 'inline script');
                 outputArea.appendChild(script);
            });

            // Load Plotly library, then execute calling scripts on load
            if (plotlyLibraryScriptElement) {
                plotlyLibraryScriptElement.onload = () => {
                    console.log('Plotly library loaded via onload. Executing dependent scripts...');
                    plotlyCallingScriptElements.forEach(script => {
                         console.log('Executing Plotly-dependent script:', script.src || 'inline script');
                         outputArea.appendChild(script);
                    });
                    // Explicitly call resize after plot generation attempt
                    setTimeout(() => { // Use setTimeout to ensure it runs after plot is potentially drawn
                        const plotDiv = outputArea.querySelector('.plotly-graph-div');
                        if (plotDiv && typeof Plotly !== 'undefined') {
                            console.log('Attempting Plotly resize on:', plotDiv.id);
                            Plotly.Plots.resize(plotDiv);
                        }
                    }, 100); // Adjust delay if needed
                };
                plotlyLibraryScriptElement.onerror = () => {
                     console.error('Failed to load Plotly library script!');
                     displayError('Failed to load Plotly library. Cannot render visualization.');
                };
                 console.log('Appending Plotly library script (will load async):', plotlyLibraryScriptElement.src);
                 outputArea.appendChild(plotlyLibraryScriptElement);
            } else {
                // If no library found, but calling scripts exist, it's an error state
                if (plotlyCallingScriptElements.length > 0) {
                     console.error('Plotly calling script found, but no Plotly library script detected!');
                     displayError('Plotly library script missing in generated output. Cannot render visualization.');
                } else {
                     console.log('No Plotly library script found, executing remaining scripts directly (if any).');
                     plotlyCallingScriptElements.forEach(script => { // Should be empty in this case, but just in case
                         console.log('Executing script (no library dependency assumed):', script.src || 'inline script');
                         outputArea.appendChild(script);
                     });
                }
            }

        } else if (type === 'svg') {
            // Directly inject SVG content (no scripts expected here)
            outputArea.innerHTML = output;
        } else {
            outputArea.textContent = "Unsupported output type.";
        }
        outputArea.style.display = 'block';
        if (!exportButtonsDiv) {
            console.error("Main: Export buttons div not found!");
            return;
        }
        exportButtonsDiv.style.display = 'block';
        updateExportButtonStates(type);
    } catch (e) {
        displayError(`Error rendering output: ${e.message}`);
    }
}

function updateExportButtonStates(type) {
    // Enable/disable based on output type and library capabilities
    const hasPlotly = typeof Plotly !== 'undefined' && typeof Plotly.toImage === 'function';
    exportPngButton.disabled = !(type === 'plotly_json' && hasPlotly);
    exportSvgButton.disabled = !(type === 'plotly_json' && hasPlotly);
    exportHtmlButton.disabled = !(type === 'plotly_json' || type === 'svg'); // Enable if we have Plotly JSON or raw SVG
}

function downloadFile(filename, content, mimeType) {
    console.log(`Attempting to download: ${filename}, Mime: ${mimeType}, Content starts with: ${content.substring(0, 30)}...`);
    const link = document.createElement('a');
    link.download = filename;

    if (typeof content === 'string' && content.startsWith('data:')) {
        // Handle Data URLs (from Plotly.toImage for PNG/SVG)
        console.log("Content is a Data URL. Setting href directly.");
        link.href = content;
    } else {
        // Handle other content (like HTML export) by creating a Blob
        console.log("Content is not a Data URL. Creating Blob URL.");
        const blob = new Blob([content], { type: mimeType });
        link.href = URL.createObjectURL(blob);
    }

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Clean up Blob URL if one was created
    if (!(typeof content === 'string' && content.startsWith('data:')) && link.href) {
         URL.revokeObjectURL(link.href);
         console.log("Blob URL revoked.");
    }
}

// --- Worker Setup and Communication --- ---
function initializePyodideWorker(workerScriptContent, pythonScriptContent, inputData) {
    if (pyodideWorker) {
        return; // Already initialized or initializing
    }
    workerLogs = ['[INFO] Initializing Worker...']; // Reset logs
    viewWorkerLogButton.style.display = 'none'; // Hide initially

    try {
        // Create a Blob from the worker script string
        const blob = new Blob([workerScriptContent], { type: 'application/javascript' });
        const workerUrl = URL.createObjectURL(blob);

        console.log("Main: Creating Worker from Blob URL...");
        pyodideWorker = new Worker(workerUrl);

        // --- Worker Message Handling ---
        pyodideWorker.onmessage = (event) => {
            const { type, message, data } = event.data;
            console.log("Main: Received message from worker:", event.data);
            workerLogs.push(`[WORKER ${type.toUpperCase()}] ${message || data}`); // Log status/errors/results
            viewWorkerLogButton.style.display = 'inline-block'; // Show log button once worker communicates

            if (type === 'status') {
                if (!loadingIndicator) {
                    console.error("Main: Loading indicator not found!");
                    return;
                }
                loadingIndicator.textContent = message; // Update loading status
            } else if (type === 'result') {
                console.log("Main: Received plot JSON from worker.");
                // Assuming Plotly output for now based on python script
                // The Python script returns JSON, Plotly.react needs this JSON
                try {
                    const plotJson = JSON.parse(data); // Correctly parse the full JSON
                    const plotData = plotJson.data;    // Declare with const
                    const plotLayout = plotJson.layout; // Declare with const

                    // Log the data and layout being passed to Plotly
                    console.log("Plot Data:", JSON.stringify(plotData));
                    console.log("Plot Layout:", JSON.stringify(plotLayout));

                    // --- Render directly into the existing #plot-area --- 
                    console.log("Main: Preparing plot area.");
                    if (!loadingIndicator) {
                        console.error("Main: Loading indicator not found!");
                        return;
                    }
                    loadingIndicator.style.display = 'none'; // Hide loading
                    if (!errorArea) {
                        console.error("Main: Error area not found!");
                        return;
                    }
                    errorArea.style.display = 'none'; // Hide error area
                    // outputArea.innerHTML = ''; // REMOVED: Don't clear the container holding #plot-area
                    
                    // Use the global plotArea reference (should be non-null if DOM loaded)
                    if (!plotArea) {
                         throw new Error("Plot area element (#plot-area) not found.");
                    }
                    plotArea.innerHTML = ''; // Clear previous plot content inside #plot-area
                    plotArea.style.display = 'block'; // Make the dedicated plot area visible

                    Plotly.newPlot('plot-area', plotData, plotLayout, { responsive: true }); // Target the existing #plot-area
                    console.log("Main: Plotly plot rendered into #plot-area.");

                    currentOutputContent = data; // Store the JSON data for potential export
                    currentOutputType = 'plotly_json'; // Set type for export logic

                } catch (e) {
                    console.error("Main: Error parsing or rendering Plotly JSON", e);
                    displayError(`Failed to render plot: ${e.message}`);
                }
                 // Update buttons after rendering attempt
                 if (!exportButtonsDiv) {
                    console.error("Main: Export buttons div not found!");
                    return;
                 }
                 exportButtonsDiv.style.display = 'block';
                 updateExportButtonStates(currentOutputType);
            } else if (type === 'error') {
                console.error("Main: Received error from worker:", message);
                displayError(`Worker error: ${message}`);
            }
        };

        // --- Worker Error Handling ---
        pyodideWorker.onerror = (error) => {
            console.error("Main: Worker error occurred:", error);
            workerLogs.push(`[ERROR] Worker script error: ${error.message} at ${error.filename}:${error.lineno}`);
            displayError(`Worker script error: ${error.message}`);
            viewWorkerLogButton.style.display = 'inline-block'; // Show log button on error
            // No need to terminate here, displayError already handles it
        };

        // Clean up the Blob URL once the worker is created (or if creation fails)
        // Though technically it's safe to revoke immediately after worker creation starts
        URL.revokeObjectURL(workerUrl);

        // Send the initial task to the worker
        console.log("Main: Sending initial task to worker...");
        pyodideWorker.postMessage({
            pythonScript: pythonScriptContent,
            inputData: inputData
        });

    } catch (initError) {
         console.error("Main: Failed to create Worker:", initError);
         displayError(`Failed to initialize visualization engine: ${initError.message}`);
         workerLogs.push(`[ERROR] Failed to create Worker: ${initError.message}`);
         viewWorkerLogButton.style.display = 'inline-block';
    }
}

// --- Initialization --- (Modified)
document.addEventListener('DOMContentLoaded', () => {
    // --- Assign global element references --- 
    loadingIndicator = document.getElementById('loading-indicator');
    errorArea = document.getElementById('error-area');
    plotArea = document.getElementById('plot-area');
    exportButtonsDiv = document.getElementById('export-buttons');

    // --- Local references for modals and buttons (used only within this scope) ---
    const codeModal = document.getElementById('code-modal');
    const dataModal = document.getElementById('data-modal');
    const closeModalButton = document.getElementById('close-modal');
    const pythonCodeDisplay = document.getElementById('python-code-display');
    const viewDataButton = document.getElementById('view-data');
    const jsonDataDisplay = document.getElementById('json-data-display');
    const closeDataModalButton = document.getElementById('close-data-modal');
    const viewWorkerLogButton = document.getElementById('view-worker-log');
    const workerLogModal = document.getElementById('worker-log-modal');
    const closeWorkerLogModalButton = document.getElementById('close-worker-log-modal');
    const workerLogDisplay = document.getElementById('worker-log-display');
    const copyWorkerLogButton = document.getElementById('copy-worker-log-button');
    const copyPythonButton = document.getElementById('copy-python-button');
    const copyDataButton = document.getElementById('copy-data-button');

    // Read embedded content
    let jsonData = null;
    let pythonScriptContent = null;
    let workerScriptContent = null;

    try {
        // Read worker script from its designated template tag
        const workerScriptElement = document.getElementById('worker-script-template');
        if (!workerScriptElement) throw new Error("Worker script template element not found.");
        workerScriptContent = decodeHtmlEntities(workerScriptElement.textContent); // Decode

        // Read Python script from its template tag
        const pythonScriptElement = document.getElementById('python-script-template');
        if (!pythonScriptElement) throw new Error("Python script template element not found.");
        pythonScriptContent = decodeHtmlEntities(pythonScriptElement.textContent); // Decode

        // Read JSON data from its template tag
        const jsonDataElement = document.getElementById('input-data');
        if (!jsonDataElement) throw new Error("JSON data template element not found.");
        const rawJsonData = decodeHtmlEntities(jsonDataElement.textContent); // Decode

        // Parse JSON data
        if (!rawJsonData) {
            console.warn("JSON data in template was empty. Defaulting to {}.");
            jsonData = {}; // Default to empty object
        } else {
             try {
                 jsonData = JSON.parse(rawJsonData);
             } catch (parseError) {
                 throw new Error(`Failed to parse JSON data from template: ${parseError.message}`);
             }
        }

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
         // Requires Plotly.toImage - make sure Plotly object is available
         const plotDiv = outputArea.querySelector('#plot-area'); // Target the specific div
         if (plotDiv && typeof Plotly !== 'undefined') {
            Plotly.toImage(plotDiv, { format: 'png', height: 600, width: 800 })
                .then(dataUrl => downloadFile('visualization.png', dataUrl, 'image/png'))
                .catch(err => {
                    console.error('Plotly PNG export failed:', err);
                    alert('Failed to export PNG.');
                });
         } else {
             alert("PNG export requires a rendered Plotly chart.");
         }
     });

     exportSvgButton.addEventListener('click', () => {
         const plotDiv = outputArea.querySelector('#plot-area');
         if (plotDiv && typeof Plotly !== 'undefined') {
             Plotly.toImage(plotDiv, { format: 'svg', height: 600, width: 800 })
                .then(dataUrl => downloadFile('visualization.svg', dataUrl, 'image/svg+xml'))
                .catch(err => {
                    console.error('Plotly SVG export failed:', err);
                    alert('Failed to export SVG.');
                });
         } else {
             alert('SVG export requires a rendered Plotly chart.');
         }
     });

     exportHtmlButton.addEventListener('click', () => {
         if (currentOutputType === 'plotly_json' && currentOutputContent) {
            try {
                // Parse the stored JSON string to get data/layout objects
                const plotJson = JSON.parse(currentOutputContent);
                const plotData = plotJson.data;
                const plotLayout = plotJson.layout;

                // Convert data/layout objects back to JSON strings. These are what we need.
                const plotDataString = JSON.stringify(plotData);
                const plotLayoutString = JSON.stringify(plotLayout);

                // Build HTML content. Embed the raw JSON strings directly into the script.
                // Replace any potential closing script tags within the JSON to prevent breaking the HTML.
                const safePlotDataString = plotDataString.replace(/<\/script>/gi, '<\\/script>');
                const safePlotLayoutString = plotLayoutString.replace(/<\/script>/gi, '<\\/script>');

                const htmlContent = "<!DOCTYPE html>\n"
                                + "<html>\n"
                                + "<head>\n"
                                + "    <meta charset=\"utf-8\" />\n"
                                + "    <title>Exported Plotly Chart</title>\n"
                                // Use the same Plotly version as the main page
                                + "    <script src='https://cdn.plot.ly/plotly-2.32.0.min.js'><\/script>\n"
                                + "</head>\n"
                                + "<body>\n"
                                + "    <div id='plotly-div'></div>\n"
                                + "    <script>\n"
                                + "        try {\n"
                                + "            var data = " + safePlotDataString + ";\n"
                                + "            var layout = " + safePlotLayoutString + ";\n"
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

     // --- View Code Button Handler (Modified) ---
     viewCodeButton.addEventListener('click', () => {
         if (pythonScriptContent) {
             pythonCodeDisplay.textContent = pythonScriptContent.trim(); // Display the code
             Prism.highlightElement(pythonCodeDisplay); // Apply syntax highlighting
             codeModal.style.display = "block"; // Show the modal
         } else {
             alert("Could not find the embedded Python script content.");
         }
     });

     // --- View Data Button Handler (Modified) ---
     viewDataButton.addEventListener('click', () => {
         try {
             const jsonDataString = JSON.stringify(jsonData, null, 2); // Pretty print JSON
             if (jsonDataString) {
                 jsonDataDisplay.textContent = jsonDataString; // Display the data
                 Prism.highlightElement(jsonDataDisplay); // Apply syntax highlighting
                 dataModal.style.display = "block"; // Show the modal
             } else {
                 alert("Could not format the embedded JSON data.");
             }
         } catch (e) {
             alert(`Error formatting JSON data: ${e.message}`);
         }
     });

     // --- View Worker Log Button Handler ---
     viewWorkerLogButton.addEventListener('click', () => {
         workerLogDisplay.textContent = workerLogs.join('\n'); // Display logs
         Prism.highlightElement(workerLogDisplay); // Apply syntax highlighting
         workerLogModal.style.display = "block"; // Show the modal
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
            const jsonDataString = JSON.stringify(jsonData, null, 2);
            if (jsonDataString) copyToClipboard(jsonDataString);
        } catch (e) { alert('Failed to copy JSON data.'); }
     });

     copyWorkerLogButton.addEventListener('click', () => {
        if(workerLogs.length > 0) copyToClipboard(workerLogs.join('\n'));
     });

    // Initialize Prism for syntax highlighting if modals are used
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }

    // --- Plot Resizing Logic --- 
    // Reusable function to update Plotly layout based on plotArea size
    const updatePlotLayout = () => {
        // Ensure plotArea is valid, visible, Plotly exists, and the plot has been rendered inside
        if (plotArea && plotArea.offsetParent !== null && typeof Plotly !== 'undefined' && plotArea.querySelector('.plotly-graph-div')) {
            try {
                const newWidth = plotArea.clientWidth;
                const newHeight = plotArea.clientHeight;
                // Defer Plotly updates slightly using setTimeout to allow browser layout reflow
                setTimeout(() => {
                    if (newWidth > 0 && newHeight > 0) {
                        Plotly.relayout(plotArea, {
                            width: newWidth,
                            height: newHeight,
                            'xaxis.autorange': true,
                            'yaxis.autorange': true
                        });
                        Plotly.Plots.resize(plotArea);
                    } else {
                    }
                }, 0); // Use setTimeout with 0 delay
            } catch (e) {
                console.error('Error during Plotly relayout:', e);
            }
        } else {
        }
    };

    // Add resize handler for Plotly chart (for WINDOW resize)
    window.addEventListener('resize', updatePlotLayout);

    // Add ResizeObserver for the outputArea (for ELEMENT resize)
    if (window.ResizeObserver && outputArea) {
        const resizeObserver = new ResizeObserver(entries => {
            // We typically only observe one element here
            for (let entry of entries) {
                updatePlotLayout();
            }
        });
        resizeObserver.observe(outputArea);
    } else {
        console.warn('ResizeObserver not supported or outputArea not found.');
    }
});
