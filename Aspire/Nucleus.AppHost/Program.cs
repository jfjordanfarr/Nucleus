using Aspire.Hosting;
using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics; // For Demystifier

namespace Nucleus.AppHost;

public static class Program
{
    private static string? _logFilePath; // CS8618
    private static readonly object _logLock = new object();

    private static void InitializeLogFile(string[]? argsForContext = null) // CS8625
    {
        if (_logFilePath != null) return;

        try
        {
            string logDirectory = Path.Combine(Path.GetTempPath(), "NucleusAppHostLogs");
            Directory.CreateDirectory(logDirectory); // Ensures the directory exists
            _logFilePath = Path.Combine(logDirectory, $"AppHost_Trace_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.log");
            
            StringBuilder initialLog = new StringBuilder();
            initialLog.AppendLine($"[{DateTime.UtcNow:o}] [InitializeLogFile] Log file initialized: {_logFilePath}");
            initialLog.AppendLine($"[{DateTime.UtcNow:o}] [InitializeLogFile] Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            initialLog.AppendLine($"[{DateTime.UtcNow:o}] [InitializeLogFile] Command line args for context: {(argsForContext == null ? "N/A" : string.Join(" ", argsForContext))}");
            initialLog.AppendLine($"[{DateTime.UtcNow:o}] [InitializeLogFile] Environment.CommandLine: {Environment.CommandLine}");
            initialLog.AppendLine($"[{DateTime.UtcNow:o}] [InitializeLogFile] AppDomain.CurrentDomain.FriendlyName: {AppDomain.CurrentDomain.FriendlyName}");
            File.AppendAllText(_logFilePath, initialLog.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // Fallback if logging setup itself fails catastrophically
            Console.Error.WriteLine($"CRITICAL LOGGING INITIALIZATION FAILURE: {ex.ToStringDemystified()}");
            _logFilePath = Path.Combine(Path.GetTempPath(), $"AppHost_CRITICAL_FAILURE_{DateTime.UtcNow:yyyyMMddHHmmssfff}.log");
            if (_logFilePath != null) // CS8604 check before using _logFilePath here
            {
                File.AppendAllText(_logFilePath, $"[{DateTime.UtcNow:o}] CRITICAL LOGGING INIT FAILED: {ex.ToStringDemystified()}\n", Encoding.UTF8);
            }
        }
    }

    private static void Log(string message, [CallerMemberName] string memberName = "")
    {
        if (_logFilePath == null) 
        {
            InitializeLogFile(); 
        }

        try
        {
            string logMessage = $"[{DateTime.UtcNow:o}] [{memberName}] {message}\n";
            lock (_logLock)
            {
                if (_logFilePath != null) // CS8604
                {
                    File.AppendAllText(_logFilePath, logMessage, Encoding.UTF8);
                }
                else
                {
                    Console.Error.WriteLine($"Log file path is null. Cannot write log: {message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to write to log file ({_logFilePath ?? "NULL"}): {ex.Message}. Message was: {message}");
        }
    }

    public static IDistributedApplicationBuilder CreateDistributedApplicationBuilder(string[] args)
    {
        InitializeLogFile(args);
        Log($"Entering CreateDistributedApplicationBuilder. Args: {(args == null ? "null" : string.Join(", ", args))}");

        var builder = DistributedApplication.CreateBuilder(args ?? Array.Empty<string>());
        Log("DistributedApplication.CreateBuilder returned.");

        Log("Adding Service Defaults...");
        // builder.AddServiceDefaults(); // CS1061 - This should now work if Aspire.Hosting is correctly referenced and ServiceDefaults project is okay
        Aspire.Hosting.DistributedApplicationBuilderExtensions.AddServiceDefaults(builder);
        Log("Service Defaults ADDED.");

        Log("Skipping addition of other project resources for now to simplify debugging.");

        Log("Exiting CreateDistributedApplicationBuilder.");
        return builder;
    }

    public static void Main(string[] args)
    {
        InitializeLogFile(args); 
        Log($"Entering Main. Args: {(args == null ? "null" : string.Join(", ", args))}");

        try
        {
            Log("Calling CreateDistributedApplicationBuilder...");
            var builder = CreateDistributedApplicationBuilder(args ?? Array.Empty<string>()); // CS8604
            Log("Returned from CreateDistributedApplicationBuilder.");

            Log("Building application...");
            var app = builder.Build();
            Log("Application BUILT.");

            Log("Running application...");
            app.Run();
            Log("Application Run COMPLETED (or was non-blocking).");
        }
        catch (Exception ex)
        {
            Log($"!!! EXCEPTION in Main: {ex.ToStringDemystified()}");
            Console.Error.WriteLine($"[AppHost CRITICAL ERROR IN MAIN] {ex.ToStringDemystified()}");
            throw;
        }
        Log("Exiting Main.");
    }
}
