
# Introducing Microsoft.Extensions.AI Preview – Unified AI Building Blocks for .NET

**Platform Development | Data Development**
*October 8th, 2024*
*By Luis Quintanilla, Program Manager*

**(Note:** Some UI elements like navigation, sign-in, banners, and feedback buttons from the original page capture are omitted.)*

---

## Table of contents

*   What is Microsoft.Extensions.AI?
*   Benefits of Microsoft.Extensions.AI
    *   Core benefits
    *   Common abstractions for AI Services
    *   Standard middleware implementations
*   How to get started
    *   Chat
    *   Embeddings
*   Start building with Microsoft.Extensions.AI
*   What's next for Microsoft.Extensions.AI?

---

We are excited to introduce the Microsoft.Extensions.AI.Abstractions and Microsoft.Extensions.AI libraries, available in preview today. These packages provide the .NET ecosystem with essential abstractions for integrating AI services into .NET applications and libraries, along with middleware for adding key capabilities.

To support the .NET ecosystem, the .NET team has enhanced the core Microsoft.Extensions libraries with these abstractions, or "exchange types," for .NET Generative AI applications and libraries.

AI capabilities are rapidly evolving, with common patterns emerging for functionality like "chat," embeddings, and tool calling. Unified abstractions are crucial for developers to work effectively across different sources. Middleware can add valuable functionality without burdening producers, benefiting consumers immediately.

For example, the `IChatClient` interface allows consumption of language models, whether hosted remotely or running locally. Any .NET package providing an AI client can implement this interface, enabling seamless integration with consuming .NET code.

```csharp
IChatClient client =
    environment.IsDevelopment ?
    new OllamaChatClient(...):
    new AzureAIInferenceChatClient(...);
```

Then, regardless of the provider you're using, you can send requests as follows:

```csharp
var response = await chatClient.CompleteAsync(
    "Translate the following text into Pig Latin: I love .NET and AI");

Console.WriteLine(response.Message); // (Appended from Page 2 start)
```

---

## What is Microsoft.Extensions.AI?

Microsoft.Extensions.AI is a set of core .NET libraries developed in collaboration with developers across the .NET ecosystem, including Semantic Kernel. These libraries provide a unified layer of C# abstractions for interacting with AI services, such as small and large language models (SLMs and LLMs), embeddings, and middleware.

*[Diagram showing the architecture: Application (Your .NET App leveraging AI) -> Microsoft.Extensions.AI (Support for DI, Pipelines, Middleware) -> Microsoft.Extensions.AI.Abstractions (Core Types: IChatClient, ChatMessage, Embeddings, Content Types) -> LLM Clients and AI Services (Semantic Kernel, OpenAI, Azure AI Inference, Ollama, LLM Community packages, GitHub Models - Provides an IChatClient that connects to LLM providers)]*

Currently, our focus is on creating abstractions that can be implemented by various services, all adhering to the same core concepts. We do not intend to release APIs tailored to any specific provider's services. Our goal is to act as a unifying layer within the .NET ecosystem, enabling developers to choose their preferred frameworks and libraries while ensuring seamless integration and collaboration across the ecosystem.

---

## Benefits of Microsoft.Extensions.AI

Microsoft.Extensions.AI offers a unified API abstraction for AI services, similar to our successful logging and dependency injection (DI) abstractions. Our goal is to provide standard implementations for caching, telemetry, tool calling, and other common tasks that work with any provider.

### Core benefits

*   **Unified API:** Delivers a consistent set of APIs and conventions for integrating AI services into .NET applications.
*   **Flexibility:** Allows .NET library authors to use AI services without being tied to a specific provider, making it adaptable to any provider.
*   **Ease of Use:** Enables .NET developers to experiment with different packages using the same underlying abstractions, maintaining a single API throughout their application.
*   **Componentization:** Simplifies adding new capabilities and facilitates the componentization and testing of applications.

### Common abstractions for AI Services

These abstractions make it easy to use idiomatic C# code for various scenarios with minimal code changes, whether you're using different services for development or production, addressing hybrid scenarios, or exploring other service providers.

Library authors who implement these abstractions will make their clients interoperable with the broader Microsoft.Extensions.AI ecosystem. Service-specific APIs remain accessible if needed, allowing consumers to code against the standard abstractions and pass through to proprietary APIs only when required.

```csharp
public interface IChatClient : IDisposable
{
    Task<ChatCompletion> CompleteAsync(...);
    IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(...);
    ChatClientMetadata Metadata { get; }
    TService? GetService<TService>(object? key = null) where TService : class;
}
```

As of this preview, we provide reference implementations for the following services:

*   OpenAI
*   Azure AI Inference
*   Ollama

However, we intend to work with package authors across the .NET ecosystem so that implementations of these Microsoft.Extensions.AI abstractions end up being part of the respective client libraries rather than requiring installation of additional packages. If you have a .NET client library for a particular AI service, we would love to see implementations of these abstractions in your library.

### Standard middleware implementations

Connecting to and using AI services is just one aspect of building robust applications. Production-ready applications require additional features like telemetry, logging, and tool calling capabilities. The Microsoft.Extensions.AI abstractions enable developers to easily integrate these components into their applications using familiar patterns.

The following sample demonstrates how to register an OpenAI `IChatClient`. `IChatClient` allows you to attach the capabilities in a consistent way across various providers.

```csharp
app.Services.AddChatClient(builder => builder
    .UseLogging()
    .UseFunctionInvocation()
    .UseDistributedCache()
    .UseOpenTelemetry()
    .Use(new OpenAIClient(...)).AsChatClient(...));
```

The capabilities demonstrated above are included in the Microsoft.Extensions.AI library, but they are a small subset of the kinds of capabilities that can be layered in with this approach. We're excited to see the creativity of .NET developers shine with all types of middleware exposed for creating powerful, robust AI-related functionality.

---

## How to get started

Microsoft.Extensions.AI is available in preview starting today.

To get started, you can create a console application and install the Microsoft.Extensions.AI package for the respective AI service you're working with.

### Chat

The following examples show how to use Microsoft.Extensions.AI for chat scenarios.

#### Azure AI Inference (GitHub Models)

1.  Install the `Microsoft.Extensions.AI.AzureAIInference` NuGet package which works with models from [GitHub Models](https://github.com/models) as well as [Azure AI Model Catalog](https://azure.microsoft.com/en-us/products/ai-studio/model-catalog/).
2.  Add the following code to your application. Replace `GH_TOKEN` with your GitHub Personal Access Token.

```csharp
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;

IChatClient client =
    new ChatCompletionsClient(
        endpoint: new Uri("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")))
    .AsChatClient("Phi-3.5-MoE-instruct"); // Model ID example

var response = await client.CompleteAsync("What is AI?");
Console.WriteLine(response.Message);
```

#### OpenAI

1.  Install the `Microsoft.Extensions.AI.OpenAI` NuGet package.
2.  Add the following code to your application. Replace `OPENAI_API_KEY` with your OpenAI Key.

```csharp
using OpenAI;
using Microsoft.Extensions.AI;

IChatClient client =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsChatClient(modelId: "gpt-4o-mini"); // Model ID example

var response = await client.CompleteAsync("What is AI?");
Console.WriteLine(response.Message);
```

#### Azure OpenAI

1.  Install the `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, and `Azure.Identity` NuGet packages.
2.  Add the following code to your application. Replace `AZURE_OPENAI_ENDPOINT` with your Azure OpenAI endpoint and `modelId` with your deployment name.

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

IChatClient client =
    new AzureOpenAIClient(
        new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")),
        new DefaultAzureCredential())
    .AsChatClient(modelId: "gpt-4o-mini"); // Deployment name example

var response = await client.CompleteAsync("What is AI?");
Console.WriteLine(response.Message);
```

#### Ollama

1.  Install the `Microsoft.Extensions.AI.Ollama` NuGet package.
2.  Add the following code to your application.

```csharp
using Microsoft.Extensions.AI;

IChatClient client =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1"); // Model name example

var response = await client.CompleteAsync("What is AI?");
Console.WriteLine(response.Message);

```

### Embeddings

Similar to chat, you can also use Microsoft.Extensions.AI for text embedding generation scenarios.

#### OpenAI

1.  Install the `Microsoft.Extensions.AI.OpenAI` NuGet package.
2.  Add the following code to your application. Replace `OPENAI_API_KEY` with your OpenAI Key.

```csharp
using OpenAI;
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsEmbeddingGenerator(modelId: "text-embedding-3-small"); // Model ID example

var embedding = await generator.GenerateAsync("What is AI?");
Console.WriteLine(string.Join(", ", embedding[0].Vector.ToArray()));
```

#### Azure OpenAI

1.  Install the `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, and `Azure.Identity` NuGet packages.
2.  Add the following code to your application. Replace `AZURE_OPENAI_ENDPOINT` with your Azure OpenAI endpoint and `modelId` with your deployment name.

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new AzureOpenAIClient(
        new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")),
        new DefaultAzureCredential())
    .AsEmbeddingGenerator(modelId: "text-embedding-3-small"); // Deployment name example

var embeddings = await generator.GenerateAsync("What is AI?");
Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
```

#### Ollama

1.  Install the `Microsoft.Extensions.AI.Ollama` NuGet package.
2.  Add the following code to your application.

```csharp
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm"); // Model name example

var embedding = await generator.GenerateAsync("What is AI?");
Console.WriteLine(string.Join(", ", embedding[0].Vector.ToArray()));
```

---

## Start building with Microsoft.Extensions.AI

With the release of Microsoft.Extensions.AI, we're excited to build the foundation of an ecosystem for AI application development. Here are some ways you can get involved and start building with Microsoft.Extensions.AI:

*   **Library Developers:** If you own libraries that provide clients for AI services, consider implementing the interfaces in your libraries. This allows users to easily integrate your NuGet package via the abstractions.
*   **Service Consumers:** If you're developing libraries that consume AI services, use the abstractions instead of hardcoding to a specific AI service. This approach gives your consumers the flexibility to choose their preferred service.
*   **Application Developers:** Try out the abstractions to simplify integration into your apps. This enables portability across models and services, facilitates testing and mocking, leverages middleware provided by the ecosystem, and maintains a consistent API throughout your app, even if you use different services in different parts of your application (i.e., local and hosted model hybrid scenarios).
*   **Ecosystem Contributors:** If you're interested in contributing to the ecosystem, consider writing custom middleware components.

We have a set of samples in the [dotnet/ai-samples](https://github.com/dotnet/ai-samples) GitHub repository to help you get started.

For an end-to-end sample using Microsoft.Extensions.AI, see [eShopSupport](https://github.com/dotnet/eShopSupport).

---

## What's next for Microsoft.Extensions.AI?

As mentioned, we're currently releasing Microsoft.Extensions.AI in preview. We expect the library to remain in preview through the .NET 9 release in November as we continue to gather feedback.

In the near term, we plan to:

*   Continue collaborating with Semantic Kernel on integrating Microsoft.Extensions.AI as its foundational layer. For more details, check out the following [post on the Semantic Kernel blog](https://devblogs.microsoft.com/semantic-kernel/semantic-kernel-dotnet-extensions-ai/). (Note: URL inferred based on context, actual URL might differ).
*   Update existing samples like eShop to use Microsoft.Extensions.AI.
*   Work with everyone across the .NET ecosystem on the adoption of Microsoft.Extensions.AI. The more providers implement the abstractions, the more consumers use it, and the more middleware components are built, the more powerful all of the pieces become.

We look forward to shaping the future of AI development in .NET with your help.

Please try out Microsoft.Extensions.AI and [share your feedback](https://github.com/dotnet/extensions/issues) (Note: Feedback link inferred) so we can build the experiences that help you and your team thrive.

---

**Category:** .NET, AI
**Topics:** .NET, AI, MEAI (?)

---

**Author**

**Luis Quintanilla**
*Program Manager*
Luis Quintanilla is a program manager based out of the New York City area working on machine learning for .NET. He's passionate about helping others be successful developing machine learning applications.

---

## Comments (16 comments)

**(Note:** Comments are summarized/formatted below. Timestamps are simplified.)*

**Brandon Studio** (Nov 21, 2024)
Do we have documentation or API reference?

**Vaibhav Ghorpade** (Nov 13, 2024)
Could you please clarify when to use Microsoft.Extensions.AI and Semantic Kernel?

> **Aziz, Muatassim** (Nov 14, 2024)
> Apparently the answer is not clear, other than they are using the same underlying layer of objects. However Semantic Kernel is currently supporting many more models. Until I watch on .net Conf I did not knew that Semantic Kernel is opinionated. 🙂

**Devis Lucato** (Oct 23, 2024)
As someone deeply involved with Semantic Kernel as an SDK for AI apps and Kernel Memory for RAG and memory design patterns using many LLMs, it's thrilling to see features like these making their way into the broader .NET community.
This unifying layer simplifies how developers integrate AI capabilities, providing a consistent and powerful experience for AI services in .NET. Kudos to the team for making this happen!
*[Read more]*

**Amir H** (Oct 19, 2024, Edited)
This is indeed a wonderful and welcoming news!
However, I would love to see more investment into things like MLX bindings for .net, similar to what .net for iOS (Xamarin.iOS/Xamarin.macOS) has been doing for UIKit to leverage the portability and cross-platform strength of C#/dotnet ecosystem while benefiting from the native hardware performance (Apple SoC).
Thanks!

**Christian** (Oct 13, 2024, Edited)
I guess that this decision is set in stone but it would be nice to hear what the reasoning around naming this namespace "Microsoft.Extensions.AI" was. I guess there was conflict between the Microsoft.Extensions.ML namespace? Since the intent seems to centered around (large) language models I wonder if something like "Microsoft.Extensions.(L)LM" was considered?

**Michel Bruchet** (Oct 12, 2024)
I love any initiative that advances .NET, but I'm wondering, as someone who already uses AutoML, what specific benefits this package could bring to my architectures?

**Houston Haynes** (Oct 10, 2024)
I can't wait to see F# versions of these... literally. I can't wait.
https://gist.github.com/houstonhaynes/da7345092da56d9e18ee90ad60efed9c

**Nelson Vincent Lopez** (Oct 9, 2024)
Cool 😎

**Christian Bezencon** (Oct 9, 2024)
Looking at examples it is not clear if this works with Azure OpenAI library for .NET (https://devblogs.microsoft.com/azure-sdk/announcing-the-stable-release-of-the-azure-openai-library-for-net/)? can you confirm ?

> **Luis Quintanilla** (Author, Oct 9, 2024)
> It does.
> Instead of `OpenAIClient` you can use `AzureOpenAIClient`.
> We'll update the post with a sample to make that clear. Thanks.

**Balasubramanian Ramanathan** (Oct 9, 2024)
It would be nice if we can have samples on how to to RAG apps with this. Like how to store the embedding to vector db like chroma and search it and get chat completion based on that

> **Devis Lucato** (Oct 23, 2024)
> Kernel Memory is closely related to Semantic Kernel, providing plenty of configuration and runtime options: https://github.com/microsoft/kernel-memory.

> **Luis Quintanilla** (Author, Oct 9, 2024)
> Check out the eShopSupport sample which shows how to use Microsoft.Extensions.AI with RAG patterns.
> https://github.com/dotnet/eShopSupport/

> **Fabian Williams** (Oct 14, 2024)
> I also did something this weekend to tease this out and play with it.. see here where I do RAG with a locally running Qdrant vector store and local Llama 3.1:70b https://github.com/fabianwilliams/LuxMentis/tree/main/dotnet/SKQdrantFringeSearchChat/SKQdrantConsume I did an accompanying video here as well. https://youtu.be/YbJ_DEcfhMU

*[Load more comments]*

---

**Source URL:** https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/