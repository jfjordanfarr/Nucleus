# Test script to simulate agent query via direct API call

# Target API Endpoint (Assumes AppHost exposes ApiService on this port)
$apiUrl = "https://localhost:19110/api/interaction/query" # Updated endpoint

# Query
$queryText = "What is the secret code mentioned in test_doc.txt?"

# --- Construct AdapterRequest JSON ---
$conversationId = [guid]::NewGuid().ToString()
$adapterRequestJson = @"
{
    "PlatformType": "Console",
    "ConversationId": "$conversationId",
    "UserId": "AgentUser",
    "QueryText": "$queryText",
    "MessageId": null,
    "ReplyToMessageId": null,
    "ArtifactReferences": null,
    "Metadata": null
}
"@

Write-Host "--- Sending Query Request ---"
Write-Host $adapterRequestJson
Write-Host "-----------------------------"

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
