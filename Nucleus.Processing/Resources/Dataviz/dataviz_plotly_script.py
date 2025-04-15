# === START OF PYTHON SCRIPT ===
import plotly.express as px
import pandas as pd
import io
import json

# Read the JSON data passed from the main script
# This assumes the data is available in a variable or fetched appropriately
# In this template setup, it will be provided via pyodide.runPythonAsync
# The 'input_data_json' variable will be injected by the worker script
data = json.loads(input_data_json)
df = pd.DataFrame(data)

# Generate the plot
# Use the actual column names from the input data
fig = px.scatter(df, x="x_col", y="y_col", title="Scatter Plot from Python") # Modified column names

# Update the layout
fig.update_layout(
    title='Scatter Plot from Python (Plotly)',
    xaxis_title='X Axis',
    yaxis_title='Y Axis',
    margin=dict(l=40, r=40, t=40, b=40),
    autosize=True  # Enable autosizing
)

# Convert the plot to JSON
graph_json = fig.to_json()

# Return the JSON string (this will be the output of pyodide.runPythonAsync)
graph_json
# === END OF PYTHON SCRIPT ===
