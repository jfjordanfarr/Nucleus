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

    // Signal main thread that Pyodide and packages are ready
    self.postMessage({ type: 'pyodide_loaded' }); // Changed from 'status'

    return pyodide; // Return the initialized pyodide instance

  }).catch(error => {
     console.error("Worker: Pyodide initialization or package loading failed:", error);
     self.postMessage({ type: 'error', message: `Pyodide/Package Error: ${error.message}` });
     pyodideLoadingPromise = null; // Reset promise on error
     throw error; // Re-throw error to indicate failure
  });
  return pyodideLoadingPromise;
}

// Handle messages from the main thread
self.onmessage = async (event) => {
  const messageData = event.data;
  const messageType = messageData.type;

  console.log(`Worker DEBUG: Received messageData:`, messageData);
  console.log(`Worker DEBUG: Received messageType: ${messageType}`);

  if (messageType === 'execute_script') {
    const pythonScript = messageData.pythonScript;
    const receivedJsonData = messageData.jsonData; // NEW - Accessing 'jsonData' based on logs

    console.log(`Worker DEBUG: Accessed pythonScript type: ${typeof pythonScript}`);
    console.log(`Worker DEBUG: Accessed receivedJsonData using '.jsonData' - Type: ${typeof receivedJsonData}`);
    console.log(`Worker DEBUG: Accessed receivedJsonData using '.jsonData' - Value:`, receivedJsonData);

    // Check if they are undefined before proceeding
    if (typeof pythonScript === 'undefined' || typeof receivedJsonData === 'undefined') {
      console.error("Worker ERROR: pythonScript or receivedJsonData is undefined after accessing .jsonData!");
      self.postMessage({ type: 'execution_error', message: 'Worker failed to receive script or data correctly (using .jsonData).' });
      return; // Stop execution
    }

    try {
      // Ensure Pyodide is initialized
      await initializePyodide();

      if (!pyodide) {
          throw new Error("Pyodide is not available after initialization attempt.");
      }

      console.log("Worker: Received task. Running Python script...");
      self.postMessage({ type: 'status', message: 'Running Python script...' });

      // Debug logging
      console.log(`Worker DEBUG: Type of receivedJsonData (JS): ${typeof receivedJsonData}`);
      console.log(`Worker DEBUG: Value of receivedJsonData (JS):`, receivedJsonData);

      // Inject data into the Python environment AS A JSON STRING
      const jsonDataString = JSON.stringify(receivedJsonData);
      console.log(`Worker DEBUG: Type of jsonDataString being set to Python global: ${typeof jsonDataString}`);
      console.log(`Worker DEBUG: Value of jsonDataString being set (first 100 chars): ${jsonDataString.substring(0,100)}...`);
      pyodide.globals.set('input_data_json', jsonDataString); // NEW JSON string method

      // Run the Python script
      console.log("Worker DEBUG: About to call runPythonAsync...");
      const result = await pyodide.runPythonAsync(pythonScript);
      console.log(`Worker DEBUG: Type of result from runPythonAsync: ${typeof result}`);
      console.log(`Worker DEBUG: Value of result from runPythonAsync:`, result);

      console.log("Worker: Python script finished. Sending result.");
      // Send the result back to the main thread
      self.postMessage({ type: 'result', data: result });

    } catch (error) {
      console.error("Worker: Error executing Python script:", error);
      // Send specific error type expected by main thread
      self.postMessage({ type: 'execution_error', message: error.message });
    }
  }
};

// Initial message to confirm worker is loaded (optional)
console.log("Worker: Script loaded. Waiting for initialization trigger.");
// Trigger initialization immediately upon loading the worker script.
initializePyodide();
