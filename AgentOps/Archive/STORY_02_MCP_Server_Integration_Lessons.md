# MCP Server Integration: Lessons from Building CodeBones.Analyzer

## Context

During the development of the Nucleus-OmniRAG project (April 2025), we needed to integrate a custom C# code analysis tool, `CodeBones.Analyzer`, with the development environment (Windsurf/VS Code) using the Model Context Protocol (MCP). This involved creating an MCP server within the analyzer application. This document details the challenges faced during integration and the key lessons learned, particularly regarding protocol implementation choices.

## Project Background

- **Goal**: Expose C# analysis capabilities (project structure analysis, class listing, usage finding) as tools callable via MCP.
- **Tool**: `CodeBones.Analyzer`, a .NET console application using Roslyn for code analysis.
- **Integration Target**: Windsurf/VS Code, which acts as an MCP client.

## The Initial Approach: Manual MCP Implementation

Our first attempt involved manually implementing the MCP server logic directly within `CodeBones.Analyzer`. This included:

1.  **Stdio Communication**: Reading JSON requests from `stdin` and writing JSON responses to `stdout`.
2.  **Capability Handshake**: Sending the server's capabilities (list of available tools and their schemas) as the first message on `stdout`.
3.  **JSON Handling**: Manually serializing and deserializing MCP request/response objects.
4.  **Error Handling**: Writing error messages and diagnostic information to `stderr`.

```csharp
// Simplified representation of the manual approach in Program.cs (Pre-SDK)

private static async Task RunMcpMode()
{
    // 1. Send Capabilities (Manual JSON Serialization)
    var capabilities = GetMcpCapabilities(); // Builds the capability structure
    Console.WriteLine(JsonSerializer.Serialize(capabilities, jsonOptions)); 
    Console.Out.Flush();
    Console.Error.WriteLine("Capabilities sent. Waiting for commands...");

    // 2. Read Requests from stdin
    using var reader = new StreamReader(Console.OpenStandardInput());
    while (!reader.EndOfStream)
    {
        string? requestLine = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(requestLine)) continue;
        
        try 
        {
            // 3. Deserialize Request
            McpRequest? request = JsonSerializer.Deserialize<McpRequest>(requestLine, jsonOptions);
            
            // 4. Process Command (switch statement based on request.Command)
            await ProcessMcpCommand(request); // Calls AnalyzeCodeBase, etc.
            
            // 5. Write Success/Error Response to stdout (Manual JSON Serialization)
            WriteSuccessResponse(resultData); 
        }
        catch (Exception ex)
        {
             WriteErrorResponse(ex.Message); 
        }
    }
}

// Helper methods for manual JSON generation/writing existed (GetMcpCapabilities, WriteSuccessResponse, etc.)
```

## The Challenge: Integration Failure - "Context Deadline Exceeded"

Despite the analyzer appearing functional when run manually (`.\CodeBones.Analyzer.exe --mcp`), integration with the Windsurf MCP client failed consistently.

- **Symptom**: Windsurf would show the `cs-analyzer` attempting to connect (yellow light, "0 tools loaded") before failing with a "Context Deadline Exceeded" error (red light).
- **Debugging Steps**:
    - Verified `mcp_config.json` pointed to the correct executable.
    - Confirmed manual execution *did* print the correct capabilities JSON to `stdout` immediately upon startup.
    - Hypothesized that initial `stderr` logging might interfere with Windsurf's handshake parsing; temporarily disabled `stderr` output before the capability message. **This did not resolve the issue.**

This indicated the problem wasn't a fundamental failure of the analyzer logic but a subtle incompatibility in the *protocol interaction* between the manual implementation and the Windsurf client.

## The Solution: Adopting the Official MCP SDK

The breakthrough came upon discovering the official `ModelContextProtocol` NuGet package. We refactored `CodeBones.Analyzer` to leverage the SDK.

**Key Changes:**
1.  **Added NuGet Packages**: `ModelContextProtocol` and `Microsoft.Extensions.Hosting`.
2.  **SDK-Managed Server**: Replaced `RunMcpMode` with `Host.CreateApplicationBuilder` configured with `.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`.
3.  **Attribute-Based Tools**: Defined analysis functions (`AnalyzeProject`, `ListClasses`, `FindUsages`) in a static class `AnalyzerTools` decorated with `[McpServerToolType]`, `[McpServerTool]`, and `[Description]` attributes.
4.  **Removed Manual Logic**: Eliminated all manual JSON handling, capability generation, and stdio read/write loops related to MCP.

```csharp
// Simplified representation of the SDK approach in Program.cs

public static async Task Main(string[] args)
{
    // ... parse args ...
    if (options.McpMode)
    {
        await RunMcpModeWithSdk(); // Use SDK host
    }
    // ... else command-line mode ...
}

private static async Task RunMcpModeWithSdk()
{
    var builder = Host.CreateApplicationBuilder();
    builder.Logging.AddConsole(opt => opt.LogToStandardErrorThreshold = LogLevel.Trace); // SDK handles logging
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport() // SDK handles stdio
        .WithToolsFromAssembly(); // SDK finds tools via attributes
    await builder.Build().RunAsync(); // SDK handles request loop & protocol
}

[McpServerToolType] // SDK discovers this class
public static class AnalyzerTools 
{
    [McpServerTool, Description("Analyze a C# project...")] // SDK exposes this method
    public static async Task<object> AnalyzeProject(
        [Description("Path to file...")] string projectPath) 
    {
        // Core analysis logic remains here
        return await AnalyzeCodeBase(projectPath, null!); 
    }

    // Other tools (ListClasses, FindUsages) defined similarly
    // ...
}
```

**Result**: The SDK-based implementation integrated successfully with Windsurf immediately.

## Key Lessons

### 1. Prioritize Official SDKs for Standard Protocols

Implementing communication protocols manually, even seemingly simple ones like MCP over stdio, is prone to subtle errors and integration issues.

**Best Practices:**
- **Always search for and prefer official SDKs** when implementing standard protocols (MCP, gRPC, HTTP APIs, etc.).
- SDKs encapsulate protocol nuances, handle edge cases, ensure compliance, and often provide higher-level abstractions that simplify development.
- The time spent finding and learning an SDK is often far less than the time spent debugging a manual implementation.

### 2. Understand Protocol Handshake Subtleties

The initial handshake (capability exchange in MCP) is critical for establishing communication. Manual implementations might fail due to incorrect timing, stream usage (`stdout` vs. `stderr`), or message formatting that deviates slightly from the client's expectations.

**Best Practices:**
- Rely on SDKs to manage handshakes and low-level protocol details correctly.
- When debugging integration, pay close attention to the *very first* messages exchanged between client and server.

### 3. Isolate Components During Integration Debugging

When a client-server integration fails, test each component in isolation before debugging the interaction.

**Best Practices:**
- Run the server component manually with expected inputs (e.g., `analyzer.exe --mcp`) to verify its standalone behavior.
- Check server logs (`stderr` in our case) for obvious errors during manual runs.
- Once standalone behavior is confirmed, focus on the client-server interaction points (configuration, process invocation, initial messages).

### 4. Leverage the Ecosystem (Search for Libraries!)

Before building custom solutions for standard problems (like implementing a known protocol), actively search for existing libraries or SDKs within the target language's ecosystem (e.g., NuGet for .NET).

**Best Practices:**
- Perform targeted searches (e.g., "Model Context Protocol .NET SDK", "MCP C# library").
- Check official protocol documentation or GitHub organizations for recommended libraries.

## Conclusion

While manually implementing protocols can be instructive, using official SDKs is significantly more efficient and robust for standard protocols like MCP. Our initial manual approach led to time-consuming debugging of subtle integration issues that were entirely bypassed by adopting the `ModelContextProtocol` SDK. This experience strongly reinforces the value of leveraging existing, well-tested libraries for standard communication tasks.

## Relevant Files

- `analyzer/Program.cs`: Contains the MCP server setup (both manual and SDK versions).
- `analyzer/CodeBones.Analyzer.csproj`: Project file showing NuGet dependencies (`ModelContextProtocol`, `Microsoft.Extensions.Hosting`).
- `mcp_config.json`: Windsurf configuration file pointing to the analyzer executable.
