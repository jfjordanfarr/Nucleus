// dataviz-worker.js
// Import Pyodide - adjust the version if needed
importScripts("https://cdn.jsdelivr.net/pyodide/v0.25.1/full/pyodide.js");

let pyodide = null;
let pyodideLoadingPromise = null;

async function initializePyodide() {
  if (pyodide) {
    return pyodide;
  }
  if (pyodideLoadingPromise) {
    return pyodideLoadingPromise;
  }

  console.log("Worker: Initializing Pyodide...");
  pyodideLoadingPromise = loadPyodide({
    // indexURL: "https://cdn.jsdelivr.net/pyodide/v0.25.1/full/" // Optional: specify indexURL if needed
  }).then(async (instance) => {
    pyodide = instance;
    console.log("Worker: Pyodide initialized. Loading micropip...");
    await pyodide.loadPackage("micropip");
    const micropip = pyodide.pyimport("micropip");
    console.log("Worker: Micropip loaded. Loading pandas and plotly...");
    await micropip.install(['pandas', 'plotly']);
    console.log("Worker: pandas and plotly loaded.");
    self.postMessage({ type: 'status', message: 'Pyodide and packages ready.' });
    pyodideLoadingPromise = null; // Reset promise after successful load
    return pyodide;
  }).catch(error => {
     console.error("Worker: Pyodide initialization failed:", error);
     self.postMessage({ type: 'error', message: `Pyodide initialization failed: ${error.message}` });
     pyodideLoadingPromise = null; // Reset promise on error
     throw error; // Re-throw error to indicate failure
  });
  return pyodideLoadingPromise;
}

// Handle messages from the main thread
self.onmessage = async (event) => {
  const { pythonScript, inputData } = event.data;

  try {
    // Ensure Pyodide is initialized
    await initializePyodide();

    if (!pyodide) {
        throw new Error("Pyodide is not available after initialization attempt.");
    }

    console.log("Worker: Received task. Running Python script...");
    self.postMessage({ type: 'status', message: 'Running Python script...' });

    // Inject data into the Python environment
    pyodide.globals.set('input_data_json', JSON.stringify(inputData));

    // Run the Python script
    const result = await pyodide.runPythonAsync(pythonScript);

    console.log("Worker: Python script finished. Sending result.");
    // Send the result back to the main thread
    self.postMessage({ type: 'result', data: result });

  } catch (error) {
    console.error("Worker: Error executing Python script:", error);
    self.postMessage({ type: 'error', message: error.message });
  }
};

// Initial message to confirm worker is loaded (optional)
console.log("Worker: Script loaded. Waiting for initialization trigger.");
// Trigger initialization immediately upon loading the worker script.
initializePyodide();
