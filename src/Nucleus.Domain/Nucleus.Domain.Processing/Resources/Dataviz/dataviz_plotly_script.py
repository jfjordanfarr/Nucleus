# === START OF PYTHON SCRIPT ===
"""
Nucleus Dataviz Python Plotly Script

- See architecture: ../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md
- Related: dataviz_script.js, DatavizHtmlBuilder.cs
- Working example: ../Docs/Architecture/Processing/Dataviz/EXAMPLE_OUTPUT_nucleus_dataviz_20250416145545.html

Data contract: Output is a dict with 'data' and 'layout' keys, serializable to JSON for JS rendering.
"""
import plotly.express as px
import pandas as pd
import json
import sys

# Initialize output_result in case of early failure
output_result = json.dumps({"error": True, "message": "Python script failed before main execution."})

try:
    # input_data_json is expected to be injected as a global variable (string) by the JS worker
    if 'input_data_json' not in globals():
        raise NameError("'input_data_json' global variable not found.")

    print("DEBUG (Python): 'input_data_json' found globally.")
    # print(f"DEBUG (Python): Value of input_data_json BEFORE loads: {input_data_json}") # Can be very long

    # 1. Parse the JSON string into a Python dictionary
    data = json.loads(input_data_json)
    print(f"DEBUG (Python): Type of data AFTER loads: {type(data)}")

    # --- Detailed Inspection of 'data' ---
    print(f"DEBUG (Python): Is 'data' a dict? {isinstance(data, dict)}")
    if isinstance(data, dict):
        print(f"DEBUG (Python): Keys in 'data': {list(data.keys())}")
        for key, value in data.items():
            print(f"DEBUG (Python):   Key '{key}': Type={type(value)}, Is List={isinstance(value, list)}, Len={len(value) if isinstance(value, list) else 'N/A'}")
            if isinstance(value, list) and len(value) > 0:
                print(f"DEBUG (Python):     First item type: {type(value[0])}")
    # --- End Detailed Inspection ---

    # 2. Create DataFrame using the parsed dictionary
    df = pd.DataFrame(data)
    print("DEBUG (Python): DataFrame created successfully.")

    # Check DataFrame structure
    print(f"DEBUG (Python): DataFrame shape: {df.shape}")
    print(f"DEBUG (Python): DataFrame columns: {df.columns.tolist()}")
    print(f"DEBUG (Python): DataFrame head:\n{df.head().to_string()}")

    # Define expected columns (adjust as necessary based on typical input)
    # These might need to be dynamically determined or passed in if they vary widely
    x_col_name = 'x_col' # Default/Example
    y_col_name = 'y_col' # Default/Example
    # Optional: Add logic here to find suitable columns if not 'x_col', 'y_col'
    if x_col_name not in df.columns or y_col_name not in df.columns:
        # Try to find the first two suitable numeric columns if defaults aren't present
        numeric_cols = df.select_dtypes(include='number').columns
        if len(numeric_cols) >= 2:
            x_col_name = numeric_cols[0]
            y_col_name = numeric_cols[1]
            print(f"DEBUG (Python): Default columns not found, using first two numeric: '{x_col_name}', '{y_col_name}'")
        else:
            raise ValueError(f"Could not find required columns '{x_col_name}' and '{y_col_name}' or suitable numeric alternatives in the DataFrame.")

    # Create the scatter plot using the dynamically determined columns
    fig = px.scatter(df, x=x_col_name, y=y_col_name, title='Scatter Plot from Python')
    print(f"DEBUG (Python): Scatter plot created using columns '{x_col_name}' and '{y_col_name}'.")

    # Update the layout
    # NOTE: 'autosize=True' is critical for frontend responsiveness (see ARCHITECTURE_PROCESSING_DATAVIZ.md)
    fig.update_layout(
        title='Scatter Plot from Python (Plotly)',
        xaxis_title=x_col_name, # Use column name for axis title
        yaxis_title=y_col_name, # Use column name for axis title
        margin=dict(l=40, r=40, t=40, b=40),
        autosize=True  # Enable autosizing
    )
    print("DEBUG (Python): Plotly layout updated.")

    # Extract data and layout as Python objects (should be JSON serializable by Pyodide/JS bridge)
    # Use fig.to_dict() to ensure serializability if direct properties cause issues
    plot_data = fig.to_dict()['data'] 
    plot_layout = fig.to_dict()['layout']
    output_dict = {'data': plot_data, 'layout': plot_layout}
    print(f"DEBUG (Python): Plotly data/layout dict generated.")

    # Return the dictionary (this will be the 'output' field in the JS worker message)
    output_result = json.dumps(output_dict) # Assign success result

except Exception as e:
    # Log error clearly to stdout and stderr
    error_message = f"ERROR in Python script: {type(e).__name__}: {e}"
    print(error_message, file=sys.stderr) # Try stderr
    print(error_message) # Also print to stdout just in case

    # Assign an error indicator as the result instead of raising
    # Return a JSON string indicating failure
    output_result = json.dumps({"error": True, "message": error_message})

# === DEBUGGING: Check final output before returning ===
# This will now print either the graph JSON or the error JSON
print(f"DEBUG (Python): Type of final output_result: {type(output_result)}")
print(f"DEBUG (Python): Value of final output_result (first 100 chars): {str(output_result)[:100]}")
# ======================================================

# The value of the last expression is returned
output_result
# === END OF PYTHON SCRIPT ===
