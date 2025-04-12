#nullable enable
#r "nuget: Microsoft.CodeAnalysis.CSharp.Workspaces, 4.0.1"
#r "nuget: Microsoft.CodeAnalysis.Workspaces.MSBuild, 4.0.1" 
#r "nuget: Microsoft.Build.Locator, 1.5.5"
#r "nuget: Microsoft.Build.Framework, 17.0.0"
#r "nuget: System.Text.Json, 8.0.3" 
#r "nuget: System.CommandLine, 2.0.0-beta4.22272.1"

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.CommandLine.Binding;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

// --- Initial Setup ---

// Register MSBuild instance
if (!MSBuildLocator.IsRegistered)
{
    MSBuildLocator.RegisterDefaults();
}

// Print server info - This is required by MCP
var serverInfo = new Dictionary<string, object>
{
    ["name"] = "nucleus-roslyn-analyzer",
    ["version"] = "1.0.0",
    ["protocol_version"] = "2024-01-01"
};
Console.WriteLine(JsonSerializer.Serialize(serverInfo));
Console.Out.Flush(); // Ensure the server info is sent immediately

// --- Command Line Setup ---
var logFileOption = new Option<FileInfo?>(
    name: "--log-file",
    description: "Path to the log file.");

var rootCommand = new RootCommand("MCP Server for C# Code Analysis using Roslyn via dotnet-script")
{
    logFileOption
};

// --- Tool Definitions ---
var findUsagesCommand = new Command("FindAllUsages", "Find all usages of a symbol")
{
    new Argument<string>("solutionPath", "Path to the solution file (.sln)"),
    new Argument<string>("symbolName", "The fully qualified name of the symbol to find"),
    new Argument<string?>("filePath", () => null, "Optional: Path to the file containing the symbol definition"),
    new Argument<int?>("line", () => null, "Optional: Line number for the symbol definition (0-based)")
};

findUsagesCommand.SetHandler(async (string solutionPath, string symbolName, string? filePath, int? line, FileInfo? logFile) => {
    await HandleFindAllUsagesAsync(solutionPath, symbolName, filePath, line, logFile);
}, new Argument<string>("solutionPath"), 
   new Argument<string>("symbolName"), 
   new Argument<string?>("filePath"), 
   new Argument<int?>("line"),
   logFileOption);

var listClassesCommand = new Command("ListClasses", "List all classes in a project")
{
    new Argument<string>("projectPath", "Path to the project file (.csproj)")
};

listClassesCommand.SetHandler(async (string projectPath, FileInfo? logFile) => {
    await HandleListClassesAsync(projectPath, logFile);
}, new Argument<string>("projectPath"), logFileOption);

var findImplementationsCommand = new Command("FindImplementations", "Find implementations of an interface or method")
{
    new Argument<string>("solutionPath", "Path to the solution file (.sln)"),
    new Argument<string>("symbolName", "The fully qualified name of the interface or method symbol")
};

findImplementationsCommand.SetHandler(async (string solutionPath, string symbolName, FileInfo? logFile) => {
    await HandleFindImplementationsAsync(solutionPath, symbolName, logFile);
}, new Argument<string>("solutionPath"), 
   new Argument<string>("symbolName"),
   logFileOption);

// --- Add tools to root command ---
rootCommand.AddCommand(findUsagesCommand);
rootCommand.AddCommand(listClassesCommand);
rootCommand.AddCommand(findImplementationsCommand);

// Set the handler for the root command (runs when no specific tool is called)
rootCommand.SetHandler(async (InvocationContext context) => {
    var logFile = context.ParseResult.GetValueForOption(logFileOption);
    SetupLogging(logFile);

    Log("MCP Server Root Handler Started. Waiting for JSON commands on stdin...");
    await ProcessMcpRequestsAsync(rootCommand, logFile);
});

// This is the entry point when run normally
var args = Environment.GetCommandLineArgs().Skip(1).ToArray(); // Skip the first arg which is the script path
return await rootCommand.InvokeAsync(args);

// --- JSON Processing Loop ---
async Task ProcessMcpRequestsAsync(RootCommand root, FileInfo? logFile)
{
    SetupLogging(logFile);
    Log("Starting MCP Request Processing Loop...");
    using var reader = new StreamReader(Console.OpenStandardInput());
    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        Log($"Received line: {line}");
        if (string.IsNullOrWhiteSpace(line)) continue;

        try
        {
            using var jsonDoc = JsonDocument.Parse(line);
            if (jsonDoc.RootElement.TryGetProperty("command", out var commandElement) &&
                jsonDoc.RootElement.TryGetProperty("arguments", out var argsElement))
            {
                string? commandName = commandElement.GetString();
                if (commandName == null)
                {
                    Log("Command name is null");
                    continue;
                }

                Log($"Parsed command: {commandName}");

                // Construct args for System.CommandLine
                var commandArgs = new List<string> { commandName };
                if (argsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in argsElement.EnumerateObject())
                    {
                        // Convert JSON property name (camelCase/PascalCase) to kebab-case for CLI option
                        string optionName = $"--{ToKebabCase(prop.Name)}";
                        commandArgs.Add(optionName);
                        
                        // Handle different JSON value kinds appropriately for CLI arguments
                        commandArgs.Add(prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                            JsonValueKind.Number => prop.Value.GetRawText(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            JsonValueKind.Null => string.Empty, // Or handle as needed
                            _ => prop.Value.ToString() // Fallback
                        });
                    }
                }
                else
                {
                    Log("Arguments are not a JSON object, skipping processing.");
                    continue;
                }

                // Add log file if specified via initial CLI args (passed through context)
                if (logFile != null)
                {
                    // Don't re-add if already present from JSON
                    bool logFileArgPresent = false;
                    for(int i=0; i < commandArgs.Count - 1; ++i) {
                        if (commandArgs[i].Equals("--log-file", StringComparison.OrdinalIgnoreCase)) {
                            logFileArgPresent = true;
                            break;
                        }
                    }
                    if (!logFileArgPresent) {
                        commandArgs.Add("--log-file");
                        commandArgs.Add(logFile.FullName);
                    }
                }

                Log($"Invoking System.CommandLine with args: {string.Join(" ", commandArgs)}");
                await root.InvokeAsync(commandArgs.ToArray());
                Log("System.CommandLine invocation finished.");
            }
            else
            {
                Log("Received JSON does not contain 'command' and 'arguments' properties.");
            }
        }
        catch (JsonException jsonEx)
        {
            Log($"JSON Parsing Error: {jsonEx.Message}");
            Console.WriteLine(JsonSerializer.Serialize(new { type = "error", message = $"JSON Parsing Error: {jsonEx.Message}" }));
        }
        catch (Exception ex)
        {
            Log($"Error processing MCP request: {ex.ToString()}");
            Console.WriteLine(JsonSerializer.Serialize(new { type = "error", message = $"Internal Server Error: {ex.Message}" }));
        }
        Console.Out.Flush(); // Ensure output is sent after each command
        Console.Error.Flush(); // Ensure logs are sent
    }
    Log("Exited MCP Request Processing Loop.");
}


// --- Tool Handler Implementations ---

async Task HandleFindAllUsagesAsync(string solutionPath, string symbolName, string? filePath, int? line, FileInfo? logFile)
{
    SetupLogging(logFile);
    Log($"HandleFindAllUsagesAsync called: Solution='{solutionPath}', Symbol='{symbolName}', File='{filePath}', Line='{line}'");
    string resultJson = "[]"; // Default to empty array
    string? error = null;
    try
    {
        using var workspace = MSBuildWorkspace.Create();
        Log("Opening solution...");
        var progressReporter = new ConsoleProgressReporter();
        var solution = await workspace.OpenSolutionAsync(solutionPath, progressReporter, CancellationToken.None);
        Log("Solution opened. Finding symbol...");

        ISymbol? symbolToFind = await FindSymbolAsync(solution, symbolName, filePath, line, CancellationToken.None);

        if (symbolToFind == null)
        {
            error = $"Symbol '{symbolName}' not found in the solution.";
            Log(error);
        }
        else
        {
            Log($"Symbol found: {symbolToFind.ToDisplayString()}. Finding references...");
            var references = await SymbolFinder.FindReferencesAsync(symbolToFind, solution, CancellationToken.None);
            Log($"Found {references.Sum(r => r.Locations.Count())} references.");

            var results = new List<FindUsagesResult>();
            foreach (var referencedSymbol in references)
            {
                foreach (var location in referencedSymbol.Locations)
                {
                    var lineSpan = location.Location.GetLineSpan();
                    string preview = string.Empty;
                    try {
                        preview = location.Location.SourceTree?.GetText(CancellationToken.None).Lines[lineSpan.StartLinePosition.Line].ToString().Trim() ?? string.Empty;
                    } catch (Exception ex) {
                        Log($"Error getting preview text: {ex.Message}");
                    }

                    results.Add(new FindUsagesResult
                    {
                        FilePath = lineSpan.Path,
                        Line = lineSpan.StartLinePosition.Line, // 0-based
                        Character = lineSpan.StartLinePosition.Character, // 0-based
                        Preview = preview
                    });
                }
                // Include definition location
                var definitionLocation = referencedSymbol.Definition.Locations.FirstOrDefault();
                if (definitionLocation != null && definitionLocation.IsInSource)
                {
                    var defLineSpan = definitionLocation.GetLineSpan();
                    string preview = string.Empty;
                    try {
                        preview = definitionLocation.SourceTree?.GetText(CancellationToken.None).Lines[defLineSpan.StartLinePosition.Line].ToString().Trim() ?? string.Empty;
                    } catch (Exception ex) {
                        Log($"Error getting definition preview text: {ex.Message}");
                    }

                    results.Add(new FindUsagesResult
                    {
                        FilePath = defLineSpan.Path,
                        Line = defLineSpan.StartLinePosition.Line,
                        Character = defLineSpan.StartLinePosition.Character,
                        Preview = preview,
                        IsDefinition = true
                    });
                }
            }
            resultJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = false });
            Log("FindAllUsages completed successfully.");
        }
    }
    catch (Exception ex)
    {
        error = $"Error in FindAllUsages: {ex.ToString()}";
        Log(error);
    }

    // Send result back via MCP
    var response = new Dictionary<string, object> { ["type"] = "tool_call_result" };
    if (error != null)
        response["error"] = error;
    else
        response["result"] = JsonDocument.Parse(resultJson).RootElement;

    Console.WriteLine(JsonSerializer.Serialize(response));
    Console.Out.Flush();
}

async Task HandleListClassesAsync(string projectPath, FileInfo? logFile)
{
   SetupLogging(logFile);
   Log($"HandleListClassesAsync called: Project='{projectPath}'");
    string resultJson = "[]";
    string? error = null;
    try
    {
        using var workspace = MSBuildWorkspace.Create();
        Log("Opening project...");
        var progressReporter = new ConsoleProgressReporter();
        var project = await workspace.OpenProjectAsync(projectPath, progressReporter, CancellationToken.None);
        Log("Project opened. Getting compilation...");
        var compilation = await project.GetCompilationAsync(CancellationToken.None);
        if (compilation == null)
        {
            error = "Could not get compilation for the project.";
            Log(error);
        }
        else
        {
            Log("Compilation obtained. Finding class symbols...");
            var classSymbols = new List<ClassInfo>();
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol != null)
                    {
                        var methods = classSymbol.GetMembers()
                                                .OfType<IMethodSymbol>()
                                                .Where(m => m.MethodKind == MethodKind.Ordinary) // Only normal methods
                                                .Select(m => m.Name)
                                                .ToList();

                        var location = classSymbol.Locations.FirstOrDefault();
                        var lineSpan = location?.GetLineSpan();

                        classSymbols.Add(new ClassInfo
                        {
                            Name = classSymbol.Name,
                            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
                            FilePath = location?.SourceTree?.FilePath,
                            Line = lineSpan?.StartLinePosition.Line,
                            Methods = methods
                        });
                    }
                }
            }
            resultJson = JsonSerializer.Serialize(classSymbols, new JsonSerializerOptions { WriteIndented = false });
            Log($"Found {classSymbols.Count} classes. ListClasses completed successfully.");
        }
    }
    catch (Exception ex)
    {
        error = $"Error in ListClasses: {ex.ToString()}";
        Log(error);
    }

    // Send result back via MCP
    var response = new Dictionary<string, object> { ["type"] = "tool_call_result" };
    if (error != null)
        response["error"] = error;
    else
        response["result"] = JsonDocument.Parse(resultJson).RootElement;

    Console.WriteLine(JsonSerializer.Serialize(response));
    Console.Out.Flush();
}

async Task HandleFindImplementationsAsync(string solutionPath, string symbolName, FileInfo? logFile)
{
    SetupLogging(logFile);
    Log($"HandleFindImplementationsAsync called: Solution='{solutionPath}', Symbol='{symbolName}'");
    string resultJson = "[]";
    string? error = null;
    try
    {
        using var workspace = MSBuildWorkspace.Create();
        Log("Opening solution...");
        var progressReporter = new ConsoleProgressReporter();
        var solution = await workspace.OpenSolutionAsync(solutionPath, progressReporter, CancellationToken.None);
        Log("Solution opened. Finding base symbol...");

        ISymbol? baseSymbol = await FindSymbolAsync(solution, symbolName, null, null, CancellationToken.None);

        if (baseSymbol == null)
        {
            error = $"Base symbol '{symbolName}' not found in the solution.";
            Log(error);
        }
        else
        {
            Log($"Base symbol found: {baseSymbol.ToDisplayString()}. Finding implementations/overrides...");
            var implementations = new List<FindImplementationsResult>();

            if (baseSymbol is INamedTypeSymbol interfaceSymbol && interfaceSymbol.TypeKind == TypeKind.Interface)
            {
                var impls = await SymbolFinder.FindImplementationsAsync(interfaceSymbol, solution, cancellationToken: CancellationToken.None);
                Log($"Found {impls.Count()} direct interface implementations.");
                foreach (var implSymbol in impls)
                {
                    var location = implSymbol.Locations.FirstOrDefault();
                    if (location != null && location.IsInSource)
                    {
                        var lineSpan = location.GetLineSpan();
                        string preview = string.Empty;
                        try {
                            preview = location.SourceTree?.GetText(CancellationToken.None).Lines[lineSpan.StartLinePosition.Line].ToString().Trim() ?? string.Empty;
                        } catch (Exception ex) {
                            Log($"Error getting implementation preview text: {ex.Message}");
                        }

                        implementations.Add(new FindImplementationsResult
                        {
                            SymbolName = implSymbol.ToDisplayString(),
                            FilePath = lineSpan.Path,
                            Line = lineSpan.StartLinePosition.Line,
                            Preview = preview
                        });
                    }
                }
            }
            else if (baseSymbol is IMethodSymbol methodSymbol && (methodSymbol.IsAbstract || methodSymbol.IsVirtual || methodSymbol.ContainingType.TypeKind == TypeKind.Interface))
            {
                var overrides = await SymbolFinder.FindOverridesAsync(methodSymbol, solution, cancellationToken: CancellationToken.None);
                Log($"Found {overrides.Count()} method overrides.");
                foreach (var overrideSymbol in overrides)
                {
                    var location = overrideSymbol.Locations.FirstOrDefault();
                    if (location != null && location.IsInSource)
                    {
                        var lineSpan = location.GetLineSpan();
                        string preview = string.Empty;
                        try {
                            preview = location.SourceTree?.GetText(CancellationToken.None).Lines[lineSpan.StartLinePosition.Line].ToString().Trim() ?? string.Empty;
                        } catch (Exception ex) {
                            Log($"Error getting override preview text: {ex.Message}");
                        }

                        implementations.Add(new FindImplementationsResult
                        {
                            SymbolName = overrideSymbol.ToDisplayString(),
                            FilePath = lineSpan.Path,
                            Line = lineSpan.StartLinePosition.Line,
                            Preview = preview
                        });
                    }
                }
            }
            else {
                error = $"Symbol '{symbolName}' is not an interface or an overridable method.";
                Log(error);
            }
            
            if (error == null) {
                resultJson = JsonSerializer.Serialize(implementations, new JsonSerializerOptions { WriteIndented = false });
                Log("FindImplementations completed successfully.");
            }
        }
    }
    catch (Exception ex)
    {
        error = $"Error in FindImplementations: {ex.ToString()}";
        Log(error);
    }

    // Send result back via MCP
    var response = new Dictionary<string, object> { ["type"] = "tool_call_result" };
    if (error != null)
        response["error"] = error;
    else
        response["result"] = JsonDocument.Parse(resultJson).RootElement;

    Console.WriteLine(JsonSerializer.Serialize(response));
    Console.Out.Flush();
}


// --- Utility Functions ---

TextWriter? logWriter = null;
object logLock = new object();

void SetupLogging(FileInfo? logFile)
{
    lock (logLock)
    {
        if (logWriter == null && logFile != null)
        {
            try
            {
                // Ensure directory exists
                logFile.Directory?.Create(); 
                // Append mode, thread-safe via StreamWriter's internal synchronization (usually)
                logWriter = new StreamWriter(logFile.FullName, append: true, Encoding.UTF8) { AutoFlush = true };
                Console.Error.WriteLine($"Logging initialized to file: {logFile.FullName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to initialize file logger: {ex.Message}");
                logWriter = null;
            }
        }
    }
}

void Log(string message)
{
    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    string logMessage = $"[{timestamp}] {message}";
    // Always write to stderr for immediate visibility if possible
    Console.Error.WriteLine(logMessage); 
    Console.Error.Flush();
    // Write to file if logging is set up
    lock(logLock) {
        logWriter?.WriteLine(logMessage);
    }
}

// Helper to convert camelCase/PascalCase to kebab-case for System.CommandLine options
string ToKebabCase(string value)
{
    if (string.IsNullOrEmpty(value)) return value;
    return System.Text.RegularExpressions.Regex.Replace(
        value,
        "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])",
        "-$1",
        System.Text.RegularExpressions.RegexOptions.Compiled)
        .Trim()
        .ToLower();
}

// Helper to find a symbol potentially using file/line context
async Task<ISymbol?> FindSymbolAsync(Solution solution, string symbolName, string? filePath, int? line, CancellationToken cancellationToken)
{
    Log($"Attempting to find symbol: '{symbolName}' with FilePath: '{filePath}' and Line: '{line}'");
    
    // If file path and line are provided, try to get the symbol directly at that location first.
    if (!string.IsNullOrEmpty(filePath) && line.HasValue)
    {
        Log($"File path and line provided. Searching specific location...");
        // Find document across all projects that matches the file path
        var document = solution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => StringComparer.OrdinalIgnoreCase.Equals(d.FilePath, filePath));
            
        if (document != null)
        {
            Log($"Document found: {document.Name}. Getting semantic model...");
            var model = await document.GetSemanticModelAsync(cancellationToken);
            if (model != null)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                if (syntaxTree != null)
                {
                    var root = await syntaxTree.GetRootAsync(cancellationToken);
                    var text = await document.GetTextAsync(cancellationToken);
                    if (line.Value >= 0 && line.Value < text.Lines.Count)
                    {
                        var textSpan = text.Lines[line.Value].Span;
                        // Adjusting span slightly around the line might be needed, depending on exact definition point
                        var nodes = root.DescendantNodes(textSpan).Where(n => n.Span.IntersectsWith(textSpan));
                        
                        Log($"Found {nodes.Count()} nodes intersecting line {line.Value}. Checking symbols...");
                        foreach(var node in nodes.OrderBy(n => n.Span.Length)) // Prefer smaller nodes
                        {
                           var symbolInfo = model.GetSymbolInfo(node, cancellationToken);
                           var symbol = symbolInfo.Symbol ?? model.GetDeclaredSymbol(node, cancellationToken);
                            if (symbol != null)
                           {
                                string foundSymbolName = symbol.ToDisplayString();
                                Log($"Symbol found at location: {foundSymbolName}");
                                // Check if it matches the requested name (or is contained within it, e.g., method in a class)
                                if (foundSymbolName.EndsWith(symbolName) || symbolName.EndsWith(foundSymbolName)) // Simple check
                                {
                                    Log("Symbol at location matches requested name. Returning this symbol.");
                                    return symbol;
                                }
                           }
                        }
                        Log("No matching symbol found exactly at line location, proceeding to solution-wide search.");
                    }
                    else {
                         Log("Invalid line number provided.");
                    }
                }
            }
        }
        else {
            Log("Document path not found in solution.");
        }
    }

    Log($"Searching solution-wide for symbol '{symbolName}'...");
    // Fallback or primary search: Iterate through projects to find the symbol by name.
    foreach (var project in solution.Projects)
    {
        Log($"Checking project: {project.Name}");
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation != null)
        {
            // Use known enum values for SymbolFilter
            var symbols = compilation.GetSymbolsWithName(
                name => name == symbolName || symbolName.EndsWith("." + name), 
                SymbolFilter.Type | SymbolFilter.Member, 
                cancellationToken
            );
            
            if (!symbols.Any() && symbolName.Contains('.'))
            {
                // Try finding the containing type/namespace first if the full name wasn't found directly
                string potentialContainer = symbolName.Substring(0, symbolName.LastIndexOf('.'));
                var containerSymbols = compilation.GetSymbolsWithName(
                    name => name == potentialContainer,
                    SymbolFilter.Namespace | SymbolFilter.Type,
                    cancellationToken
                );
                
                foreach (var container in containerSymbols)
                {
                    string memberName = symbolName.Substring(symbolName.LastIndexOf('.') + 1);
                    IEnumerable<ISymbol> memberSymbols = Array.Empty<ISymbol>();
                    
                    if (container is INamespaceSymbol nsSymbol)
                    {
                        memberSymbols = nsSymbol.GetMembers(memberName);
                    }
                    else if (container is INamedTypeSymbol typeSymbol)
                    {
                        memberSymbols = typeSymbol.GetMembers(memberName);
                    }
                    
                    if (memberSymbols.Any())
                    {
                        symbols = memberSymbols;
                        break;
                    }
                }
            }

            var foundSymbol = symbols.FirstOrDefault(s => s.ToDisplayString() == symbolName); // Prefer exact match
                                 
            if (foundSymbol != null)
            {
                Log($"Symbol '{symbolName}' found in project {project.Name}.");
                return foundSymbol;
            }
        }
    }

    Log($"Symbol '{symbolName}' not found anywhere in the solution.");
    return null;
}

// Progress reporter for MSBuildWorkspace
private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
{
    public void Report(ProjectLoadProgress loadProgress)
    {
        // Optionally log detailed progress
        // Console.Error.WriteLine($"MSBuild Load: {loadProgress.Operation} - {loadProgress.FilePath} ({loadProgress.ElapsedTime})");
    }
}

// Result structure for serialization
public class FindUsagesResult
{
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public int Character { get; set; }
    public string? Preview { get; set; }
    public bool IsDefinition { get; set; } = false;
}

public class ClassInfo
{
    public string? Name { get; set; }
    public string? Namespace { get; set; }
    public string? FilePath { get; set; }
    public int? Line { get; set; }
    public List<string>? Methods { get; set; }
}

public class FindImplementationsResult
{
    public string? SymbolName { get; set; }
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public string? Preview { get; set; }
}

// Dispose log writer if needed (optional, Console.Error handles itself)
AppDomain.CurrentDomain.ProcessExit += (s, e) => {
    lock(logLock) {
         logWriter?.Dispose();
    }
};
