---
title: "Part 1: Set up project and development environment to build a custom knowledge retrieval (RAG) app"
titleSuffix: Azure AI Foundry
description:  Build a custom chat app using the Azure AI Foundry SDK. Part 1 of a 3-part tutorial series, which shows how to create the resources you need for parts 2 and 3.
manager: scottpolly
ms.service: azure-ai-foundry
ms.custom:
  - ignite-2024
ms.topic: tutorial
ms.date: 02/12/2025
ms.reviewer: lebaro
ms.author: sgilley
author: sdgilley
#customer intent: As a developer, I want to create a project and set up my development environment to build a custom knowledge retrieval (RAG) app with the Azure AI Foundry SDK.
---

# Tutorial:  Part 1 - Set up project and development environment to build a custom knowledge retrieval (RAG) app with the Azure AI Foundry SDK

In this tutorial, you use the [Azure AI Foundry](https://ai.azure.com) SDK (and other libraries) to build, configure, and evaluate a chat app for your retail company called Contoso Trek. Your retail company specializes in outdoor camping gear and clothing. The chat app should answer questions about your products and services. For example, the chat app can answer questions such as "which tent is the most waterproof?" or "what is the best sleeping bag for cold weather?".

This tutorial is part one of a three-part tutorial.  This part one gets you ready to write code in part two and evaluate your chat app in part three. In this part, you:

> [!div class="checklist"]
> - Create a project
> - Create an Azure AI Search index
> - Install the Azure CLI and sign in
> - Install Python and packages
> - Deploy models into your project
> - Configure your environment variables

If you've completed other tutorials or quickstarts, you might have already created some of the resources needed for this tutorial. If you have, feel free to skip those steps here.

This tutorial is part one of a three-part tutorial.

[!INCLUDE [feature-preview](../includes/feature-preview.md)]

## Prerequisites

* An Azure account with an active subscription. If you don't have one, [create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).

## Create a project

To create a project in [Azure AI Foundry](https://ai.azure.com), follow these steps:

1. Go to the **Home** page of [Azure AI Foundry](https://ai.azure.com).
1. Select **+ Create project**.
1. Enter a name for the project.  Keep all the other settings as default.
1. Projects are created in hubs.  If you see **Create a new hub** select it and specify a name.  Then select **Next**. (If you don't see **Create new hub**, don't worry; it's because a new one is being created for you.) 
1. Select **Customize** to specify properties of the hub.
1. Use any values you want, except for **Region**.  We recommend you use either **East US2** or **Sweden Central** for the region for this tutorial series.
1. Select **Next**.
1. Select **Create project**.

## Deploy models

You need two models to build a RAG-based chat app: an Azure OpenAI chat model (`gpt-4o-mini`) and an Azure OpenAI embedding model (`text-embedding-ada-002`). Deploy these models in your Azure AI Foundry project, using this set of steps for each model.

These steps deploy a model to a real-time endpoint from the Azure AI Foundry portal [model catalog](../how-to/model-catalog-overview.md):

1. On the left navigation pane, select **Model catalog**.
1. Select the **gpt-4o-mini** model from the list of models. You can use the search bar to find it. 

    :::image type="content" source="../media/tutorials/chat/select-model.png" alt-text="Screenshot of the model selection page." lightbox="../media/tutorials/chat/select-model.png":::

1. On the model details page, select **Deploy**.

    :::image type="content" source="../media/tutorials/chat/deploy-model.png" alt-text="Screenshot of the model details page with a button to deploy the model." lightbox="../media/tutorials/chat/deploy-model.png":::

1. Leave the default **Deployment name**. select **Deploy**.  Or, if the model isn't available in your region, a different region is selected for you and connected to your project.  In this case, select **Connect and deploy**.

After you deploy the **gpt-4o-mini**, repeat the steps to deploy the **text-embedding-ada-002** model.

## <a name="create-search"></a> Create an Azure AI Search service

The goal with this application is to ground the model responses in your custom data. The search index is used to retrieve relevant documents based on the user's question.

You need an Azure AI Search service and connection in order to create a search index.

> [!NOTE]
> Creating an [Azure AI Search service](/azure/search/) and subsequent search indexes has associated costs. You can see details about pricing and pricing tiers for the Azure AI Search service on the creation page, to confirm cost before creating the resource. For this tutorial, we recommend using a pricing tier of **Basic** or above.

If you already have an Azure AI Search service, you can skip to the [next section](#connect).

Otherwise, you can create an Azure AI Search service using the Azure portal. 

> [!TIP]
> This step is the only time you use the Azure portal in this tutorial series.  The rest of your work is done in Azure AI Foundry portal or in your local development environment.

1. [Create an Azure AI Search service](https://portal.azure.com/#create/Microsoft.Search) in the Azure portal.
1. Select your resource group and instance details. You can see details about pricing and pricing tiers on this page.
1. Continue through the wizard and select **Review + assign** to create the resource.
1. Confirm the details of your Azure AI Search service, including estimated cost.
1. Select **Create** to create the Azure AI Search service.

### <a name="connect"></a>Connect the Azure AI Search to your project

If you already have an Azure AI Search connection in your project, you can skip to [Install the Azure CLI and sign in](#installs).

In the Azure AI Foundry portal, check for an Azure AI Search connected resource.

1. In [Azure AI Foundry](https://ai.azure.com), go to your project and select **Management center** from the left pane.
1. In the **Connected resources** section, look to see if you have a connection of type **Azure AI Search**.
1. If you have an Azure AI Search connection, you can skip ahead to the next section.
1. Otherwise, select **New connection** and then **Azure AI Search**.
1. Find your Azure AI Search service in the options and select **Add connection**.
1. Use **API key** for **Authentication**.

    > [!IMPORTANT]
    > The **API key** option isn't recommended for production. To select and use the recommended **Microsoft Entra ID** authentication option, you must also configure access control for the Azure AI Search service. Assign the *Search Index Data Contributor* and *Search Service Contributor* roles to your user account. For more information, see [Connect to Azure AI Search using roles](../../search/search-security-rbac.md) and [Role-based access control in Azure AI Foundry portal](../concepts/rbac-ai-foundry.md).

1. Select **Add connection**.  


## Create a new Python environment

In the IDE of your choice, create a new folder for your project.  Open a terminal window in that folder.

[!INCLUDE [Install Python](../includes/install-python.md)]

## Install packages

Install `azure-ai-projects`(preview) and `azure-ai-inference` (preview), along with other required packages.

1. First, create a file named **requirements.txt** in your project folder. Add the following packages to the file:

    :::code language="txt" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/requirements.txt":::

1. Install the required packages:

    ```bash
    pip install -r requirements.txt
    ```

### Create helper script

Create a folder for your work. Create a file called **config.py** in this folder. This helper script is used in the next two parts of the tutorial series. Add the following code:

:::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/config.py":::

> [!NOTE]
> This script also uses a package you haven't installed yet, `azure.monitor.opentelemetry`.  You'll install this package in the next part of the tutorial series.

## Configure environment variables

[!INCLUDE [create-env-file](../includes/create-env-file-tutorial.md)]

## <a name="installs"></a> Install the Azure CLI and sign in 

[!INCLUDE [Install the Azure CLI](../includes/install-cli.md)]

Keep this terminal window open to run your python scripts from here as well, now that you've signed in.

## Clean up resources

To avoid incurring unnecessary Azure costs, you should delete the resources you created in this tutorial if they're no longer needed. To manage resources, you can use the [Azure portal](https://portal.azure.com?azure-portal=true).

But don't delete them yet, if you want to build a chat app in [the next part of this tutorial series](copilot-sdk-build-rag.md).

## Next step

In this tutorial, you set up everything you need to build a custom chat app with the Azure AI SDK. In the next part of this tutorial series, you build the custom app.

> [!div class="nextstepaction"]
> [Part 2: Build a custom chat app with the Azure AI SDK](copilot-sdk-build-rag.md)

---
---
---
---

---
title: "Part 2: Build a custom knowledge retrieval (RAG) app with the Azure AI Foundry SDK"
titleSuffix: Azure AI Foundry
description:  Learn how to build a RAG-based chat app using the Azure AI Foundry SDK. This tutorial is part 2 of a 3-part tutorial series.
manager: scottpolly
ms.service: azure-ai-foundry
ms.topic: tutorial
ms.date: 02/12/2025
ms.reviewer: lebaro
ms.author: sgilley
author: sdgilley
ms.custom: copilot-learning-hub, ignite-2024
#customer intent: As a developer, I want to learn how to use the prompt flow SDK so that I can build a RAG-based chat app.
---

# Tutorial:  Part 2 - Build a custom knowledge retrieval (RAG) app with the Azure AI Foundry SDK

In this tutorial, you use the [Azure AI Foundry](https://ai.azure.com) SDK (and other libraries) to build, configure, and evaluate a chat app for your retail company called Contoso Trek. Your retail company specializes in outdoor camping gear and clothing. The chat app should answer questions about your products and services. For example, the chat app can answer questions such as "which tent is the most waterproof?" or "what is the best sleeping bag for cold weather?".

This part two shows you how to enhance a basic chat application by adding [retrieval augmented generation (RAG)](../concepts/retrieval-augmented-generation.md) to ground the responses in your custom data. Retrieval Augmented Generation (RAG) is a pattern that uses your data with a large language model (LLM) to generate answers specific to your data. In this part two, you learn how to:

> [!div class="checklist"]
> - Get example data
> - Create a search index of the data for the chat app to use
> - Develop custom RAG code

This tutorial is part two of a three-part tutorial.

## Prerequisites

* Complete [Tutorial:  Part 1 - Create resources for building a custom chat application with the Azure AI SDK](copilot-sdk-create-resources.md) to:

    * Create a project with a connected Azure AI Search index
    * Install the Azure CLI, Python, and required packages
    * Configure your environment variables

## Create example data for your chat app

The goal with this RAG-based application is to ground the model responses in your custom data. You use an Azure AI Search index that stores vectorized data from the embeddings model. The search index is used to retrieve relevant documents based on the user's question.

If you already have a search index with data, you can skip to [Get product documents](#get-documents). Otherwise, you can create a simple example data set to use in your chat app.  

Create an **assets** directory and add this example data to a **products.csv** file:

:::code language="csv" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/assets/products.csv":::

## Create a search index

The search index is used to store vectorized data from the embeddings model. The search index is used to retrieve relevant documents based on the user's question. 

1. Create the file **create_search_index.py** in your main folder (that is, the same directory where you placed your **assets** folder, not inside the **assets** folder).  
1. Copy and paste the following code into your **create_search_index.py** file.
1. Add the code to import the required libraries, create a project client, and configure some settings: 

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/create_search_index.py" id="imports_and_config":::

1. Now add the function to define a search index:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/create_search_index.py" id="create_search_index":::

1. Create the function to add a csv file to the index:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/create_search_index.py" id="add_csv_to_index":::

1. Finally, run the functions to build the index and register it to the cloud project:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/create_search_index.py" id="test_create_index":::

1. From your console, log in to your Azure account and follow instructions for authenticating your account:

    ```bash
    az login
    ```

1. Run the code to build your index locally and register it to the cloud project:

    ```bash
    python create_search_index.py
    ```

## <a name="get-documents"></a> Get product documents

Next, you create a script to get product documents from the search index. The script queries the search index for documents that match a user's question.

### Create script to get product documents

When the chat gets a request, it searches through your data to find relevant information.  This script uses the Azure AI SDK to query the search index for documents that match a user's question.  It then returns the documents to the chat app.

1. Create the **get_product_documents.py** file in your main directory. Copy and paste the following code into the file.

1. Start with code to import the required libraries, create a project client, and configure settings: 

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/get_product_documents.py" id="imports_and_config":::

1. Add the function to get product documents:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/get_product_documents.py" id="get_product_documents":::

1. Finally, add code to test the function when you run the script directly:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/get_product_documents.py" id="test_get_documents":::

### Create prompt template for intent mapping

The **get_product_documents.py** script uses a prompt template to convert the conversation to a search query. The template instructs how to extract the user's intent from the conversation.  

Before you run the script, create the prompt template. Add the file **intent_mapping.prompty** to your **assets** folder:

:::code language="prompty" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/assets/intent_mapping.prompty":::

### Test the product document retrieval script

Now that you have both the script and template, run the script to test out what documents the search index returns from a query.  In a terminal window run:

```bash
python get_product_documents.py --query "I need a new tent for 4 people, what would you recommend?"
```

## <a name="develop-code"></a> Develop custom knowledge retrieval (RAG) code

Next you create custom code to add retrieval augmented generation (RAG) capabilities to a basic chat application.

### Create a chat script with RAG capabilities

1. In your main folder, create a new file called **chat_with_products.py**. This script retrieves product documents and generates a response to a user's question.
1. Add the code to import the required libraries, create a project client, and configure settings: 

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/chat_with_products.py" id="imports_and_config":::

1. Create the chat function that uses the RAG capabilities:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/chat_with_products.py" id="chat_function":::

1. Finally, add the code to run the chat function:
    
    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/chat_with_products.py" id="test_function":::

### Create a grounded chat prompt template

The **chat_with_products.py** script calls a prompt template to generate a response to the user's question. The template instructs how to generate a response based on the user's question and the retrieved documents.  Create this template now.

In your **assets** folder, add the file **grounded_chat.prompty**:

:::code language="prompty" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/assets/grounded_chat.prompty":::

### Run the chat script with RAG capabilities

Now that you have both the script and the template, run the script to test your chat app with RAG capabilities:

```bash
python chat_with_products.py --query "I need a new tent for 4 people, what would you recommend?"
```

### <a name="logging"></a> Add telemetry logging

To enable logging of telemetry to your project:

1. Add an Application Insights resource to your project.  Navigate to the **Tracing** tab in the [Azure AI Foundry portal](https://ai.azure.com/), and create a new resource if you don't already have one.

    :::image type="content" source="../../ai-services/agents/media/ai-foundry-tracing.png" alt-text="A screenshot of the tracing screen in the Azure AI Foundry portal." lightbox="../../ai-services/agents/media/ai-foundry-tracing.png":::

1. Install `azure-monitor-opentelemetry`:

   ```bash
   pip install azure-monitor-opentelemetry
   ```
   
1. Add the `--enable-telemetry` flag when you use the `chat_with_products.py` script:

   ```bash
   python chat_with_products.py --query "I need a new tent for 4 people, what would you recommend?" --enable-telemetry 
   ```

Follow the link in the console output to see the telemetry data in your Application Insights resource. If it doesn't appear right away, wait a few minutes and select **Refresh** in the toolbar.

## Clean up resources

To avoid incurring unnecessary Azure costs, you should delete the resources you created in this tutorial if they're no longer needed. To manage resources, you can use the [Azure portal](https://portal.azure.com?azure-portal=true).

But don't delete them yet, if you want to deploy your chat app to Azure in [the next part of this tutorial series](copilot-sdk-evaluate.md).

## Next step

> [!div class="nextstepaction"]
> [Part 3: Evaluate your chat app to Azure](copilot-sdk-evaluate.md)

---
---
---
---

---
title: "Part 3: Evaluate a chat app with the Azure AI SDK"
titleSuffix: Azure AI Foundry
description: Evaluate and deploy a custom chat app with the prompt flow SDK. This tutorial is part 3 of a 3-part tutorial series.
manager: scottpolly
ms.service: azure-ai-foundry
ms.custom:
  - ignite-2024
ms.topic: tutorial
ms.date: 11/06/2024
ms.reviewer: lebaro
ms.author: sgilley
author: sdgilley
#customer intent: As a developer, I want to learn how to use the prompt flow SDK so that I can evaluate and deploy a chat app.
---

# Tutorial: Part 3 - Evaluate a custom chat application with the Azure AI Foundry SDK

In this tutorial, you use the [Azure AI Foundry](https://ai.azure.com) SDK (and other libraries) to  evaluate the chat app you built in [Part 2 of the tutorial series](copilot-sdk-build-rag.md). In this part three, you learn how to:

> [!div class="checklist"]
> - Create an evaluation dataset
> - Evaluate the chat app with Azure AI evaluators
> - Iterate and improve your app


This tutorial is part three of a three-part tutorial.

## Prerequisites

- Complete [part 2 of the tutorial series](copilot-sdk-build-rag.md) to build the chat application.
- Make sure you've completed the steps to [add telemetry logging](copilot-sdk-build-rag.md#logging) from part 2.


## <a name="evaluate"></a> Evaluate the quality of the chat app responses

Now that you know your chat app responds well to your queries, including with chat history, it's time to evaluate how it does across a few different metrics and more data.

You use an evaluator with an evaluation dataset and the `get_chat_response()` target function, then assess the evaluation results.

Once you run an evaluation, you can then make improvements to your logic, like improving your system prompt, and observing how the chat app responses change and improve.

### Create evaluation dataset

Use the following evaluation dataset, which contains example questions and expected answers (truth).

1. Create a file called **chat_eval_data.jsonl** in your **assets** folder.
1. Paste this dataset into the file:

    :::code language="jsonl" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/assets/chat_eval_data.jsonl":::

## Evaluate with Azure AI evaluators

Now define an evaluation script that will:

- Generate a target function wrapper around our chat app logic.
- Load the sample `.jsonl` dataset.
- Run the evaluation, which takes the target function, and merges the evaluation dataset with the responses from the chat app.
- Generate a set of GPT-assisted metrics (relevance, groundedness, and coherence) to evaluate the quality of the chat app responses.
- Output the results locally, and logs the results to the cloud project.

The script allows you to review the results locally, by outputting the results in the command line, and to a json file.

The script also logs the evaluation results to the cloud project so that you can compare evaluation runs in the UI.

1. Create a file called **evaluate.py** in your main folder.
1. Add the following code to import the required libraries, create a project client, and configure some settings: 

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/evaluate.py" id="imports_and_config":::

1. Add code to create a wrapper function that implements the evaluation interface for query and response evaluation:

    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/evaluate.py" id="evaluate_wrapper":::

1. Finally, add code to run the evaluation, view the results locally, and gives you a link to the evaluation results in Azure AI Foundry portal:
 
    :::code language="python" source="~/azureai-samples-main/scenarios/rag/custom-rag-app/evaluate.py" id="run_evaluation":::

### Configure the evaluation model

Since the evaluation script calls the model many times, you might want to increase the number of tokens per minute for the evaluation model.  

In Part 1 of this tutorial series, you created an **.env** file that specifies the name of the evaluation model, `gpt-4o-mini`.  Try to increase the tokens per minute limit for this model, if you have available quota. If you don't have enough quota to increase the value, don't worry.  The script is designed to handle limit errors.

1. In your project in Azure AI Foundry portal, select **Models + endpoints**.
1. Select **gpt-4o-mini**.  
1. Select **Edit**.
1. If you have quota to increase the **Tokens per Minute Rate Limit**, try increasing it to 30 or above. 
1. Select **Save and close**.

### Run the evaluation script

1. From your console, sign in to your Azure account with the Azure CLI:

    ```bash
    az login
    ```

1. Install the required package:

    ```bash
    pip install azure-ai-evaluation[remote]
    ```

1. Now run the evaluation script:

    ```bash
    python evaluate.py
    ```

Expect the evaluation to take a few minutes to complete.

### Interpret the evaluation output

In the console output, you see an answer for each question, followed by a table with summarized metrics. (You might see different columns in your output.)

If you weren't able to increase the tokens per minute limit for your model, you might see some time-out errors, which are expected. The evaluation script is designed to handle these errors and continue running.  

> [!NOTE]
> You may also see many `WARNING:opentelemetry.attributes:` - these can be safely ignored and do not affect the evaluation results.

```Text
====================================================
'-----Summarized Metrics-----'
{'groundedness.gpt_groundedness': 1.6666666666666667,
 'groundedness.groundedness': 1.6666666666666667}
'-----Tabular Result-----'
                                     outputs.response  ... line_number
0   Could you specify which tent you are referring...  ...           0
1   Could you please specify which camping table y...  ...           1
2   Sorry, I only can answer queries related to ou...  ...           2
3   Could you please clarify which aspects of care...  ...           3
4   Sorry, I only can answer queries related to ou...  ...           4
5   The TrailMaster X4 Tent comes with an included...  ...           5
6                                            (Failed)  ...           6
7   The TrailBlaze Hiking Pants are crafted from h...  ...           7
8   Sorry, I only can answer queries related to ou...  ...           8
9   Sorry, I only can answer queries related to ou...  ...           9
10  Sorry, I only can answer queries related to ou...  ...          10
11  The PowerBurner Camping Stove is designed with...  ...          11
12  Sorry, I only can answer queries related to ou...  ...          12

[13 rows x 8 columns]
('View evaluation results in Azure AI Foundry portal: '
 'https://xxxxxxxxxxxxxxxxxxxxxxx')
```


### View evaluation results in Azure AI Foundry portal

Once the evaluation run completes, follow the link to view the evaluation results on the **Evaluation** page in the Azure AI Foundry portal.

:::image type="content" source="../media/tutorials/develop-rag-copilot-sdk/eval-studio-overview.png" alt-text="Screenshot shows evaluation overview in Azure AI Foundry portal.":::

You can also look at the individual rows and see metric scores per row, and view the full context/documents that were retrieved. These metrics can be helpful in interpreting and debugging evaluation results.

:::image type="content" source="../media/tutorials/develop-rag-copilot-sdk/eval-studio-rows.png" alt-text="Screenshot shows rows of evaluation results in Azure AI Foundry portal.":::

For more information about evaluation results in Azure AI Foundry portal, see [How to view evaluation results in Azure AI Foundry portal](../how-to/evaluate-results.md).

## Iterate and improve

Notice that the responses are not well grounded. In many cases, the model replies with a question rather than an answer. This is a result of the prompt template instructions. 
 
* In your **assets/grounded_chat.prompty** file, find the sentence "If the question is not related to outdoor/camping gear and clothing, just say 'Sorry, I only can answer queries related to outdoor/camping gear and clothing. So, how can I help?'"
* Change the sentence to "If the question is related to outdoor/camping gear and clothing but vague, try to answer based on the reference documents, then ask for clarifying questions."  
* Save the file and re-run the evaluation script.

Try other modifications to the prompt template, or try different models, to see how the changes affect the evaluation results.

## Clean up resources

To avoid incurring unnecessary Azure costs, you should delete the resources you created in this tutorial if they're no longer needed. To manage resources, you can use the [Azure portal](https://portal.azure.com?azure-portal=true).

## Related content

- [Learn more about the Azure AI Foundry SDK](../how-to/develop/sdk-overview.md)

---
---
---
---

---
title: "Tutorial: Deploy an enterprise chat web app in the Azure AI Foundry portal playground"
titleSuffix: Azure AI Foundry
description: Use this article to deploy an enterprise chat web app in the Azure AI Foundry portal playground.
manager: scottpolly
ms.service: azure-ai-foundry
ms.custom:
  - ignite-2023
  - build-2024
  - ignite-2024
ms.topic: tutorial
ms.date: 02/13/2025
ms.reviewer: tgokal
ms.author: sgilley
author: sdgilley
# customer intent: As a developer, I want to deploy an enterprise chat web app in the Azure AI Foundry portal playground so that I can use my own data with a large language model.
---

# Tutorial: Deploy an enterprise chat web app

[!INCLUDE [feature-preview](../includes/feature-preview.md)]

In this article, you deploy an enterprise chat web app that uses your own data with a large language model in Azure AI Foundry portal.

Your data source is used to help ground the model with specific data. Grounding means that the model uses your data to help it understand the context of your question. You're not changing the deployed model itself. Your data is stored separately and securely in your original data source

The steps in this tutorial are:

> [!div class="checklist"]
> * Configure resources.
> * Add your data.
> * Test the model with your data.
> * Deploy your web app.

## Prerequisites

- An Azure subscription - <a href="https://azure.microsoft.com/free/cognitive-services" target="_blank">Create one for free</a>.
- A [deployed Azure OpenAI](../how-to/deploy-models-openai.md) chat model. Complete the [Azure AI Foundry playground quickstart](../quickstarts/get-started-playground.md) to create this resource if you haven't already.

- A Search service connection to index the sample product data.  If you don't have one, follow the steps to [create](copilot-sdk-create-resources.md#create-search) and [connect](copilot-sdk-create-resources.md#connect) a search service.

- A local copy of product data. The [Azure-Samples/rag-data-openai-python-promptflow repository on GitHub](https://github.com/Azure-Samples/rag-data-openai-python-promptflow/) contains sample retail product information that's relevant for this tutorial scenario. Specifically, the `product_info_11.md` file contains product information about the TrailWalker hiking shoes that's relevant for this tutorial example. [Download the example Contoso Trek retail product data in a ZIP file](https://github.com/Azure-Samples/rag-data-openai-python-promptflow/raw/refs/heads/main/tutorial/data/product-info.zip) to your local machine.

- A **Microsoft.Web** resource provider registered in the selected subscription, to be able to deploy to a web app. For more information on registering a resource provider, see [Register resource provider](/azure/azure-resource-manager/management/resource-providers-and-types#register-resource-provider-1).

- Necessary permissions to add role assignments in your Azure subscription. Granting permissions by role assignment is only allowed by the Owner of the specific Azure resources.

## Azure AI Foundry portal and Azure portal 

In this tutorial, you perform some tasks in the Azure AI Foundry portal and some tasks in the Azure portal.

The Azure AI Foundry portal is a web-based environment for building, training, and deploying AI models. As a developer, it's where you will build and deploy your chat web application.

The Azure portal allows an administrator to manage and monitor Azure resources. As an administrator, you'll use the portal to configure settings for various Azure services required for access from the web app.

## Configure resources

> [!IMPORTANT]
> You must have the necessary permissions to add role assignments in your Azure subscription. Granting permissions by role assignment is only allowed by the Owner of the specific Azure resources. You might need to ask your Azure subscription owner (who might be your IT admin) to complete this section for you.

In order for the resources to work correctly inside a web app, you need to configure them with the correct permissions. This work is done in the Azure portal.

To start, identify the resources you need to configure from the Azure AI Foundry portal.

1. Open the [Azure AI Foundry portal](https://ai.azure.com) and select the project you used to deploy the Azure OpenAI chat model.
1. Select **Management center** from the left pane.
1. Select **Connected resources** under your project.
1. Identify the three resources you need to configure:  the **Azure OpenAI**, the **Azure AI Search**, and the **Azure Blob storage** that corresponds to your **workspaceblobstore**.

    :::image type="content" source="../media/tutorials/deploy-chat-web-app/resources.png" alt-text="Screenshot shows the connected resources that need to be configured.":::

    > [!TIP]
    > If you have multiple **Azure OpenAI** resources, use the one that contains your deployed chat model.

1. For each resource, select the link to open the resource details.  From the details page, select the resource name to open the resource in the Azure portal.  (For the workspaceblobstore, select **View in Azure Portal**). 
1. After the browser tab opens, go back to the Azure AI Foundry portal and repeat the process for the next resource. 
1. When you're done, you should have three new browser tabs open, for **Search service**, **Azure AI services**, and **blobstore Container**. Keep all three new tabs open as you'll go back and forth between them to configure the resources.

### Enable managed identity

On the browser tab for the **Search service** resource in the Azure portal, enable the managed identity:

1. From the left pane, under **Settings**, select **Identity**.
1. Switch **Status** to **On**.
1. Select **Save**.

On the browser tab for the **Azure AI services** resource in the Azure portal, enable the managed identity:

1. From the left pane, under **Resource Management**, select **Identity**.
1. Switch **Status** to **On**.
1. Select **Save**.

### Set access control for search

On the browser tab for the **Search service** resource in the Azure portal, set the API Access policy:

1. From the left pane, under **Settings**, select **Keys**.
1. Under **API Access control**, select **Both**.
1. When prompted, select **Yes** to confirm the change.

### Assign roles

You'll repeat this pattern multiple times in the bulleted items below.

[!INCLUDE [Assign RBAC](../includes/assign-rbac.md)]

Use these steps to assign roles for the resources you're configuring in this tutorial:

* Assign the following roles on the browser tab for **Search service** in the Azure portal:
    * **Search Index Data Reader** to the **Azure AI services** managed identity
    * **Search Service Contributor** to the **Azure AI services** managed identity
    * **Contributor** to yourself (to find **Contributor**, switch to the **Privileged administrator roles** tab at the top.  All other roles are in the **Job function roles** tab.)

* Assign the following roles on the browser tab for **Azure AI services** in the Azure portal:

    * **Cognitive Services OpenAI Contributor** to the **Search service** managed identity
    * **Contributor** to yourself.

* Assign the following roles on the browser tab for **Azure Blob storage** in the Azure portal:

    * **Storage Blob Data Contributor** to the **Azure AI services** managed identity
    * **Storage Blob Data Reader** to the **Search service** managed identity
    * **Contributor** to yourself

You're done configuring resources. You can close the Azure portal browser tabs now if you wish.

## Add your data and try the chat model again

In the [Azure AI Foundry playground quickstart](../quickstarts/get-started-playground.md) (that's a prerequisite for this tutorial), observe how your model responds without your data. Now add your data to the model to help it answer questions about your products.

[!INCLUDE [Chat with your data](../includes/chat-with-data.md)] 

## Deploy your web app

Once you're satisfied with the experience in the Azure AI Foundry portal, you can deploy the model as a standalone web application. 

### Find your resource group in the Azure portal

In this tutorial, your web app is deployed to the same resource group as your [Azure AI Foundry hub](../how-to/create-secure-ai-hub.md). Later you configure authentication for the web app in the Azure portal.

Follow these steps to navigate to your resource group in the Azure portal:

1. Go to your project in [Azure AI Foundry](https://ai.azure.com). Then select **Management center** from the left pane.
1. Under the **Project** heading, select **Overview**.
1. Select the resource group name to open the resource group in the Azure portal. In this example, the resource group is named `rg-sdg-ai`.

    :::image type="content" source="../media/tutorials/chat/resource-group-manage-page.png" alt-text="Screenshot of the resource group in the Azure AI Foundry portal." lightbox="../media/tutorials/chat/resource-group-manage-page.png":::

1. You should now be in the Azure portal, viewing the contents of the resource group where you deployed the hub. Notice the resource group name and location, you'll use this information in the next section.
1. Keep this page open in a browser tab. You'll return to it later.

### Deploy the web app

Publishing creates an Azure App Service in your subscription. It might incur costs depending on the [pricing plan](https://azure.microsoft.com/pricing/details/app-service/windows/) you select. When you're done with your app, you can delete it from the Azure portal.

To deploy the web app:

> [!IMPORTANT]
> You need to [register **Microsoft.Web** as a resource provider](/azure/azure-resource-manager/management/resource-providers-and-types#register-resource-provider-1) before you can deploy to a web app. 

1. Complete the steps in the previous section to [add your data](#add-your-data-and-try-the-chat-model-again) to the playground.  (You can deploy a web app with or without your own data, but at least you need a deployed model as described in the [Azure AI Foundry playground quickstart](../quickstarts/get-started-playground.md)).

1. Select **Deploy > ...as a web app**.

    :::image type="content" source="../media/tutorials/chat/deploy-web-app.png" alt-text="Screenshot of the deploy new web app button." lightbox="../media/tutorials/chat/deploy-web-app.png":::

1. On the **Deploy to a web app** page, enter the following details:
    - **Name**: A unique name for your web app.
    - **Subscription**: Your Azure subscription. If you don't see any available subscriptions, first [register **Microsoft.Web** as a resource provider](/azure/azure-resource-manager/management/resource-providers-and-types#register-resource-provider-1).
    - **Resource group**: Select a resource group in which to deploy the web app. Use the same resource group as the hub.
    - **Location**: Select a location in which to deploy the web app. Use the same location as the hub.
    - **Pricing plan**: Choose a pricing plan for the web app.
    - **Enable chat history in the web app**: For the tutorial, the chat history box isn't selected. If you enable the feature, your users have access to their individual previous queries and responses. For more information, see [chat history remarks](#understand-chat-history).

1. Select **Deploy**.

1. Wait for the app to be deployed, which might take a few minutes. 

1. When it's ready, the **Launch** button is enabled on the toolbar. But don't launch the app yet and don't close the chat playground page - you'll return to it later.

### Configure web app authentication

By default, the web app is only accessible to you. In this tutorial, you add authentication to restrict access to the app to members of your Azure tenant. Users are asked to sign in with their Microsoft Entra account to be able to access your app. You can follow a similar process to add another identity provider if you prefer. The app doesn't use the user's sign in information in any other way other than verifying they're a member of your tenant.

1. Return to the browser tab containing the Azure portal (or reopen the [Azure portal](https://portal.azure.com?azure-portal=true) in a new browser tab) and view the contents of the resource group where you deployed the web app (you might need to refresh the view the see the web app).

1. Select the **App Service** resource from the list of resources in the resource group.

1. From the collapsible left menu under **Settings**, select **Authentication**. 

    :::image type="content" source="../media/tutorials/chat/azure-portal-app-service.png" alt-text="Screenshot of web app authentication menu item under settings in the Azure portal." lightbox="../media/tutorials/chat/azure-portal-app-service.png":::

1. If you see **Microsoft** listed an Identity provider on this page, nothing further is needed.  You can skip the next step.
1. Add an identity provider with the following settings:
    - **Identity provider**: Select Microsoft as the identity provider. The default settings on this page restrict the app to your tenant only, so you don't need to change anything else here.
    - **Tenant type**: Workforce
    - **App registration**: Create a new app registration
    - **Name**: *The name of your web app service*
    - **Supported account types**: Current tenant - Single tenant
    - **Restrict access**: Requires authentication
    - **Unauthenticated requests**: HTTP 302 Found redirect - recommended for websites

### Use the web app

You're almost there. Now you can test the web app.

1. If you changed settings, wait 10 minutes or so for the authentication settings to take effect.
1. Return to the browser tab containing the chat playground page in the Azure AI Foundry portal.
1. Select **Launch** to launch the deployed web app. If prompted, accept the permissions request.
1. If you don't see **Launch** in the playground, select **Web apps** from the left pane, then select your app from the list to launch it.

    *If the authentication settings haven't yet taken effect, close the browser tab for your web app and return to the chat playground in the Azure AI Foundry portal. Then wait a little longer and try again.*

1. In your web app, you can ask the same question as before ("How much are the TrailWalker hiking shoes"), and this time it uses information from your data to construct the response. You can expand the **reference** button to see the data that was used.

   :::image type="content" source="../media/tutorials/chat/chat-with-data-web-app.png" alt-text="Screenshot of the chat experience via the deployed web app." lightbox="../media/tutorials/chat/chat-with-data-web-app.png":::

## Understand chat history

With the chat history feature, your users have access to their individual previous queries and responses.

You can enable chat history when you [deploy the web app](#deploy-the-web-app). Select the **Enable chat history in the web app** checkbox.

:::image type="content" source="../media/tutorials/chat/deploy-web-app-chat-history.png" alt-text="Screenshot of the option to enable chat history when deploying a web app." lightbox="../media/tutorials/chat/deploy-web-app-chat-history.png":::

> [!IMPORTANT]
> Enabling chat history will create a [Cosmos DB instance](/azure/cosmos-db/introduction) in your resource group, and incur [additional charges](https://azure.microsoft.com/pricing/details/cosmos-db/autoscale-provisioned/) for the storage used.
> Deleting your web app does not delete your Cosmos DB instance automatically. To delete your Cosmos DB instance, along with all stored chats, you need to navigate to the associated resource in the Azure portal and delete it.

Once you enable chat history, your users are able to show and hide it in the top right corner of the app. When the history is shown, they can rename, or delete conversations. As they're logged into the app, conversations are automatically ordered from newest to oldest, and named based on the first query in the conversation.

If you delete the Cosmos DB resource but keep the chat history option enabled on the studio, your users are notified of a connection error, but can continue to use the web app without access to the chat history.

## Update the web app

Use the playground to add more data or test the model with different scenarios. When you're ready to update the web app with the new model, select **Deploy > ...as a web app** again. Select **Update an existing web app** and choose the existing web app from the list. The new model deploys to the existing web app.

## Clean up resources

To avoid incurring unnecessary Azure costs, you should delete the resources you created in this quickstart if they're no longer needed. To manage resources, you can use the [Azure portal](https://portal.azure.com?azure-portal=true).

## Related content

- [Get started building a chat app using the prompt flow SDK](../quickstarts/get-started-code.md)
- [Build a custom chat app with the Azure AI SDK.](./copilot-sdk-create-resources.md).

---
---
---
---

---
title: How to use Azure OpenAI Service in Azure AI Foundry portal
titleSuffix: Azure AI Foundry
description: Learn how to use Azure OpenAI Service in Azure AI Foundry portal.
manager: nitinme
ms.service: azure-ai-foundry
ms.custom:
  - ignite-2023
  - build-2024
  - ignite-2024
ms.topic: how-to
ms.date: 2/12/2025
ms.reviewer: eur
ms.author: eur
author: eric-urban
---

# How to use Azure OpenAI Service in Azure AI Foundry portal

You might have existing Azure OpenAI Service resources and model deployments that you created using the old Azure OpenAI Studio or via code. You can pick up where you left off by using your existing resources in Azure AI Foundry portal.

This article describes how to:
- Use Azure OpenAI Service models outside of a project.
- Use Azure OpenAI Service models and an Azure AI Foundry project.

> [!TIP]
> You can use Azure OpenAI Service in Azure AI Foundry portal without creating a project or a connection. When you're working with the models and deployments, we recommend that you work outside of a project. Eventually, you want to work in a project for tasks such as managing connections, permissions, and deploying the models to production.

## Use Azure OpenAI models outside of a project

You can use your existing Azure OpenAI model deployments in Azure AI Foundry portal outside of a project. Start here if you previously deployed models using the old Azure OpenAI Studio or via the Azure OpenAI Service SDKs and APIs.

To use Azure OpenAI Service outside of a project, follow these steps:
1. Go to the [Azure AI Foundry home page](https://ai.azure.com) and make sure you're signed in with the Azure subscription that has your Azure OpenAI Service resource.
1. Find the tile that says **Focused on Azure OpenAI Service?** and select **Let's go**. 

    :::image type="content" source="../../media/azure-openai-in-ai-studio/home-page.png" alt-text="Screenshot of the home page in Azure AI Foundry portal with the option to select Azure OpenAI Service." lightbox="../../media/azure-openai-in-ai-studio/home-page.png":::

    If you don't see this tile, you can also go directly to the [Azure OpenAI Service page](https://ai.azure.com/resource/overview) in Azure AI Foundry portal.

1. You should see your existing Azure OpenAI Service resources. In this example, the Azure OpenAI Service resource `contoso-azure-openai-eastus` is selected.

    :::image type="content" source="../../media/ai-services/azure-openai-studio-select-resource.png" alt-text="Screenshot of the Azure OpenAI Service resources page in Azure AI Foundry portal." lightbox="../../media/ai-services/azure-openai-studio-select-resource.png":::

    If your subscription has multiple Azure OpenAI Service resources, you can use the selector or go to **All resources** to see all your resources. 

If you create more Azure OpenAI Service resources later (such as via the Azure portal or APIs), you can also access them from this page.

## <a name="project"></a> Use Azure OpenAI Service in a project

You might eventually want to use a project for tasks such as managing connections, permissions, and deploying models to production. You can use your existing Azure OpenAI Service resources in an Azure AI Foundry project. 

Let's look at two ways to connect Azure OpenAI Service resources to a project:

- [When you create a project](#connect-azure-openai-service-when-you-create-a-project-for-the-first-time)
- [After you create a project](#connect-azure-openai-service-after-you-create-a-project)

### Connect Azure OpenAI Service when you create a project for the first time

When you create a project for the first time, you also create a hub. When you create a hub, you can select an existing Azure AI services resource (including Azure OpenAI) or create a new AI services resource.

:::image type="content" source="../../media/how-to/projects/projects-create-resource.png" alt-text="Screenshot of the create resource page within the create project dialog." lightbox="../../media/how-to/projects/projects-create-resource.png":::

For more details about creating a project, see the [create an Azure AI Foundry project](../../how-to/create-projects.md) how-to guide or the [create a project and use the chat playground](../../quickstarts/get-started-playground.md) quickstart.

### Connect Azure OpenAI Service after you create a project

If you already have a project and you want to connect your existing Azure OpenAI Service resources, follow these steps:

1. Go to your Azure AI Foundry project.
1. Select **Management center** from the left pane.
1. Select **Connected resources** (under **Project**) from the left pane. 
1. Select **+ New connection**.

    :::image type="content" source="../../media/ai-services/connections-add.png" alt-text="Screenshot of the connected resources page with the button to create a new connection." lightbox="../../media/ai-services/connections-add.png":::

1. On the **Add a connection to external assets** page, select the kind of AI service that you want to connect to the project. For example, you can select Azure OpenAI Service, Azure AI Content Safety, Azure AI Speech, Azure AI Language, and other AI services.

    :::image type="content" source="../../media/ai-services/connections-add-assets.png" alt-text="Screenshot of the page to select the kind of AI service that you want to connect to the project." lightbox="../../media/ai-services/connections-add-assets.png":::

1. On the next page in the wizard, browse or search to find the resource you want to connect. Then select **Add connection**.  

    :::image type="content" source="../../media/ai-services/connections-add-azure-openai.png" alt-text="Screenshot of the page to select the Azure AI Service resource that you want to connect to the project." lightbox="../../media/ai-services/connections-add-azure-openai.png":::

1. After the resource is connected, select **Close** to return to the **Connected resources** page. You should see the new connection listed.

## Try Azure OpenAI models in the playgrounds

You can try Azure OpenAI models in the Azure OpenAI Service playgrounds outside of a project.

> [!TIP]
> You can also try Azure OpenAI models in the project-level playgrounds. However, while you're only working with the Azure OpenAI Service models, we recommend working outside of a project.

1. Go to the [Azure OpenAI Service page](https://ai.azure.com/resource/overview) in Azure AI Foundry portal.
1. Select a playground from under **Resource playground** in the left pane.

    :::image type="content" source="../../media/ai-services/playgrounds/azure-openai-studio-playgrounds.png" alt-text="Screenshot of the playgrounds that you can select to use Azure OpenAI Service." lightbox="../../media/ai-services/playgrounds/azure-openai-studio-playgrounds.png":::

Here are a few guides to help you get started with Azure OpenAI Service playgrounds:
- [Quickstart: Use the chat playground](../../quickstarts/get-started-playground.md)
- [Quickstart: Get started using Azure OpenAI Assistants](../../../ai-services/openai/assistants-quickstart.md?context=/azure/ai-studio/context/context)
- [Quickstart: Use GPT-4o in the real-time audio playground](../../../ai-services/openai/realtime-audio-quickstart.md?context=/azure/ai-studio/context/context)
- [Quickstart: Analyze images and video in the chat playground](/azure/ai-services/openai/gpt-v-quickstart)

Each playground has different model requirements and capabilities. The supported regions vary depending on the model. For more information about model availability per region, see the [Azure OpenAI Service models documentation](../../../ai-services/openai/concepts/models.md).

## Fine-tune Azure OpenAI models

In Azure AI Foundry portal, you can fine-tune several Azure OpenAI models. The purpose is typically to improve model performance on specific tasks or to introduce information that wasn't well represented when you originally trained the base model.

1. Go to the [Azure OpenAI Service page](https://ai.azure.com/resource/overview) in Azure AI Foundry portal to fine-tune Azure OpenAI models.
1. Select **Fine-tuning** from the left pane.

    :::image type="content" source="../../media/ai-services/fine-tune-azure-openai.png" alt-text="Screenshot of the page to select fine-tuning of Azure OpenAI Service models." lightbox="../../media/ai-services/fine-tune-azure-openai.png":::

1. Select **+ Fine-tune model** in the **Generative AI fine-tuning** tabbed page.
1. Follow the [detailed how to guide](../../../ai-services/openai/how-to/fine-tuning.md?context=/azure/ai-studio/context/context) to fine-tune the model.

For more information about fine-tuning Azure AI models, see:
- [Overview of fine-tuning in Azure AI Foundry portal](../../concepts/fine-tuning-overview.md)
- [How to fine-tune Azure OpenAI models](../../../ai-services/openai/how-to/fine-tuning.md?context=/azure/ai-studio/context/context)
- [Azure OpenAI models that are available for fine-tuning](../../../ai-services/openai/concepts/models.md?context=/azure/ai-studio/context/context)


## Deploy models to production

You can deploy Azure OpenAI base models and fine-tuned models to production via the Azure AI Foundry portal.

1. Go to the [Azure OpenAI Service page](https://ai.azure.com/resource/overview) in Azure AI Foundry portal.
1. Select **Deployments** from the left pane.

    :::image type="content" source="../../media/ai-services/endpoint/models-endpoints-azure-openai-deployments.png" alt-text="Screenshot of the models and endpoints page to view and create Azure OpenAI Service deployments." lightbox="../../media/ai-services/endpoint/models-endpoints-azure-openai-deployments.png":::

You can create a new deployment or view existing deployments. For more information about deploying Azure OpenAI models, see [Deploy Azure OpenAI models to production](../../how-to/deploy-models-openai.md).

## Develop apps with code

At some point, you want to develop apps with code. Here are some developer resources to help you get started with Azure OpenAI Service and Azure AI services:
- [Azure OpenAI Service and Azure AI services SDKs](../../../ai-services/reference/sdk-package-resources.md?context=/azure/ai-studio/context/context)
- [Azure OpenAI Service and Azure AI services REST APIs](../../../ai-services/reference/rest-api-resources.md?context=/azure/ai-studio/context/context)
- [Quickstart: Get started building a chat app using code](../../quickstarts/get-started-code.md)
- [Quickstart: Get started using Azure OpenAI Assistants](../../../ai-services/openai/assistants-quickstart.md?context=/azure/ai-studio/context/context)
- [Quickstart: Use real-time speech to text](../../../ai-services/speech-service/get-started-speech-to-text.md?context=/azure/ai-studio/context/context)


## Related content

- [Azure OpenAI in Azure AI Foundry portal](../../azure-openai-in-ai-foundry.md)
- [Use Azure AI services resources](./connect-ai-services.md)

---
---
---