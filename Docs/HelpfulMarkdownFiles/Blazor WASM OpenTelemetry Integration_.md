# **Integrating OpenTelemetry in Blazor WebAssembly with.NET Aspire: Current State, Challenges, and Practical Solutions**

## **1\. Executive Summary**

Integrating OpenTelemetry directly from Blazor WebAssembly (WASM) applications into the.NET Aspire dashboard to achieve comprehensive observability—encompassing logs, traces, and metrics—is **not an officially supported scenario** out-of-the-box within the.NET Aspire framework as of its GA release (version 8.x) and subsequent.NET 9 previews.1 The standard .AddServiceDefaults() extension method, which significantly simplifies OpenTelemetry setup for backend services within an Aspire solution 3, is fundamentally incompatible with the browser-based execution environment of Blazor WASM due to inherent platform differences.5

The primary obstacle stems from the Blazor WASM execution model:.NET code runs inside a sandboxed browser environment using WebAssembly technology.6 This sandbox environment restricts access to certain system resources and underlying.NET APIs, such as System.Diagnostics.Process, which are utilized by components within the standard OpenTelemetry.NET SDK.5 Attempting direct instrumentation with the.NET SDK often results in runtime errors like PlatformNotSupportedException 5 or necessitates complex and unsustainable modifications to the SDK itself.9

Despite these challenges, a viable and recommended path forward exists: leveraging the **OpenTelemetry JavaScript (JS) SDK** through Blazor's built-in **JavaScript interop** capabilities.6 This strategy aligns with standard web development observability practices by instrumenting the application at the JavaScript layer, allowing for the capture of browser-specific events, user interactions, and crucial network requests made from the client.3

It is crucial to understand the limitations of the.NET Aspire dashboard in this context. The dashboard is primarily designed as a **local development tool** focused on visualizing telemetry (logs, traces, metrics) originating from backend services.12 Official statements and technical analysis confirm that it **does not currently support the ingestion or visualization of logs or metrics** sent from browser clients using the OpenTelemetry JS SDK.1 However, evidence from community experiments and an understanding of the underlying OTLP/HTTP protocol suggest that **traces** exported from the JS SDK *can likely* be successfully ingested and displayed within the dashboard's "Traces" view.3 This capability enables the valuable correlation of frontend user actions with backend service operations during local development.

This report aims to provide a detailed analysis of the challenges and limitations surrounding Blazor WASM observability within.NET Aspire. Furthermore, it offers practical, step-by-step guidance for implementing the recommended OpenTelemetry JS SDK approach via JavaScript interop. By following this guidance, developers can achieve essential trace observability for their Blazor WASM applications within the Aspire local development environment, addressing the critical need for better error diagnosis and visibility into request flows across their distributed system.

## **2\. Introduction: The Need for Observability in Blazor WASM**

Modern distributed applications, often composed of multiple microservices and a frontend client, present significant operational challenges. Understanding the internal state and behavior of such systems based on the data they emit—the core principle of observability—is crucial for debugging, performance tuning, and ensuring reliability.15 This need is particularly acute for frontend applications like those built with Blazor WebAssembly (WASM), where diagnosing errors or understanding performance bottlenecks can be difficult without integrated visibility tools. Developers often find themselves relying solely on the browser's developer console, lacking a centralized view of how frontend actions correlate with backend processes. Observability extends beyond simple logging; it encompasses metrics (numerical measurements over time) and traces (tracking requests across service boundaries) to provide a holistic understanding of system behavior.12

Blazor WASM introduces a unique execution model: C\# code is compiled into.NET assemblies, which are then executed client-side within the browser using a.NET runtime implemented in WebAssembly.6 This contrasts sharply with server-side frameworks or Blazor Server, where the code runs with full access to server resources. The browser's security sandbox, while essential for user safety, imposes limitations on what the running WASM code can access, creating distinct observability challenges compared to backend services.8

OpenTelemetry (OTel) has emerged as the industry standard, vendor-neutral framework for instrumenting applications to generate, collect, and export telemetry data (logs, traces, metrics).12 Its adoption provides a consistent approach to observability across diverse services, languages, and platforms. This standardization is particularly relevant within the.NET Aspire ecosystem, which heavily leverages and integrates OpenTelemetry for its backend components to streamline observability setup.4

.NET Aspire itself is an opinionated stack designed to simplify the development of cloud-native, distributed.NET applications.12 Integrated observability is a cornerstone of its vision. The Aspire Dashboard serves as the primary visualization tool during local development, offering views for logs, traces, and metrics collected from the application's components.3 For backend projects (like APIs or worker services), Aspire utilizes a ServiceDefaults project and the AddServiceDefaults() extension method.3 This helper method automatically configures the OpenTelemetry.NET SDK for logging, tracing, and metrics, and sets up an OpenTelemetry Protocol (OTLP) exporter. The exporter is configured via environment variables automatically injected by Aspire, directing telemetry data to the dashboard's OTLP endpoint, typically listening on http://localhost:4317.12 It's important to remember that the Aspire dashboard is intended solely for local development and debugging; it lacks the persistence and scalability features required for production monitoring.14

This streamlined, almost "zero-configuration" observability experience provided by Aspire for backend services highlights a significant gap: this automated setup **does not extend** to Blazor WASM frontend applications.1 The mechanisms used in ServiceDefaults are incompatible with the Blazor WASM environment, leaving developers to seek alternative solutions for achieving frontend observability within their Aspire-orchestrated applications.

## **3\. The Core Problem: Why Standard.NET OpenTelemetry Fails in Blazor WASM**

The attempt to directly apply the standard OpenTelemetry.NET SDK packages—such as OpenTelemetry.Extensions.Hosting, OpenTelemetry.Instrumentation.AspNetCore, and OpenTelemetry.Instrumentation.Http—within a Blazor WASM application encounters fundamental roadblocks due to platform limitations and incompatibilities.

**Unsupported APIs and Sandbox Restrictions:** The most significant issue arises from the Blazor WASM execution environment itself. The.NET code runs within a secure browser sandbox powered by WebAssembly, which deliberately restricts access to many underlying operating system functions and certain.NET APIs for security and cross-platform compatibility reasons.6 The OpenTelemetry.NET SDK, designed primarily for server-side or desktop environments with broader access, relies on some of these restricted APIs. A prime example is the use of System.Diagnostics.Process.GetCurrentProcess(), often employed internally by OTel components to gather process-related information for resource identification (like automatically determining the service name).5 Calling such APIs from within the WASM sandbox results in a PlatformNotSupportedException at runtime. This specific exception and its context within Blazor WASM instrumentation are well-documented in community discussions and GitHub issues, such as open-telemetry/opentelemetry-dotnet\#2816.5 This isn't merely a bug in a single component but reflects a core incompatibility between the SDK's expectations and the constraints of the WASM environment.

**Potential Threading Model Conflicts:** Further complications can arise from assumptions made by background processing components within the OTel.NET SDK. For instance, processors like the BatchExportProcessor often utilize background threads or tasks for efficient telemetry batching and exporting. The threading model in Blazor WASM has historically been single-threaded (though experimental multithreading support exists). Discussions within the OpenTelemetry community suggest that the SDK's reliance on mechanisms like EventListener and background services might conflict with this single-threaded nature, potentially requiring exporters specifically designed for such environments.5

**Impracticality of SDK Modification:** While technically possible, attempting to work around these issues by modifying the OpenTelemetry.NET SDK source code—for example, by removing calls to unsupported APIs as explored in some community blog posts 9—is not a sustainable solution. It requires maintaining a custom fork of the SDK, making updates difficult and introducing significant fragility.9

**Aspire ServiceDefaults Project Incompatibility:** The streamlined observability setup offered by.NET Aspire via the ServiceDefaults project 4 also fails when applied to Blazor WASM. The core reason, identified in discussions like dotnet/aspire\#1284, is that the ServiceDefaults project template includes a framework reference to Microsoft.AspNetCore.App (\<FrameworkReference Include="Microsoft.AspNetCore.App" /\>).1 This reference implicitly brings in numerous server-side ASP.NET Core assemblies and dependencies that are incompatible with the client-side Blazor WASM runtime environment, leading to build failures or runtime errors.1 Consequently, developers are forced to remove the reference to the ServiceDefaults project from their Blazor WASM application. While this allows the application to build and run, it completely bypasses the automated OpenTelemetry configuration, health checks, and service discovery benefits that ServiceDefaults provides.1

**Official Stance and Confirmation:** The lack of straightforward support is not an oversight but reflects the current technical realities. Key members of the.NET Aspire team have explicitly confirmed in public forums (like GitHub issues) that telemetry integration from Blazor WASM into the Aspire dashboard is not a supported scenario at present, acknowledging the incompatibility of ServiceDefaults with WASM.1

These factors collectively point to a fundamental mismatch. The OpenTelemetry.NET SDK and Aspire's ServiceDefaults are designed with the assumption of a traditional.NET execution environment, possessing full runtime capabilities and operating system access typical of server-side applications.17 Blazor WASM operates under the significantly different and more restrictive rules of the browser sandbox.5 Therefore, a successful observability strategy for Blazor WASM requires tools and techniques specifically designed for, or adaptable to, the browser environment. Furthermore, this situation reflects.NET Aspire's initial design focus, which centers heavily on simplifying the orchestration, configuration, and observability of *backend* distributed systems components.13 While Aspire can launch and manage frontend projects, the deep, automated integrations seen in backend services, particularly for observability via ServiceDefaults, do not currently translate to the client-side WASM world.1 Developers must therefore adopt web-standard approaches for frontend instrumentation.

## **4\. The Viable Workaround: Leveraging the OpenTelemetry JavaScript SDK via JS Interop**

Given the incompatibility of the standard.NET OpenTelemetry SDK with the Blazor WASM runtime environment, the most practical and standards-aligned solution is to shift the instrumentation layer. Since Blazor WASM applications execute entirely within the web browser 6 and Blazor provides robust mechanisms for interacting with JavaScript via its JS Interop feature 6, the logical approach is to utilize the **OpenTelemetry JavaScript (JS) SDK**. This strategy leverages tooling specifically designed for browser environments to capture telemetry data directly at the source of user interactions and client-side network activity.

**OpenTelemetry JavaScript SDK Considerations:** The OTel JS SDK provides the necessary components for browser-based instrumentation. While its development is ongoing, particularly regarding browser-specific features, the core tracing APIs and the OTLP exporter components are generally considered stable.3 Support for logging signals in the browser is still evolving.3 Crucially for integration with tools like the Aspire dashboard, the SDK includes exporters capable of sending telemetry data using the OTLP protocol over HTTP/JSON (e.g., @opentelemetry/exporter-trace-otlp-http). This is essential because browsers typically cannot establish the gRPC connections often used by backend OTLP exporters.3

A significant advantage of the OTel JS SDK is its ecosystem of automatic instrumentation packages.11 These packages can automatically create trace spans for common browser activities with minimal manual coding. Examples include instrumentations for:

* fetch API calls  
* XMLHttpRequest objects (often used internally by libraries like HttpClient in Blazor WASM)  
* Initial document/page loads  
* User interactions (e.g., button clicks, route changes)

**Bridging.NET and JavaScript with Blazor JS Interop:** Blazor's JavaScript interop functionality is the key enabler for this approach. The IJSRuntime service, available through dependency injection in Blazor components and services, allows.NET code to invoke JavaScript functions defined in the global scope (window) or within imported JS modules.6 Methods like InvokeVoidAsync (for calling JS functions that don't return a value) and InvokeAsync\<T\> (for functions that return a value) facilitate this communication. This bridge allows the Blazor C\# application to trigger the initialization of the OTel JS SDK and potentially pass necessary configuration data. This use of JS Interop acts as a necessary abstraction layer, enabling the Blazor WASM application to delegate the observability task to the appropriate tool (the OTel JS SDK) operating within its native context (the browser), thereby working *with* the browser environment rather than against the limitations of the WASM sandbox.

**Implementation Strategy and Steps:** Implementing this workaround involves coordinating setup in both the Blazor WASM project and the associated JavaScript code.

1. **Project Setup (NPM & Dependencies):**  
   * Introduce Node.js package management to the Blazor WASM project. Create a package.json file in the project's root directory (or a suitable sub-directory).  
   * Use npm install (or yarn add, pnpm add) to install the required OpenTelemetry JavaScript packages as development dependencies. Refer to Table 1 below for essential packages.  
   * Ensure your build process handles these Node dependencies if necessary (e.g., using a bundler like Webpack or Parcel if complex JS is involved, although simple script includes might suffice).  
2. **JavaScript Initialization File:**  
   * Create a JavaScript file within the wwwroot folder (e.g., wwwroot/js/otel-init.js). This file will house the OTel JS SDK configuration and initialization logic.  
3. **Loading the Script:**  
   * Reference the created JavaScript file in the main HTML host page of the Blazor WASM application (usually wwwroot/index.html). Add a \<script\> tag, typically placed towards the end of the \<body\>, ensuring it loads after the Blazor framework script (\_framework/blazor.webassembly.js). Adapting the concept from 11:

...\<body\>...\<script src="\_framework/blazor.webassembly.js"\>\</script\>\<script src="js/otel-init.js"\>\</script\>\</body\>\</html\>\`\`\`

4. **OpenTelemetry JS Initialization Code:**  
   * Populate the otel-init.js file with the necessary code to configure and start the OTel JS SDK. This involves importing modules, creating a tracer provider, configuring the OTLP/HTTP exporter, and registering instrumentations. The code should expose a function callable from Blazor C\# to trigger initialization, accepting configuration parameters like the OTLP endpoint URL and service name.  
   * **(Conceptual JavaScript Example \- wwwroot/js/otel-init.js)**  
     JavaScript  
     // Note: Assumes packages are installed via npm and potentially bundled,  
     // or using a module-aware script tag setup. Adjust imports as needed.  
     import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';  
     import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';  
     import { SimpleSpanProcessor, BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';  
     import { ZoneContextManager } from '@opentelemetry/context-zone';  
     import { registerInstrumentations } from '@opentelemetry/instrumentation';  
     import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';  
     import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';  
     import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';  
     import { Resource } from '@opentelemetry/resources';  
     import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';

     // Expose initialization function to the global scope for Blazor interop  
     window.initializeOpenTelemetry \= (otlpEndpoint, serviceName) \=\> {  
       console.log(\`Attempting to initialize OpenTelemetry for service: ${serviceName} at endpoint: ${otlpEndpoint}\`);

       if (\!otlpEndpoint ||\!serviceName) {  
         console.error('OTLP endpoint or service name missing. OpenTelemetry initialization aborted.');  
         return;  
       }

       const resource \= new Resource({  
        : serviceName,  
         // Add other relevant resource attributes, e.g., deployment environment if available  
       });

       const provider \= new WebTracerProvider({  
         resource: resource,  
       });

       // Configure the OTLP exporter to send traces via HTTP/JSON  
       const exporter \= new OTLPTraceExporter({  
         url: otlpEndpoint, // The endpoint URL received from Blazor C\# config  
         // headers: {}, // Optional: Add custom headers if required by your collector  
         // concurrencyLimit: 10, // Optional: limits the number of concurrent exports  
       });

       // Choose a span processor:  
       // SimpleSpanProcessor: Exports spans immediately (good for dev/debug, higher overhead)  
       // BatchSpanProcessor: Batches spans before export (better for prod, less overhead)  
       provider.addSpanProcessor(new SimpleSpanProcessor(exporter));  
       // For production consider:  
       // provider.addSpanProcessor(new BatchSpanProcessor(exporter, {  
       //   scheduledDelayMillis: 500, // Export interval  
       //   maxQueueSize: 2048,      // Max spans in queue  
       //   maxExportBatchSize: 512, // Max spans per batch  
       // }));

       // Register the provider with ZoneContextManager for robust async context propagation in browsers  
       provider.register({  
         contextManager: new ZoneContextManager(),  
       });

       // Register the desired automatic instrumentations  
       registerInstrumentations({  
         instrumentations:, // Example: ignore specific URLs  
             // propagateTraceHeaderCorsUrls: \[ /.+/g \] // Ensure trace context headers are added for CORS requests  
           }),  
           // Consider adding UserInteractionInstrumentation for button clicks, route changes etc.  
           // new UserInteractionInstrumentation()  
         \],  
         // tracerProvider: provider, // Optional: Explicitly pass provider  
       });

       console.log(\`OpenTelemetry initialized successfully for service: ${serviceName}\`);  
     };

5. **Invoking Initialization from Blazor C\#:**  
   * In the Blazor WASM application's entry point (Program.cs), after the WebAssemblyHost is built, obtain instances of IConfiguration and IJSRuntime.  
   * Retrieve the necessary configuration values (OTLP endpoint URL, service name) from IConfiguration. **Crucially, ensure these values are actually available to the WASM application's configuration.** (See Configuration Passing below).  
   * Use jsRuntime.InvokeVoidAsync to call the window.initializeOpenTelemetry JavaScript function, passing the retrieved configuration values as arguments. Include error handling for the JS interop call.  
   * **(Conceptual C\# Example \- Program.cs)**  
     C\#  
     // Example: Program.cs (Blazor WASM)  
     using Microsoft.AspNetCore.Components.WebAssembly.Hosting;  
     using Microsoft.Extensions.Configuration;  
     using Microsoft.Extensions.DependencyInjection;  
     using Microsoft.JSInterop;  
     using System;  
     using System.Threading.Tasks;

     public class Program  
     {  
         public static async Task Main(string args)  
         {  
             var builder \= WebAssemblyHostBuilder.CreateDefault(args);  
             builder.RootComponents.Add\<App\>("\#app");  
             //... register HttpClient, other services

             var host \= builder.Build();

             // \--- OpenTelemetry JS Initialization \---  
             var config \= host.Services.GetRequiredService\<IConfiguration\>();  
             var jsRuntime \= host.Services.GetRequiredService\<IJSRuntime\>();

             // \*\*\* Critical Step: Obtain configuration \*\*\*  
             // Ensure these keys exist in wwwroot/appsettings.json, appsettings.Development.json,  
             // or are provided via another mechanism (see Configuration Passing Strategies).  
             var otlpEndpoint \= config; // Aspire dashboard default: http://localhost:4317  
             var serviceName \= config?? builder.HostEnvironment.ApplicationName?? "BlazorWasmClient";

             if (\!string.IsNullOrWhiteSpace(otlpEndpoint))  
             {  
                 try  
                 {  
                     Console.WriteLine($"Invoking JS OpenTelemetry initialization. Endpoint: {otlpEndpoint}, Service: {serviceName}");  
                     await jsRuntime.InvokeVoidAsync("initializeOpenTelemetry", otlpEndpoint, serviceName);  
                 }  
                 catch (JSException jsEx)  
                 {  
                     Console.Error.WriteLine($"Error calling initializeOpenTelemetry (JS): {jsEx.Message}");  
                 }  
                 catch (Exception ex)  
                 {  
                     Console.Error.WriteLine($"Error during OpenTelemetry JS Interop initialization: {ex.Message}");  
                 }  
             }  
             else  
             {  
                 Console.Warn("OTEL\_EXPORTER\_OTLP\_ENDPOINT configuration is missing. OpenTelemetry JS SDK will not be initialized.");  
             }  
             // \--- End OpenTelemetry JS Initialization \---

             await host.RunAsync();  
         }  
     }

**Configuration Passing Strategies:** Getting the OTLP endpoint URL (which Aspire typically manages for backend services) into the Blazor WASM application's configuration context is a critical hurdle. Because the WASM application runs client-side, it doesn't automatically inherit environment variables set by the Aspire AppHost process in the same way a backend service does. Developers must explicitly bridge this configuration gap:

* **Option 1: Build-time Configuration (appsettings.json):** The simplest method for local development is often to hardcode the expected Aspire dashboard OTLP endpoint (http://localhost:4317) into the Blazor WASM project's wwwroot/appsettings.Development.json file. This requires assuming the port won't change, but is easy to implement.  
  JSON  
  // Example: wwwroot/appsettings.Development.json  
  {  
    "OTEL\_EXPORTER\_OTLP\_ENDPOINT": "http://localhost:4317",  
    "OTEL\_SERVICE\_NAME": "MyBlazorApp.Web"  
  }

* **Option 2: Pass from AppHost (More Dynamic):** In the AppHost project (Program.cs), when adding the Blazor WASM project reference, attempt to inject the configuration value. Aspire provides mechanisms like .WithEnvironment() which *might* be leveraged, although direct environment variable propagation to WASM C\# IConfiguration needs careful testing due to the sandbox. The core idea is the AppHost orchestrates the value. This approach draws inspiration from techniques used to pass configuration to other frontend frameworks within Aspire 3, even if the exact mechanism needs adaptation for Blazor WASM.  
  C\#  
  // Example: AppHost/Program.cs (Conceptual \- Test WASM config access)  
  var builder \= DistributedApplication.CreateBuilder(args);  
  var cache \= builder.AddRedis("cache");  
  var apiService \= builder.AddProject\<Projects.MyApi\>("apiservice");

  // Get the dashboard's OTLP endpoint (assuming it's added as a resource or known)  
  // This part might require accessing Aspire's internal configuration or assuming the default.  
  var otlpEndpoint \= builder.Configuration?? "http://localhost:4317";

  builder.AddProject\<Projects.MyBlazorApp\_Web\>("webfrontend")  
        .WithReference(cache)  
        .WithReference(apiService)  
         // Attempt to pass the OTLP endpoint as an environment variable  
         // Check if Blazor WASM IConfiguration picks this up correctly.  
        .WithEnvironment("OTEL\_EXPORTER\_OTLP\_ENDPOINT", otlpEndpoint)  
        .WithEnvironment("OTEL\_SERVICE\_NAME", "MyBlazorApp.Web"); // Explicitly set service name

  builder.Build().Run();

* **Option 3: Configuration API Endpoint:** Create a simple configuration endpoint in one of the backend APIs managed by Aspire. The Blazor WASM application can make an HTTP request to this endpoint during its startup phase (Program.cs, before initializing OTel) to fetch dynamic configuration values, including the OTLP endpoint. This offers maximum flexibility but adds complexity.  
* **Option 4: JS Interop Argument (Combined with C\# Config Read):** As demonstrated in the C\# Program.cs example above, read the configuration value using IConfiguration within the C\# code and then pass it directly as an argument to the initializeOpenTelemetry JavaScript function. This is often the most straightforward implementation, provided the configuration value can be reliably loaded into IConfiguration using one of the other methods (e.g., from appsettings.json).

Successfully implementing one of these configuration passing strategies is non-trivial and represents a key difference compared to the automated configuration enjoyed by backend services in Aspire. It requires deliberate design and careful handling to ensure the OTel JS SDK receives the correct endpoint URL.

**Table 1: Key OpenTelemetry JavaScript Packages for Blazor WASM Tracing**

| Package Name | Purpose | Status/Notes |
| :---- | :---- | :---- |
| @opentelemetry/api | Core OpenTelemetry API interfaces and functions. | Stable, Required |
| @opentelemetry/sdk-trace-web | Web SDK implementation for tracing in browser environments. | Stable, Required for Tracing |
| @opentelemetry/exporter-trace-otlp-http | Exports traces using OTLP over HTTP/JSON. | Stable, **Required for Aspire Dashboard/Browser Export** |
| @opentelemetry/resources | Defines resource attributes (e.g., service name). | Stable, Required for identifying the service |
| @opentelemetry/semantic-conventions | Standard attribute keys for semantic correctness. | Stable, Recommended for standard attributes |
| @opentelemetry/context-zone | Context manager for handling async operations in browsers. | Stable, Highly Recommended for correct context propagation |
| @opentelemetry/instrumentation | Core package for registering instrumentations. | Stable, Required for using auto-instrumentations |
| @opentelemetry/instrumentation-document-load | Automatic instrumentation for page load timing. | Stable, Recommended for initial load tracing |
| @opentelemetry/instrumentation-fetch | Automatic instrumentation for fetch API calls. | Stable, **Essential for tracing backend API calls** |
| @opentelemetry/instrumentation-xml-http-request | Automatic instrumentation for XMLHttpRequest calls. | Stable, **Essential if using HttpClient (which uses XHR)** |
| @opentelemetry/instrumentation-user-interaction | Automatic instrumentation for user clicks, route changes. | Experimental/Stable (check latest), Useful for UI interaction tracing |

## **5\. Viewing Telemetry: The Aspire Dashboard and Beyond**

Once the OpenTelemetry JavaScript SDK is configured and exporting data from the Blazor WASM application, the next consideration is where this telemetry data is sent and how it can be visualized, particularly within the context of the.NET Aspire dashboard during local development.

**Aspire Dashboard OTLP Endpoint:** The.NET Aspire dashboard includes a built-in OTLP receiver to collect telemetry from orchestrated services. For local development, Aspire typically configures backend services to export telemetry via environment variables, pointing to the dashboard's endpoint.12 Crucially, the dashboard listens for OTLP data over **HTTP**, commonly on port 4317 (i.e., http://localhost:4317).12 While a gRPC endpoint might also be available, browser-based JavaScript exporters, like the one used for Blazor WASM, *must* target the HTTP endpoint due to browser limitations regarding gRPC.3 Developers can verify the exact OTLP endpoint URL being used by Aspire in their local environment by examining the environment variables injected into any *backend* service listed in the Aspire dashboard's "Resources" section.12 The OTEL\_EXPORTER\_OTLP\_ENDPOINT or similarly named variable will contain the target URL.

**Telemetry Visibility in the Aspire Dashboard:** The capabilities of the Aspire dashboard to display telemetry originating from a browser-based OTel JS SDK are specific and limited:

* **Traces (Likely Visible):** Given that the dashboard supports OTLP over HTTP and the OTel JS SDK can export traces using this protocol, it is highly probable that traces sent from the instrumented Blazor WASM application will appear in the dashboard's "Traces" view.13 This has been observed in community experiments with other JS frameworks integrated with Aspire.3 This trace visibility is the primary benefit of this integration for local development, as it allows developers to see the flow of requests initiated by the frontend (e.g., API calls triggered by button clicks) and correlate them with the corresponding trace spans generated by the backend services receiving those calls.  
* **Logs & Metrics (Not Visible):** It is essential to understand that the current Aspire dashboard **does not support** the ingestion or visualization of logs or metrics sent from the browser using the OTel JS SDK.1 The "Structured Logs" and "Metrics" tabs within the dashboard 13 will only display data collected from backend.NET services configured via the standard Aspire mechanisms (e.g., ServiceDefaults). This limitation has been explicitly confirmed by the.NET Aspire team.1 Therefore, for Blazor WASM telemetry, the dashboard effectively functions only as a "Trace Correlator".

**Addressing Error Diagnosis Needs:** Despite the lack of log visibility in the dashboard, the trace data can still significantly aid in diagnosing errors occurring within the Blazor WASM application:

* **Automatic Error Capture:** The automatic instrumentations for fetch and XMLHttpRequest will typically capture failed HTTP requests. These failures should appear as spans marked with an error status within the trace view in the dashboard, often including details like the HTTP status code.  
* **Manual Error Recording:** For errors occurring within Blazor component logic or other client-side operations, developers can implement manual error reporting within the trace context. This involves wrapping potentially problematic C\# code in try...catch blocks. Inside the catch block, a call can be made via JS interop to a helper JavaScript function. This JS function would then use the OTel JS API (specifically methods like span.recordException(error) and span.setStatus({ code: SpanStatusCode.ERROR, message: error.message })) to record the exception details and mark the currently active span (or a newly created one) as having an error status. These recorded exceptions and error statuses will then be visible when examining the trace details in the Aspire dashboard.  
* **Browser Developer Console:** It must be stressed that the browser's built-in developer tools console remains an indispensable tool for Blazor WASM debugging. Detailed error messages,.NET exception stack traces originating from the WASM runtime, and general diagnostic messages logged using Console.WriteLine or ILogger (configured to output to the console) will appear here. The OTel trace integration provides cross-service correlation but does not replace the need for standard browser-based debugging techniques.

**Production Monitoring and Alternative Backends:** The.NET Aspire dashboard is explicitly designed as a local development and debugging tool. It uses in-memory storage for telemetry data, often with circular buffers, meaning data is not persisted long-term and the system is not built for production scale or reliability.14

For monitoring applications in staging or production environments, telemetry data from both the Blazor WASM frontend (via OTel JS) and the backend services (via OTel.NET) must be exported to a dedicated, persistent observability backend or platform that supports the OTLP standard. Numerous options exist, including cloud provider services (like Azure Monitor Application Insights 16) and open-source or commercial solutions (such as Jaeger, Prometheus/Grafana 16, Tempo/Loki 16, Datadog 25, New Relic 8, Sentry 28, Honeycomb, etc.). Deploying to these environments necessitates changing the OTLP exporter endpoint configuration – both in the Blazor WASM JavaScript initialization code and in the backend service configurations – to point to the appropriate production OTel collector or ingestion endpoint.12

This requirement highlights an unavoidable divergence in observability configuration between local development (targeting the ephemeral Aspire dashboard) and deployed environments (targeting a persistent production backend). Managing these different endpoint configurations effectively, typically through environment variables or other externalized configuration mechanisms 12, becomes crucial. The addition of the client-side OTel JS configuration adds another layer to this environmental management challenge compared to applications consisting only of backend services managed by Aspire.

## **6\. Recommendations and Practical Guidance**

Based on the analysis of current capabilities, limitations, and community practices, the following recommendations provide a practical path forward for integrating basic OpenTelemetry observability into Blazor WASM applications within a.NET Aspire project.

**Recommended Path:** The most feasible and standards-aligned approach available *today* is to utilize the **OpenTelemetry JavaScript SDK via Blazor's JS Interop feature**. While this constitutes a workaround necessitated by the current limitations of direct.NET OTel instrumentation in WASM and the scope of Aspire's built-in support, it enables valuable trace visibility for local development and aligns with standard web observability techniques.

**Implementation Checklist & Key Steps:**

* **In the .NET Aspire AppHost Project (Program.cs):**  
  * Ensure the Blazor WASM project is added to the Aspire application model using builder.AddProject\<Projects.YourBlazorWasmProject\>("webfrontend").  
  * Determine and implement a strategy to provide the OTLP HTTP endpoint URL (e.g., http://localhost:4317 for local development) and a desired service name (e.g., YourAppName.Web) to the Blazor WASM application's runtime configuration. Options include adding values to wwwroot/appsettings.Development.json (simplest for fixed local endpoint), attempting injection via .WithEnvironment() (requires testing WASM config access), or providing a dedicated configuration API endpoint.  
* **In the Blazor WASM Project:**  
  * Initialize Node.js package management (npm init or similar) and create a package.json.  
  * Install the required OpenTelemetry JS packages using npm install \--save-dev \<package-name\> (refer to Table 1 for key packages like @opentelemetry/sdk-trace-web, @opentelemetry/exporter-trace-otlp-http, @opentelemetry/instrumentation-fetch, @opentelemetry/instrumentation-xml-http-request).  
  * Create the JavaScript initialization file (e.g., wwwroot/js/otel-init.js) containing the OTel JS SDK setup code, including provider creation, OTLP/HTTP exporter configuration, and registration of desired automatic instrumentations (see Section 4 example).  
  * Include the \<script src="js/otel-init.js"\>\</script\> tag in wwwroot/index.html, ensuring it loads *after* the main Blazor script.  
  * In Program.cs (Main method, after host \= builder.Build();):  
    * Retrieve IConfiguration and IJSRuntime from the host.Services.  
    * Read the OTLP endpoint URL and service name from the IConfiguration instance (ensure the values are present based on the chosen configuration passing strategy).  
    * Call the JavaScript initialization function using await jsRuntime.InvokeVoidAsync("initializeOpenTelemetry", otlpEndpoint, serviceName);, including appropriate null/empty checks and error handling (try-catch).  
  * Ensure any HttpClient instances used to communicate with backend APIs are registered in Program.cs using builder.Services.AddHttpClient(...) or similar, as the OTel JS fetch/xhr instrumentations will automatically trace requests made through these configured clients.

**Error Capturing Strategy:**

* **Automatic:** Leverage the FetchInstrumentation and XMLHttpRequestInstrumentation to automatically capture failed HTTP requests. These should appear as spans with an error status in the Aspire dashboard's trace view.  
* **Manual:** For critical application logic errors within Blazor components:  
  * Wrap relevant C\# code sections in try...catch blocks.  
  * In the catch block, invoke a dedicated JS interop function (e.g., window.recordOtelError(errorMessage, stackTrace)).  
  * Implement the corresponding JavaScript function (recordOtelError) to use the OTel JS API (api.trace.getActiveSpan()?.recordException(errorObject); or api.trace.getActiveSpan()?.setStatus({ code: SpanStatusCode.ERROR, message });) to record the error details on the currently active trace span.  
* **Console Logging:** Continue to use ILogger (configured for console output) or Console.WriteLine within C\# code for detailed debugging messages. These are essential for development but will *not* be sent to the Aspire dashboard via OpenTelemetry.

**Performance Considerations:** Be mindful that client-side instrumentation introduces some performance overhead (CPU for processing, memory for storing span data, network bandwidth for exporting telemetry).

* Start with essential instrumentations: DocumentLoadInstrumentation, FetchInstrumentation, XMLHttpRequestInstrumentation.  
* Add more comprehensive instrumentations like UserInteractionInstrumentation cautiously, monitoring the impact on application responsiveness and resource usage in the browser.  
* For production, prefer the BatchSpanProcessor over the SimpleSpanProcessor in the JavaScript configuration to reduce export frequency and overhead.

**Staying Informed:** The landscape for.NET, Aspire, and OpenTelemetry is continually evolving. Monitor the following resources for potential future improvements or official solutions for Blazor WASM observability:

* **GitHub Repositories:**  
  * dotnet/aspire (Issues and Releases) 1  
  * open-telemetry/opentelemetry-dotnet (Issues) 5  
  * open-telemetry/opentelemetry-js (Issues and Releases)  
* **Official Documentation & Blogs:**  
  * .NET Blog (Announcements)  
  * .NET Aspire Documentation 4  
* **Conferences:** Look for relevant sessions or workshops at events like.NET Conf 24 or NDC Conferences 27, although specific Blazor WASM OTel talks might be rare initially.

## **7\. Conclusion**

The integration of OpenTelemetry into Blazor WebAssembly applications within the.NET Aspire ecosystem presents unique challenges compared to backend services. Direct instrumentation using the standard OpenTelemetry.NET SDK is currently infeasible due to fundamental limitations imposed by the browser's WebAssembly sandbox environment.5 Furthermore, Aspire's automated observability configuration mechanisms, primarily the ServiceDefaults project, are incompatible with Blazor WASM.1 The.NET Aspire dashboard, while excellent for visualizing backend telemetry during local development, has explicit limitations regarding browser-originated telemetry, currently lacking support for logs and metrics sent via the OTel JS SDK.2

Despite these hurdles, achieving valuable observability for Blazor WASM is possible through a well-defined workaround: utilizing the **OpenTelemetry JavaScript SDK invoked via Blazor's JavaScript Interop** capabilities.3 This approach leverages standard web technologies and allows for the capture of critical client-side activities, most importantly network requests made to backend services.

While this method does not provide the complete observability picture (logs, metrics, traces) within the Aspire dashboard itself, it successfully enables **trace visualization**. By configuring the OTel JS SDK with an OTLP/HTTP exporter pointing to the Aspire dashboard's endpoint, developers can view traces originating from the Blazor WASM frontend and correlate them with downstream traces from backend services.3 This trace data, combined with automatic and manual error recording on spans, significantly aids in diagnosing issues and understanding request flows across the distributed application during local development, directly addressing the core need for improved error diagnosis visibility expressed by developers facing this challenge.

Implementing this solution requires additional setup compared to backend services, involving Node.js package management, JavaScript coding, careful JS Interop implementation, and a deliberate strategy for passing configuration (like the OTLP endpoint URL) from the Aspire environment to the client-side code. However, the resulting trace visibility offers substantial benefits for debugging complex interactions in modern web applications built with Blazor WASM and.NET Aspire. Developers should adopt this practical workaround while remaining watchful for future advancements in the.NET, Aspire, and OpenTelemetry ecosystems that may offer more integrated and seamless solutions for Blazor WASM observability down the line.

#### **Works cited**

1. Blazor WASM not working with Latest Aspire Preview 3 \#1284 \- GitHub, accessed April 9, 2025, [https://github.com/dotnet/aspire/issues/1284](https://github.com/dotnet/aspire/issues/1284)  
2. Blazor WASM Project: Tracing & Metrics · Issue \#4387 · dotnet/aspire \- GitHub, accessed April 9, 2025, [https://github.com/dotnet/aspire/issues/4387](https://github.com/dotnet/aspire/issues/4387)  
3. .NET Aspirations \- Embracing OpenTelemetry · Alexandre Nédélec, accessed April 9, 2025, [https://techwatching.dev/posts/aspire-open-telemetry/](https://techwatching.dev/posts/aspire-open-telemetry/)  
4. NET Aspire service defaults \- Learn Microsoft, accessed April 9, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults)  
5. Tracing not working in Blazor WebAssembly application · Issue ..., accessed April 9, 2025, [https://github.com/open-telemetry/opentelemetry-dotnet/issues/2816](https://github.com/open-telemetry/opentelemetry-dotnet/issues/2816)  
6. ASP.NET Core Blazor | Microsoft Learn, accessed April 9, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-9.0)  
7. A Beginner's Guide To Blazor Server and WebAssembly Applications \- EzzyLearning.net, accessed April 9, 2025, [https://www.ezzylearning.net/tutorial/a-beginners-guide-to-blazor-server-and-webassembly-applications](https://www.ezzylearning.net/tutorial/a-beginners-guide-to-blazor-server-and-webassembly-applications)  
8. How to observe your Blazor WebAssembly application with OpenTelemetry and real user monitoring | New Relic, accessed April 9, 2025, [https://newrelic.com/blog/how-to-relic/how-to-observe-your-blazor-webassembly-application-with-opentelemetry-and-real-user-monitoring](https://newrelic.com/blog/how-to-relic/how-to-observe-your-blazor-webassembly-application-with-opentelemetry-and-real-user-monitoring)  
9. OpenTelemetry. NET in Blazor WebAssembly \- Wolfgang Ziegler, accessed April 9, 2025, [https://wolfgang-ziegler.com/blog/otel-dotnet-blazor](https://wolfgang-ziegler.com/blog/otel-dotnet-blazor)  
10. .NET Aspirations \- Embracing OpenTelemetry · Alexandre Nédélec, accessed April 9, 2025, [https://techwatching.dev/posts/aspire-open-telemetry](https://techwatching.dev/posts/aspire-open-telemetry)  
11. Browser | OpenTelemetry, accessed April 9, 2025, [https://opentelemetry.io/docs/languages/js/getting-started/browser/](https://opentelemetry.io/docs/languages/js/getting-started/browser/)  
12. NET Aspire telemetry \- Learn Microsoft, accessed April 9, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)  
13. Quickstart: Build your first .NET Aspire solution \- Learn Microsoft, accessed April 9, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-your-first-aspire-app](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-your-first-aspire-app)  
14. Practical OpenTelemetry in .NET 8 \- Martin Thwaites \- NDC London 2024 \- YouTube, accessed April 9, 2025, [https://www.youtube.com/watch?v=WzZI\_IT6gYo](https://www.youtube.com/watch?v=WzZI_IT6gYo)  
15. Observability with Aspire by Carl Sargunar | Issue 113 of Skrift Magazine, accessed April 9, 2025, [https://skrift.io/issues/observability-with-aspire/](https://skrift.io/issues/observability-with-aspire/)  
16. Using OpenTelemetry for flexible observability \- ISE Developer Blog, accessed April 9, 2025, [https://devblogs.microsoft.com/ise/using-opentelemetry-for-flexible-observability/](https://devblogs.microsoft.com/ise/using-opentelemetry-for-flexible-observability/)  
17. .NET | OpenTelemetry, accessed April 9, 2025, [https://opentelemetry.io/docs/languages/dotnet/](https://opentelemetry.io/docs/languages/dotnet/)  
18. OpenTelemetry help and support for Azure Monitor Application Insights \- Learn Microsoft, accessed April 9, 2025, [https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-help-support-feedback](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-help-support-feedback)  
19. WebAssembly and Containers: Orchestrating Distributed Architectures with .NET Aspire, accessed April 9, 2025, [https://www.infoq.com/articles/webassembly-containers-dotnet-aspire/](https://www.infoq.com/articles/webassembly-containers-dotnet-aspire/)  
20. Example: Use OpenTelemetry with OTLP and the standalone Aspire ..., accessed April 9, 2025, [https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example)  
21. Getting Started | OpenTelemetry, accessed April 9, 2025, [https://opentelemetry.io/docs/languages/net/getting-started/](https://opentelemetry.io/docs/languages/net/getting-started/)  
22. Why Everyone Will be Using .NET Aspire /w David Fowler : r/dotnet \- Reddit, accessed April 9, 2025, [https://www.reddit.com/r/dotnet/comments/1isllmz/why\_everyone\_will\_be\_using\_net\_aspire\_w\_david/](https://www.reddit.com/r/dotnet/comments/1isllmz/why_everyone_will_be_using_net_aspire_w_david/)  
23. Why is microsoft taunting aspire as next best thing : r/dotnet \- Reddit, accessed April 9, 2025, [https://www.reddit.com/r/dotnet/comments/1b13b3o/why\_is\_microsoft\_taunting\_aspire\_as\_next\_best/](https://www.reddit.com/r/dotnet/comments/1b13b3o/why_is_microsoft_taunting_aspire_as_next_best/)  
24. Improving your application telemetry using .NET 8 and Open Telemetry | .NET Conf 2023, accessed April 9, 2025, [https://www.youtube.com/watch?v=BnjHArsYGLM](https://www.youtube.com/watch?v=BnjHArsYGLM)  
25. jasonhand/DotNetConf24: Companion repo for the "Getting Started with OpenTelemetry in .NET" session at .NET Conf 2024 (11/17/2024) \- GitHub, accessed April 9, 2025, [https://github.com/jasonhand/DotNetConf24](https://github.com/jasonhand/DotNetConf24)  
26. Getting Started with OpenTelemetry in .NET \- YouTube, accessed April 9, 2025, [https://www.youtube.com/watch?v=4xMIROBECRw](https://www.youtube.com/watch?v=4xMIROBECRw)  
27. How To Observe Your Blazor WASM With OpenTelemetry And Real User Monitoring? ? by Harry Kimpel \- YouTube, accessed April 9, 2025, [https://www.youtube.com/watch?v=qaq-Hc1o0eI](https://www.youtube.com/watch?v=qaq-Hc1o0eI)  
28. OpenTelemetry Support | Sentry for Blazor WebAssembly, accessed April 9, 2025, [https://docs.sentry.io/platforms/dotnet/guides/blazor-webassembly/tracing/instrumentation/opentelemetry/](https://docs.sentry.io/platforms/dotnet/guides/blazor-webassembly/tracing/instrumentation/opentelemetry/)  
29. Releases · dotnet/aspire \- GitHub, accessed April 9, 2025, [https://github.com/dotnet/aspire/releases](https://github.com/dotnet/aspire/releases)  
30. Devops | NDC Conferences, accessed April 9, 2025, [https://ndcconferences.com/courses/devops](https://ndcconferences.com/courses/devops)  
31. Net | NDC Conferences, accessed April 9, 2025, [https://ndcconferences.com/courses/net](https://ndcconferences.com/courses/net)