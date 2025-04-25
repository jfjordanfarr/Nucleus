# Test script to simulate agent ingestion via direct API call

# Target API Endpoint (Assumes AppHost exposes ApiService on this port)
$apiUrl = "https://localhost:19110/api/interaction/ingest" # Updated endpoint

# File to ingest (Use absolute path for Console PlatformType)
$filePath = "d:\Projects\Nucleus\_LocalData\test_doc.txt"
$fileName = Split-Path -Path $filePath -Leaf         # Extract filename
$fileExtension = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant() # Extract extension

# Basic MIME type mapping (align with LocalFileArtifactProvider)
$mimeType = switch ($fileExtension) {
    ".txt"  { "text/plain" }
    ".pdf"  { "application/pdf" }
    ".jpg"  { "image/jpeg" }
    ".jpeg" { "image/jpeg" }
    ".png"  { "image/png" }
    ".docx" { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
    default { "application/octet-stream" }
}

# --- Construct AdapterRequest JSON ---
$conversationId = [guid]::NewGuid().ToString()
$adapterRequestJson = @"
{
    "PlatformType": "Console",
    "ConversationId": "$conversationId",
    "UserId": "AgentUser",
    "QueryText": "Ingest referenced file: $($filePath)",
    "MessageId": null,
    "ReplyToMessageId": null,
    "ArtifactReferences": [
        {
            "ReferenceId": "$($filePath.Replace('\\', '\\\\'))",  # Escaped path is the ID for local files
            "ReferenceType": "local_file_path", # Indicate it's a local file
            "FileName": "$fileName",          # Include the filename
            "MimeType": "$mimeType"           # Include the determined MIME type
        }
    ],
    "Metadata": null
}
"@

Write-Host "--- Sending Ingestion Request ---"
Write-Host $adapterRequestJson
Write-Host "---------------------------------"

# --- Send Request (Bypassing Cert Validation) ---
$originalCallback = [System.Net.ServicePointManager]::ServerCertificateValidationCallback
try {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

    $response = Invoke-RestMethod -Uri $apiUrl `
                                  -Method Post `
                                  -ContentType 'application/json' `
                                  -Body $adapterRequestJson `
                                  -ErrorAction Stop

    Write-Host "--- Response ---"
    Write-Host ($response | ConvertTo-Json -Depth 5)
    Write-Host "----------------"

} catch {
    Write-Error "API Call Failed: $($_.Exception.Message)"
    # Optionally display more error details: $_ | Format-List * -Force
} finally {
    # Restore original certificate validation callback
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $originalCallback
}
