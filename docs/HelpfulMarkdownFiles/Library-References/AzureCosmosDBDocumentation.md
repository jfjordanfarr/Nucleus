---
title: Unified AI Database
titleSuffix: Azure Cosmos DB
description: Review Azure Cosmos DB as a NoSQL, relational, and vector database for the AI era that has unmatched reliability and flexibility for operational data needs.
author: markjbrown
ms.author: mjbrown
ms.service: azure-cosmos-db
ms.topic: overview
ms.date: 12/03/2024
adobe-target: true
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB
  - ✅ MongoDB vCore
  - ✅ Cassanda
  - ✅ Gremlin
  - ✅ Table
  - ✅ PostgreSQL
---

# Azure Cosmos DB - Database for the AI Era

> "OpenAI relies on Cosmos DB to dynamically scale their ChatGPT service – one of the fastest-growing consumer apps ever – enabling high reliability and low maintenance."
> – Satya Nadella, Microsoft chairman and chief executive officer

Today's applications are required to be highly responsive and always online. They must respond in real time to large changes in usage at peak hours, store ever increasing volumes of data, and make this data available to users in milliseconds. To achieve low latency and high availability, instances of these applications need to be deployed in datacenters that are close to their users.

The surge of AI-powered applications created another layer of complexity, because many of these applications integrate a multitude of data stores. For example, some organizations built applications that simultaneously connect to MongoDB, Postgres, Redis, and Gremlin. These databases differ in implementation workflow and operational performances, posing extra complexity for scaling applications.

Azure Cosmos DB simplifies and expedites your application development by being the single database for your operational data needs, from [geo-replicated distributed caching](https://medium.com/@marcodesanctis2/using-azure-cosmos-db-as-your-persistent-geo-replicated-distributed-cache-b381ad80f8a0) to backup storage, to [vector indexing and search](vector-database.md). It provides the data infrastructure for modern applications like [AI agent](ai-agents.md), digital commerce, Internet of Things, and booking management. It can accommodate all your operational data models, including relational, document, vector, key-value, graph, and table.

## An AI database providing industry-leading capabilities...

### ...for free

Azure Cosmos DB is a fully managed NoSQL, relational, and vector database. It offers single-digit millisecond response times, automatic and instant scalability, along with guaranteed speed at any scale. Business continuity is assured with [SLA-backed](https://azure.microsoft.com/support/legal/sla/cosmos-db) availability and enterprise-grade security.

App development is faster and more productive thanks to:

- Turnkey multi-region data distribution anywhere in the world
- Open source APIs
- SDKs for popular languages
- AI database functionalities like integrated vector database or seamless integration with Azure AI Services to support Retrieval Augmented Generation
- Query Copilot for generating NoSQL queries based on your natural language prompts ([preview](nosql/query/how-to-enable-use-copilot.md))

As a fully managed service, Azure Cosmos DB takes database administration off your hands with automatic management, updates, and patching. It also handles capacity management with cost-effective serverless and automatic scaling options that respond to application needs to match capacity with demand.

The following free options are available:

- [Azure Cosmos DB lifetime free tier](free-tier.md) provides 1000 [RU/s](request-units.md) of throughput and 25 GB of storage free.
- [Azure AI Advantage](ai-advantage.md) offers 40,000 [RU/s](request-units.md) of throughput for 90 days (equivalent of up to $6,000) to Azure AI or GitHub Copilot customers.
- [Try Azure Cosmos DB free](https://azure.microsoft.com/try/cosmosdb/) for 30 days without creating an Azure account; no commitment follows when the trial period ends.

When you decide that Azure Cosmos DB is right for you, you can receive up to 63% discount on [Azure Cosmos DB prices through Reserved Capacity](reserved-capacity.md).

> [!div class="nextstepaction"]
> [Try Azure Cosmos DB free](https://azure.microsoft.com/try/cosmosdb/)

> [!TIP]
> To learn more about Azure Cosmos DB, join us every Thursday at 1PM Pacific on Azure Cosmos DB Live TV. See the [Upcoming session schedule and past episodes](https://www.youtube.com/@AzureCosmosDB/streams).

### ...for more than just AI apps

Besides AI, Azure Cosmos DB should also be your goto database for a variety of use cases, including [retail and marketing](use-cases.md#retail-and-marketing), [IoT and telematics](use-cases.md#iot-and-telematics), [gaming](use-cases.md#gaming), [social](social-media-apps.md), and [personalization](use-cases.md#personalization), among others. Azure Cosmos DB is well positioned for solutions that handle massive amounts of data, reads, and writes at a global scale with near-real response times. Azure Cosmos DB's guaranteed high availability, high throughput, low latency, and tunable consistency are huge advantages when building these types of applications.

#### For what kinds of apps is Azure Cosmos DB a good fit

- **Flexible Schema for Iterative Development.** For example, apps wanting to adopt flexible modern DevOps practices and accelerate feature deployment timelines.
- **Latency sensitive workloads.** For example, real-time Personalization.
- **Highly elastic workloads.** For example, concert booking platform.
- **High throughput workloads.** For example, IoT device state/telemetry.
- **Highly available mission critical workloads.** For example, customer-facing Web Apps.

#### For what kinds of apps is Azure Cosmos DB a poor fit

- **Analytical workloads (OLAP).** For example, interactive, streaming, and batch analytics to enable Data Scientist / Data Analyst scenarios. Consider Microsoft Fabric instead.
- **Highly relational apps.** For example, white-label CRM applications. Consider Azure SQL, Azure Database for MySQL, or Azure Database for PostgreSQL instead.

### ...with unmatched reliability and flexibility

#### Guaranteed speed at any scale

Gain unparalleled [SLA-backed](https://azure.microsoft.com/support/legal/sla/cosmos-db) speed and throughput, fast global access, and instant elasticity.

- Real-time access with fast read and write latencies globally, and throughput and consistency all backed by [SLAs](https://azure.microsoft.com/support/legal/sla/cosmos-db)
- Multi-region writes and data distribution to any Azure region with just a button.
- Independently and elastically scale storage and throughput across any Azure region – even during unpredictable traffic bursts – for unlimited scale worldwide.

#### Simplified application development

Build fast with open-source APIs, multiple SDKs, schemaless data, and no-ETL analytics over operational data.

- Deeply integrated with key Azure services used in modern (cloud-native) app development including Azure Functions, IoT Hub, AKS (Azure Kubernetes Service), App Service, and more.
- Choose from multiple database APIs including the native API for NoSQL, MongoDB, PostgreSQL, Apache Cassandra, Apache Gremlin, and Table.
- Use Azure Cosmos DB as your unified AI database for data models like relational, document, vector, key-value, graph, and table.
- Build apps on API for NoSQL using the languages of your choice with SDKs for .NET, Java, Node.js, and Python. Or your choice of drivers for any of the other database APIs.
- Change feed makes it easy to track and manage changes to database containers and create triggered events with Azure Functions.
- Azure Cosmos DB's schema-less service automatically indexes all your data, regardless of the data model, to deliver blazing fast queries.

#### Mission-critical ready

Guarantee business continuity, 99.999% availability, and enterprise-level security for every application.

- Azure Cosmos DB offers a comprehensive suite of [SLAs](https://azure.microsoft.com/support/legal/sla/cosmos-db) including industry-leading availability worldwide.
- Easily distribute data to any Azure region with automatic data replication. Enjoy zero downtime with multi-region writes or RPO 0 when using Strong consistency.
- Enjoy enterprise-grade encryption-at-rest with self-managed keys.
- Azure role-based access control keeps your data safe and offers fine-tuned control.

#### Fully managed and cost-effective

End-to-end database management, with serverless and automatic scaling matching your application and total cost of ownership (TCO) needs.

- Fully managed database service. Automatic, no touch, maintenance, patching, and updates, saving developers time and money.
- Cost-effective options for unpredictable or sporadic workloads of any size or scale, enabling developers to get started easily without having to plan or manage capacity.
- Serverless model offers spiky workloads automatic and responsive service to manage traffic bursts on demand.
- Autoscale provisioned throughput automatically and instantly scales capacity for unpredictable workloads, while maintaining [SLAs](https://azure.microsoft.com/support/legal/sla/cosmos-db).

#### Azure Synapse Link for Azure Cosmos DB

[Azure Synapse Link for Azure Cosmos DB](synapse-link.md) is a cloud-native hybrid transactional and analytical processing (HTAP) capability that enables analytics at near real-time over operational data in Azure Cosmos DB. Azure Synapse Link creates a tight seamless integration between Azure Cosmos DB and Azure Synapse Analytics.

- Reduced analytics complexity with No ETL jobs to manage.
- Near real-time insights into your operational data.
- No effect on operational workloads.
- Optimized for large-scale analytics workloads.
- Cost effective.
- Analytics for locally available, globally distributed, multi-region writes.
- Native integration with Azure Synapse Analytics.

## Related content

- Learn [how to choose an API](choose-api.md) in Azure Cosmos DB
  - [Get started with Azure Cosmos DB for NoSQL](nosql/quickstart-dotnet.md)
  - [Get started with Azure Cosmos DB for MongoDB](mongodb/create-mongodb-nodejs.md)
  - [Get started with Azure Cosmos DB for Apache Cassandra](cassandra/manage-data-dotnet.md)
  - [Get started with Azure Cosmos DB for Apache Gremlin](gremlin/quickstart-dotnet.md)
  - [Get started with Azure Cosmos DB for Table](table/quickstart-dotnet.md)
  - [Get started with Azure Cosmos DB for PostgreSQL](postgresql/quickstart-app-stacks-python.md)


  ----------
  ----------
  ----------


  ### YamlMime:FAQ
metadata:
  title: Frequently asked questions
  titleSuffix: Azure Cosmos DB
  description: Get answers to frequently asked questions about Azure Cosmos DB. Learn about capacity, performance levels, and scaling, and service features.
  author: markjbrown
  ms.author: mjbrown
  ms.service: azure-cosmos-db
  ms.topic: faq
  ms.date: 02/23/2024
title: Frequently asked questions about Azure Cosmos DB
summary: |
  [!INCLUDE[NoSQL, MongoDB, Cassandra, Gremlin, Table](includes/appliesto-nosql-mongodb-cassandra-gremlin-table.md)]
sections:
  - name: General
    questions:
      - question: |
          What are the typical use cases for Azure Cosmos DB?
        answer: |
          Azure Cosmos DB is well suited for web, mobile, gaming, and IoT use cases. In these use cases; automatic scale, predictable performance, fast order of millisecond response times, and the ability to query over schema-free data is important. Azure Cosmos DB lends itself to rapid development and supporting the continuous iteration of application data models. Applications that manage user-generated content and data often map to [common use cases for Azure Cosmos DB](use-cases.md).
      - question: |
          How does Azure Cosmos DB offer predictable performance?
        answer: |
          A [request unit](request-units.md) (RU) is the measure of throughput in Azure Cosmos DB. A single request unit throughput corresponds to the throughput of the `GET` HTTP action for a 1-kilobite document. Every operation in Azure Cosmos DB; including reads, writes, queries, and stored procedure executions; has a deterministic request unit value based on the throughput required to complete the operation. Instead of being forced to consider CPU, IO, and memory in relation to your application throughput, you can think in terms of request units.

          You can configure each Azure Cosmos DB container with provisioned throughput in terms of request units per second (RU/s). You can benchmark individual requests to measure in request units, and create a container to handle the sum of request units across all requests for that container in a second. You can also scale up or scale down your container's throughput as the needs of your application evolve. For more information on how to measure request units, see the [throughput calculator](https://cosmos.azure.com/capacitycalculator).
      - question: |
          How does Azure Cosmos DB support various data models such as key/value, columnar, document, and graph?
        answer: |
          Key/value (table), columnar, document, and graph data models are all natively supported because of the ARS (atoms, records, and sequences) design that Azure Cosmos DB is built on. Atoms, records, and sequences can be easily mapped and projected to various data models. The APIs for a subset of models are available using the ARS design (MongoDB RU, NoSQL, Table, Apache Cassandra, and Apache Gremlin). Azure Cosmos DB also supports other APIs such as MongoDB vCore, Cassandra MI, or PostgreSQL. 
      - question: |
          What is an Azure Cosmos DB container?
        answer: |
          A container is a group of items. Containers can span one or more partitions and can scale to handle practically unlimited volumes of storage or throughput.

          | | Containers known as |
          | --- | --- |
          | **Azure Cosmos DB for NoSQL** | Container |
          | **Azure Cosmos DB for MongoDB RU** | Collection |
          | **Azure Cosmos DB for MongoDB vCore** | Collection |
          | **Azure Cosmos DB for Apache Cassandra** | Table |
          | **Azure Cosmos DB for Apache Gremlin** | Graph |
          | **Azure Cosmos DB for Table** | Table |
          
          A container is a billable entity, where the throughput and used storage determines the cost. Each container is billed hourly, based on the provisioned throughput and used storage space. For more information, see [Azure Cosmos DB pricing](https://azure.microsoft.com/pricing/details/cosmos-db/).
      - question: |
          Can I use multiple APIs to access my data?
        answer: |
          Azure Cosmos DB is Microsoft's globally distributed, multi-model database service. Multi-model refers to Azure Cosmos DB's support for multiple APIs and data models. In this paradigm, different APIs use different data formats for storage and wire protocol. For example; NoSQL uses JSON, MongoDB uses binary-encoded JSON (BSON), Table uses Entity Data Model (EDM), Cassandra uses Cassandra Query Language (CQL), Gremlin uses JSON format. As a result, we recommend using the same API for all access to the data in a given account.
      - question: |
          Can I integrate Azure Cosmos DB directly with other services?
        answer: |
          Yes. Azure Cosmos DB APIs allow direct integration. For example, the Azure Cosmos DB REST APIs can be integrated with Azure API Management for CRUD operations, eliminating the need for intermediate services like Azure Functions.        
      - question: |
          Is Azure Cosmos DB HIPAA compliant?
        answer: |
          Yes, Azure Cosmos DB is HIPAA-compliant. HIPAA establishes requirements for the use, disclosure, and safeguarding of individually identifiable health information. For more information, see the [Microsoft Trust Center](/compliance/regulatory/offering-hipaa-hitech).
      - question: |
          What are the storage limits of Azure Cosmos DB?
        answer: |
          There's no limit to the total amount of data that a container can store in Azure Cosmos DB.
      - question: |
          What are the throughput limits of Azure Cosmos DB?
        answer: |
          There's no limit to the total amount of throughput that a container can support in Azure Cosmos DB. The key idea is to distribute your workload roughly even among a sufficiently large number of partition keys.
      - question: |
          Are direct and gateway connectivity modes encrypted?
        answer: |
          Yes both modes are always fully encrypted.
      - question: |
          How much does Azure Cosmos DB cost?
        answer: |
          The number of provisioned containers, number of hours containers were online, and the provisioned throughput for each container determines Azure Cosmos DB usage charges. For more pricing details, refer to [Azure Cosmos DB pricing](https://azure.microsoft.com/pricing/details/cosmos-db/).
      - question: |
          How can I get extra help with Azure Cosmos DB?
        answer: |
          To ask a technical question, you can post to one of these two question and answer forums:

          - [Microsoft Question & Answers (Q&A)](/answers/topics/azure-cosmos-db.html)
          - [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-cosmosdb). Stack Overflow is best for programming questions. Make sure your question is [on-topic](https://stackoverflow.com/help/on-topic) and [provide as many details as possible, making the question clear and answerable](https://stackoverflow.com/help/how-to-ask).

          To fix an issue with your account, file a [support request](https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade/newsupportrequest) in the Azure portal.
  - name: Migrating Azure Cosmos DB Accounts across different resource groups, subscriptions, and tenants
    questions:
    - question: |
        How do I migrate an Azure Cosmos DB account to a different resource group or to a different subscription?
      answer: |
        The general guideline to migrate a Cosmos DB account to a different resource group or subscription is described in the [moving Azure resources to a new resource group or subscription](/azure/azure-resource-manager/management/move-resource-group-and-subscription) article.

        After successfully moving the Azure Cosmos DB account per the general guideline, any identities (System-Assigned or User-Assigned) associated with the account must be [reassigned](/azure/cosmos-db/how-to-setup-managed-identity). This is required in order to ensure that these identities continue to have the necessary permissions to access the Key Vault key.
        > [!WARNING]
        > If your Cosmos DB account has Customer Managed Keys enabled, you can only migrate the account to a different resource group or subscription if it's in an Active state. Accounts in a Revoked state can't be migrated.
    - question: |
        How do I migrate an Azure Cosmos DB account to a different tenant?
      answer: |
        If your Cosmos DB account has Customer Managed Keys enabled, you can only migrate the account if it is a cross-tenant customer-managed key account. For more information, see the guide on [configuring cross-tenant customer-managed keys for your Azure Cosmos DB account with Azure Key Vault](/azure/cosmos-db/how-to-setup-cross-tenant-customer-managed-keys?tabs=azure-portal).
        > [!WARNING]
        > After migrating, it's crucial to keep the Azure Cosmos DB account and the Azure Key Vault in separate tenants to preserve the original cross-tenant relationship. Ensure the Key Vault key remains in place until the Cosmos DB account migration is complete.
  - name: Try Azure Cosmos DB free
    questions:
      - question: |
          Is a free account available?
        answer: |
          Yes, you can sign up for a time-limited account at no charge, with no commitment. To sign up, visit [Try Azure Cosmos DB for free](https://azure.microsoft.com/try/cosmosdb/).

          If you're new to Azure, you can sign up for an [Azure free account](https://azure.microsoft.com/free/), which gives you 30 days and a credit to try all the Azure services. If you have a Visual Studio subscription, you're also eligible for [free Azure credits](https://azure.microsoft.com/pricing/member-offers/msdn-benefits-details/) to use on any Azure service.

          You can also use the [Azure Cosmos DB Emulator](emulator.md) to develop and test your application locally for free, without creating an Azure subscription. When you're satisfied with how your application is working in the Azure Cosmos DB Emulator, you can switch to using an Azure Cosmos DB account in the cloud.
      - question: |
          How do I try Azure Cosmos DB entirely free?
        answer: |
          You can access a time-limited Azure Cosmos DB experience without a subscription, free of charge, and commitments. To sign up for a Try Azure Cosmos DB subscription, go to [Try Azure Cosmos DB for free](https://azure.microsoft.com/try/cosmosdb/) and use any personal Microsoft account (MSA).

          This subscription is distinct from the [Azure Free Trial](https://azure.microsoft.com/free/), and can be used along with an Azure Free Trial or an Azure paid subscription.

          Try Azure Cosmos DB subscriptions appear in the Azure portal with other subscriptions associated with your user ID.

          The following conditions apply to Try Azure Cosmos DB subscriptions:

          - Account access can be granted to personal Microsoft accounts (MSA). Avoid using Microsoft Entra accounts or accounts belonging to corporate Microsoft Entra tenants, they might have limitations in place that could block access granting.
          - One [throughput provisioned container](./set-throughput.md#set-throughput-on-a-container) per subscription for API for NoSQL, Gremlin, and Table accounts.
          - Up to three [throughput provisioned collections](./set-throughput.md#set-throughput-on-a-container) per subscription for MongoDB accounts.
          - One [throughput provisioned database](./set-throughput.md#set-throughput-on-a-database) per subscription. Throughput provisioned databases can contain any number of containers inside.
          - 10-GB storage capacity.
          - Global replication is available in the following [Azure regions](https://azure.microsoft.com/regions/): Central US, North Europe, and Southeast Asia
          - Maximum throughput of 5 K RU/s when provisioned at the container level.
          - Maximum throughput of 20 K RU/s when provisioned at the database level.
          - Subscriptions expire after 30 days, and can be extended to a maximum of 31 days total. After expiration, the information contained is deleted.
          - Azure support tickets can't be created for Try Azure Cosmos DB accounts; however, support is provided for subscribers with existing support plans.
  - name: Get started with Azure Cosmos DB
    questions:
      - question: |
          How do I sign up for Azure Cosmos DB?
        answer: |
          Azure Cosmos DB is available in the Azure portal. First, sign up for an Azure subscription. After you sign up, add an Azure Cosmos DB account to your Azure subscription.
      - question: |
          How do I authenticate to Azure Cosmos DB?
        answer: |
          A primary key is a security token to access all resources for an account. Individuals with the key have read and write access to all resources in the database account. Multiple keys are available on the **Keys** section of the [Azure portal](https://portal.azure.com).

          Use caution when you distribute primary keys.
      - question: |
          Where is Azure Cosmos DB available?
        answer: |
          For information about regional availability for Azure Cosmos DB, see [Azure products available by region](https://azure.microsoft.com/explore/global-infrastructure/products-by-region/?products=cosmos-db). You can account your database to one or more of these regions.

          The software development kits (SDKs) for Azure Cosmos DB allow configuration of the regions they use for connections. In most SDKs, the `PreferredLocations`` value is set to any of the Azure regions in which Azure Cosmos DB is available.
      - question: |
          Is there anything I should be aware of when distributing data across the world via the Azure datacenters?
        answer: |
          Azure Cosmos DB is present across all Azure regions, as specified on the [Azure regions](https://azure.microsoft.com/regions/) page. Because it's a core Azure service, every new datacenter has an Azure Cosmos DB presence.

          When you set a region, remember that Azure Cosmos DB respects sovereign and government clouds. For example, you can't replicate data out of a [sovereign region](https://azure.microsoft.com/global-infrastructure/). Similarly, you can't enable replication into other sovereign locations from an outside account.
      - question: |
          Is it possible to switch between container-level and database-level throughput provisioning?
        answer: |
          Container and database-level throughput provisioning are separate offerings and switching between either of these require migrating data from source to destination. You need to create a new database or container and then migrate data using [bulk executor library](bulk-executor-overview.md) or [Azure Data Factory](/azure/data-factory/connector-azure-cosmos-db).
      - question: |
          Does Azure Cosmos DB support time series analysis?
        answer: |
          Yes, Azure Cosmos DB supports time series analysis. You can use the change feed to build aggregated views over time series data. You can extend this approach by using Apache Spark streaming or another stream data processor.
      - question: |
          What are the Azure Cosmos DB service quotas and throughput limits?
        answer: |
          For information about service quotas and throughput limits, see [service quotas](concepts-limits.md) and [throughout limits](set-throughput.md#comparison-of-models).
additionalContent: |
  ## Related content

  - Frequently asked questions about [Azure Cosmos DB for NoSQL](nosql/faq.yml)
  - Frequently asked questions about [Azure Cosmos DB for MongoDB](mongodb/faq.yml)
  - Frequently asked questions about [Azure Cosmos DB for Apache Gremlin](gremlin/faq.yml)
  - Frequently asked questions about [Azure Cosmos DB for Apache Cassandra](cassandra/faq.yml)
  - Frequently asked questions about [Azure Cosmos DB for Table](table/faq.yml)

  -------
  -------
  -------
  -------



  ---
title: |
  Try Azure Cosmos DB free
description: |
  Try Azure Cosmos DB free. No credit card required. Test your apps, deploy, and run small workloads free for 30 days. Upgrade your account at any time.
author: meredithmooreux
ms.author: merae
ms.service: azure-cosmos-db
ms.custom: references_regions
ms.topic: overview
ms.date: 09/19/2024
---

# Try Azure Cosmos DB free

[!INCLUDE[NoSQL, MongoDB, Cassandra, Gremlin, Table](includes/appliesto-nosql-mongodb-cassandra-gremlin-table.md)]

[Try Azure Cosmos DB](https://aka.ms/trycosmosdb) for free before you commit with an Azure Cosmos DB sandbox account. There's no credit card required to get started. Your sandbox account is free for 30 days.  Your data is deleted at the expiration date.   

You can also upgrade your active trial sandbox account to a paid Azure subscription  at any time during the 30-day trial period.  You can only have one Try Azure Cosmos DB sandbox account at a time.  If you're using the API for NoSQL, after you upgrade to a paid Azure subscription and create a new Azure Cosmos DB account you can migrate the data from your Try Azure Cosmos DB sandbox account to your upgraded Azure subscription and Azure Cosmos DB account before the trial ends.

This article walks you through how to create your Try Azure Cosmos DB sandbox account, limits, and upgrading your account. It will also explain how to migrate your data from your Azure Cosmos DB sandbox to your own account using the API for NoSQL.

When you decide that Azure Cosmos DB is right for you, you can receive up to a 63% discount on  [Azure Cosmos DB prices through Reserved Capacity](reserved-capacity.md).

<br>

> [!VIDEO https://www.youtube.com/embed/7EFcxFGRB5Y?si=e7BiJ-JGK7WH79NG]

## Limits to free account

### [NoSQL / Cassandra/ Gremlin / Table](#tab/nosql+cassandra+gremlin+table)

The following table lists the limits for the [Try Azure Cosmos DB](https://aka.ms/trycosmosdb) for free trial sandbox.

| Resource | Limit |
| --- | --- |
| Duration of the trial | 30 days¹² |
| Maximum containers per subscription | 1 |
| Maximum throughput per container | 5,000 |
| Maximum throughput per shared-throughput database | 20,000 |
| Maximum total storage per account | 10 GB |

¹ A new Try Azure Cosmos DB sandbox account can be requested after expiration.

² After expiration, the information stored in your account is deleted. You can upgrade your account prior to expiration and migrate the information stored to an enterprise subscription.

> [!NOTE]
> The Try Azure Cosmos DB sandbox account supports global distribution in only the  **East US**, **North Europe**, **Southeast Asia**, and **North Central US** regions. By default, the Try Azure Cosmos DB sandbox account is created in East US.  Azure support tickets can't be created for Try Azure Cosmos DB sandbox accounts. If the account exceeds the maximum resource limits, it's automatically deleted.  Don’t use personal or sensitive data in your sandbox database during the trial period.

### [MongoDB](#tab/mongodb)

The following table lists the limits for the [Try Azure Cosmos DB](https://aka.ms/trycosmosdb) free trial sandbox.

| Resource | Limit |
| --- | --- |
| Duration of the trial | 30 days¹²  |
| Maximum containers per subscription | 3 |
| Maximum throughput per container | 5,000 |
| Maximum throughput per shared-throughput database | 20,000 |
| Maximum total storage per account | 10 GB |

¹ A new Try Azure Cosmos DB sandbox account can be requested after expiration
² After expiration, the information stored in your account is deleted. You can upgrade your account prior to expiration and migrate the information stored to an enterprise subscription.

> [!NOTE]
> The Try Azure Cosmos DB sandbox account supports global distribution in only the  **East US**, **North Europe**, **Southeast Asia**, and **North Central US** regions. By default, the Try Azure Cosmos DB sandbox account is created in East US.  Azure support tickets can't be created for Try Azure Cosmos DB sandbox accounts. If the account exceeds the maximum resource limits, it's automatically deleted.  Don’t use personal or sensitive data in your sandbox database during the trial period.

---

## Create your Try Azure Cosmos DB account

From the [Try Azure Cosmos DB home page](https://aka.ms/trycosmosdb), select an API. Azure Cosmos DB provides five APIs: NoSQL and MongoDB for document data, Gremlin for graph data, Azure Table, and Cassandra.

> [!NOTE]
> Not sure which API will best meet your needs? To learn more about the APIs for Azure Cosmos DB, see [Choose an API in Azure Cosmos DB](choose-api.md).

:::image type="content" source="media/try-free/try-cosmos-db-page.png" lightbox="media/try-free/try-cosmos-db-page.png" alt-text="Screenshot of the API options including NoSQL and MongoDB on the Try Azure Cosmos DB page.":::

## Launch a Quick Start

Launch the Quickstart in Data Explorer in Azure portal to start using Azure Cosmos DB or get started with our documentation.

* [API for NoSQL](nosql/quickstart-portal.md)
* [API for MongoDB](mongodb/quickstart-python.md#object-model)
* [API for Apache Cassandra](cassandra/adoption.md)
* [API for Apache Gremlin](gremlin/quickstart-console.md)
* [API for Table](table/quickstart-dotnet.md)

You can also get started with one of the learning resources in the Data Explorer.

:::image type="content" source="media/try-free/data-explorer.png" lightbox="media/try-free/data-explorer.png" alt-text="Screenshot of the Azure Cosmos DB Data Explorer landing page.":::

## Upgrade your account

Your Try Azure Cosmos DB sandbox account is free for 30 days. After expiration, a new sandbox account can be created. You can upgrade your active Try Azure Cosmos DB account at any time during the 30 day trial period.  If you're using the API for NoSQL, after you upgrade to a paid Azure subscription and create a new Azure Cosmos DB account you can migrate the data from your Try Azure Cosmos DB sandbox account to your upgraded Azure subscription and Azure Cosmos DB account before the trial ends. Here are the steps to start an upgrade.

### Start upgrade

1. From either the Azure portal or the Try Azure Cosmos DB free page, select the option to **Upgrade** your account.

    :::image type="content" source="media/try-free/upgrade-account.png" lightbox="media/try-free/upgrade-account.png" alt-text="Screenshot of the confirmation page for the account upgrade experience.":::

1. Choose to either **Sign up for an Azure account** or **Sign in** and create a new Azure Cosmos DB account following the instructions in the next section.

### Create a new account

> [!NOTE]
> While this example uses API for NoSQL, the steps are similar for the APIs for MongoDB, Cassandra, Gremlin, or Table.

[!INCLUDE[Create NoSQL account](includes/create-nosql-account.md)]

### Move data to new account

If you desire, you can migrate your existing data from the free sandbox account to the newly created account.

#### [NoSQL](#tab/nosql)

1. Navigate back to the **Upgrade** page from the [Start upgrade](#start-upgrade) section of this guide. Select **Next** to move on to the third step and move your data.

    :::image type="content" source="media/try-free/account-creation-options.png" lightbox="media/try-free/account-creation-options.png" alt-text="Screenshot of the sign-in/sign-up experience to upgrade your current account.":::

1. Locate your **Primary Connection string** for the Azure Cosmos DB account you created for your data. This information can be found within the **Keys** page of your new account.

    :::image type="content" source="media/try-free/account-keys.png" lightbox="media/try-free/account-keys.png" alt-text="Screenshot of the Keys page for an Azure Cosmos DB account.":::

1. Back in the **Upgrade** page from the [Start upgrade](#start-upgrade) section of this guide, insert the connection string of the new Azure Cosmos DB account in the **Connection string** field.

    :::image type="content" source="media/try-free/migrate-data.png" lightbox="media/try-free/migrate-data.png" alt-text="Screenshot of the migrate data options in the portal.":::

1. Select **Next** to move the data to your account. Provide your email address to be notified by email once the migration has been completed.

#### [MongoDB / Cassandra / Gremlin / Table](#tab/mongodb+cassandra+gremlin+table)

> [!IMPORTANT]
> Data migration is not available for the APIs for MongoDB, Cassandra, Gremlin, or Table.

---

## Delete your account

There can only be one free Try Azure Cosmos DB sandbox account per Microsoft account. You may want to delete your account or to try different APIs, you'll have to create a new account. Here’s how to delete your account.

1. Go to the [Try Azure Cosmos DB](https://aka.ms/trycosmosdb) page.

1. Select **Delete my account**.

    :::image type="content" source="media/try-free/delete-account.png" lightbox="media/try-free/delete-account.png" alt-text="Screenshot of the confirmation page for the account deletion experience.":::

## Comments

Use the **Feedback** icon in the command bar of the Data Explorer to give the product team any comments you have about the Try Azure Cosmos DB sandbox experience.

## Next steps

After you create a Try Azure Cosmos DB sandbox account, you can start building apps with Azure Cosmos DB with the following articles:

* Use [API for NoSQL to build a console app using .NET](nosql/quickstart-dotnet.md) to manage data in Azure Cosmos DB.
* Use [API for MongoDB to build a sample app using Python](mongodb/quickstart-python.md) to manage data in Azure Cosmos DB.
* [Create a notebook](nosql/tutorial-create-notebook-vscode.md) and analyze your data.
* Learn more about [understanding your Azure Cosmos DB bill](understand-your-bill.md)
* Get started with Azure Cosmos DB with one of our quickstarts:
  * [Get started with Azure Cosmos DB for NoSQL](nosql/quickstart-portal.md)
  * [Get started with Azure Cosmos DB for MongoDB](mongodb/quickstart-python.md#object-model)
  * [Get started with Azure Cosmos DB for Cassandra](cassandra/adoption.md)
  * [Get started with Azure Cosmos DB for Gremlin](gremlin/quickstart-console.md)
  * [Get started with Azure Cosmos DB for Table](table/quickstart-dotnet.md)
* Trying to do capacity planning for a migration to Azure Cosmos DB? You can use information about your existing database cluster for [capacity planning](sql/estimate-ru-with-capacity-planner.md).
* If all you know is the number of vCores and servers in your existing database cluster, see [estimating request units using vCores or vCPUs](convert-vcore-to-request-unit.md).
* If you know typical request rates for your current database workload, see [estimating request units using Azure Cosmos DB capacity planner](estimate-ru-with-capacity-planner.md).

------------
------------
------------
------------

---
title: Integrated vector store
titleSuffix: Azure Cosmos DB for NoSQL
description: Enhance AI-based applications using the integrated vector store functionality in Azure Cosmos DB for NoSQL
author: jcodella
ms.author: jacodel
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.custom:
  - build-2024
  - ignite-2024
ms.topic: concept-article
ms.date: 12/03/2024
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
---

# Vector Search in Azure Cosmos DB for NoSQL

Azure Cosmos DB for NoSQL now offers efficient vector indexing and search. This feature is designed to handle multi-modal, high-dimensional vectors, enabling efficient and accurate vector search at any scale. You can now store vectors directly in the documents alongside your data. Each document in your database can contain not only traditional schema-free data, but also multi-modal high-dimensional vectors as other properties of the documents. This colocation of data and vectors allows for efficient indexing and searching, as the vectors are stored in the same logical unit as the data they represent. Keeping vectors and data together simplifies data management, AI application architectures, and the efficiency of vector-based operations.

Azure Cosmos DB for NoSQL offers the flexibility it offers in choosing the vector indexing method: 
- A "flat" or k-nearest neighbors exact search (sometimes called brute-force) can provide 100% retrieval recall for smaller, focused vector searches. especially when combined with query filters and partition-keys.
- A quantized flat index that compresses vectors using DiskANN-based quantization methods for better efficiency in the kNN search.
- DiskANN, a suite of state-of-the-art vector indexing algorithms developed by Microsoft Research to power efficient, high accuracy multi-modal vector search at any scale.

[Learn more about vector indexing here](../index-policy.md#vector-indexes)

Vector search in Azure Cosmos DB can be combined with all other supported Azure Cosmos DB NoSQL query filters and indexes using `WHERE` clauses. This enables your vector searches to be the most relevant data to your applications.

This feature enhances the core capabilities of Azure Cosmos DB, making it more versatile for handling vector data and search requirements in AI applications.

<br>

> [!VIDEO https://www.youtube.com/embed/I6uui4Xx_YA?si=KwV2TxVH4t3UqIJk]

## What is a vector store?

A vector store or [vector database](../vector-database.md) is a database designed to store and manage vector embeddings, which are mathematical representations of data in a high-dimensional space. In this space, each dimension corresponds to a feature of the data, and tens of thousands of dimensions might be used to represent sophisticated data. A vector's position in this space represents its characteristics. Words, phrases, or entire documents, and images, audio, and other types of data can all be vectorized.

## How does a vector store work?

In a vector store, vector search algorithms are used to index and query embeddings. Some well-known vector search algorithms include Hierarchical Navigable Small World (HNSW), Inverted File (IVF), DiskANN, etc. Vector search is a method that helps you find similar items based on their data characteristics rather than by exact matches on a property field. This technique is useful in applications such as searching for similar text, finding related images, making recommendations, or even detecting anomalies. It's used to query the [vector embeddings](/azure/ai-services/openai/concepts/understand-embeddings) of your data that you created by using a machine learning model by using an embeddings API. Examples of embeddings APIs are [Azure OpenAI Embeddings](/azure/ai-services/openai/how-to/embeddings) or [Hugging Face on Azure](https://azure.microsoft.com/solutions/hugging-face-on-azure/). Vector search measures the distance between the data vectors and your query vector. The data vectors that are closest to your query vector are the ones that are found to be most similar semantically.

In the Integrated Vector Database in Azure Cosmos DB for NoSQL, embeddings can be stored, indexed, and queried alongside the original data. This approach eliminates the extra cost of replicating data in a separate pure vector database. Moreover, this architecture keeps the vector embeddings and original data together, which better facilitates multi-modal data operations, and enables greater data consistency, scale, and performance.

## Enable the vector indexing and search feature

Vector indexing and search in Azure Cosmos DB for NoSQL requires enabling on the Features page of your Azure Cosmos DB. Follow the below steps to register:

1. Navigate to your Azure Cosmos DB for NoSQL resource page.
1. Select the "Features" pane under the "Settings" menu item.
1. Select the “Vector Search in Azure Cosmos DB for NoSQL” feature.
1. Read the description of the feature to confirm you want to enable it.
1. Select "Enable" to turn on the vector indexing and search capability.

    > [!TIP]
    > Alternatively, use the Azure CLI to update the capabilities of your account to support NoSQL vector search.
    >
    > ```azurecli
    > az cosmosdb update \
    >      --resource-group <resource-group-name> \
    >      --name <account-name> \
    >      --capabilities EnableNoSQLVectorSearch
    > ```
    >

> [!NOTE]  
> The registration request will be autoapproved; however, it may take 15 minutes to fully activate on the account.

## Container Vector Policies

Performing vector search with Azure Cosmos DB for NoSQL requires you to define a vector policy for the container. This provides essential information for the database engine to conduct efficient similarity search for vectors found in the container's documents. This also informs the vector indexing policy of necessary information, should you choose to specify one. The following information is included in the contained vector policy:

- “path”: the property containing the vector (required).
- “datatype”: the data type of the vector property (default Float32).  
- “dimensions”: The dimensionality or length of each vector in the path. All vectors in a path should have the same number of dimensions. (default 1536).
- “distanceFunction”: The metric used to compute distance/similarity. Supported metrics are:
  - [cosine](https://en.wikipedia.org/wiki/Cosine_similarity), which has values from -1 (least similar) to +1 (most similar).
  - [dot product](https://en.wikipedia.org/wiki/Dot_product), which has values from -inf (least similar) to +inf (most similar).
  - [euclidean](https://en.wikipedia.org/wiki/Euclidean_distance), which has values from 0 (most similar) to +inf) (least similar).

> [!NOTE]
> Each unique path can have at most one policy. However, multiple policies can be specified provided that they all target a different path.

The container vector policy can be described as JSON objects. Here are two examples of valid container vector policies:

### A policy with a single vector path

```json
{
    "vectorEmbeddings": [
        {
            "path":"/vector1",
            "dataType":"float32",
            "distanceFunction":"cosine",
            "dimensions":1536
        }
    ]
}
```

### A policy with two vector paths

```json
{
    "vectorEmbeddings": [
        {
            "path":"/vector1",
            "dataType":"float32",
            "distanceFunction":"cosine",
            "dimensions":1536
        },
        {
            "path":"/vector2",
            "dataType":"int8",
            "distanceFunction":"dotproduct",
            "dimensions":100
        }
    ]
}
```

## Vector indexing policies

**Vector** indexes increase the efficiency when performing vector searches using the `VectorDistance` system function. Vectors searches have lower latency, higher throughput, and less RU consumption when using a vector index.  You can specify the following types of vector index policies:

| Type | Description | Max dimensions |
| --- | --- |
| **`flat`** | Stores vectors on the same index as other indexed properties. | 505 |
| **`quantizedFlat`** | Quantizes (compresses) vectors before storing on the index. This can improve latency and throughput at the cost of a small amount of accuracy. | 4096 |
| **`diskANN`** | Creates an index based on DiskANN for fast and efficient approximate search. | 4096 |

> [!NOTE]
> The `quantizedFlat` and `diskANN` indexes requires that at least 1,000 vectors to be inserted. This is to ensure accuracy of the quantization process. If there are fewer than 1,000 vectors, a full scan is executed instead and will lead to higher RU charges for a vector search query.

A few points to note:

- The `flat` and `quantizedFlat` index types uses Azure Cosmos DB's index to store and read each vector when performing a vector search. Vector searches with a `flat` index are brute-force searches and produce 100% accuracy or recall. That is, it's guaranteed to find the most similar vectors in the dataset. However, there's a limitation of `505` dimensions for vectors on a flat index.

- The `quantizedFlat` index stores quantized (compressed) vectors on the index. Vector searches with `quantizedFlat` index are also brute-force searches, however their accuracy might be slightly less than 100% since the vectors are quantized before adding to the index. However, vector searches with `quantized flat` should have lower latency, higher throughput, and lower RU cost than vector searches on a `flat` index. This is a good option for smaller scenarios, or scenarios where you're using query filters to narrow down the vector search to a relatively small set of vectors. `quantizedFlat` is recommended when the number of vectors to be indexed is somewhere around 50,000 or fewer per physical partition. However, this is just a general guideline and actual performance should be tested as each scenario can be different.

- The `diskANN` index is a separate index defined specifically for vectors using [DiskANN](https://www.microsoft.com/research/publication/diskann-fast-accurate-billion-point-nearest-neighbor-search-on-a-single-node/), a suite of high performance vector indexing algorithms developed by Microsoft Research. DiskANN indexes can offer some of the lowest latency, highest throughput, and lowest RU cost queries, while still maintaining high accuracy. In general, DiskANN is the most performant of all index types if there are more than 50,000 vectors per physical partition.

Here are examples of valid vector index policies:

```json
{
    "indexingMode": "consistent",
    "automatic": true,
    "includedPaths": [
        {
            "path": "/*"
        }
    ],
    "excludedPaths": [
        {
            "path": "/_etag/?"
        },
        {
            "path": "/vector1/*"
        }
    ],
    "vectorIndexes": [
        {
            "path": "/vector1",
            "type": "diskANN"
        }
    ]
}
```

```json
{
    "indexingMode": "consistent",
    "automatic": true,
    "includedPaths": [
        {
            "path": "/*"
        }
    ],
    "excludedPaths": [
        {
            "path": "/_etag/?"
        },
        {
            "path": "/vector1/*",
        },
        {
            "path": "/vector2/*",
        }
    ],
    "vectorIndexes": [
        {
            "path": "/vector1",
            "type": "quantizedFlat"
        },
        {
            "path": "/vector2",
            "type": "diskANN"
        }
    ]
}
```

> [!IMPORTANT]
> The vector path added to the "excludedPaths" section of the indexing policy to ensure optimized performance for insertion. Not adding the vector path to "excludedPaths" will result in higher RU charge and latency for vector insertions.

> [!IMPORTANT]
> Wild card characters (*, []) are not currently supported in the vector policy or vector index.

## Perform vector search with queries using VectorDistance()

Once you created a container with the desired vector policy, and inserted vector data into the container, you can conduct a vector search using the [Vector Distance](query/vectordistance.md) system function in a query. An example of a NoSQL query that projects the similarity score as the alias `SimilarityScore`, and sorts in order of most-similar to least-similar:

```sql
SELECT TOP 10 c.title, VectorDistance(c.contentVector, [1,2,3]) AS SimilarityScore   
FROM c  
ORDER BY VectorDistance(c.contentVector, [1,2,3])   
```

> [!IMPORTANT]
> Always use a `TOP N` clause in the `SELECT` statement of a query. Otherwise the vector search will try to return many more results and the query will cost more RUs and have higher latency than necessary.

## Current limitations

Vector indexing and search in Azure Cosmos DB for NoSQL has some limitations. 
- `quantizedFlat` and `diskANN` indexes require at least 1,000 vectors to be indexed to ensure that the quantization is accurate. If fewer than 1,000 vectors are indexed, then a full-scan is used instead and RU charges may be higher. 
- Vectors indexed with the `flat` index type can be at most 505 dimensions. Vectors indexed with the `quantizedFlat` or `DiskANN` index type can be at most 4,096 dimensions.
- The rate of vector insertions should be limited. Very large ingestion (in excess of 5M vectors) may require additional index build time. 
- The vector search feature is not currently supported on the existing containers. To use it, a new container must be created, and the container-level vector embedding policy must be specified.
- Shared throughput databases are unsupported.
- At this time, vector indexing and search is not supported on accounts with Analytical Store (and Synapse Link) and Shared Throughput.
- Once vector indexing and search is enabled on a container, it cannot be disabled.

## Related content

- [DiskANN + Azure Cosmos DB - Microsoft Mechanics Video](https://www.youtube.com/watch?v=MlMPIYONvfQ)
- [.NET - How-to Index and query vector data](how-to-dotnet-vector-index-query.md)
- [Python - How-to Index and query vector data](how-to-python-vector-index-query.md)
- [Java - How-to Index and query vector data](how-to-java-vector-index-query.md)
- [VectorDistance system function](query/vectordistance.md)
- [Vector index overview](../index-overview.md#vector-indexes)
- [Vector index policies](../index-policy.md#vector-indexes)
- [Manage index](how-to-manage-indexing-policy.md#vector-indexing-policy-examples)
- Integrations:
  - [LangChain, Python](https://python.langchain.com/v0.2/docs/integrations/vectorstores/azure_cosmos_db_no_sql/)
  - [Semantic Kernel, .NET](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Connectors/Connectors.Memory.AzureCosmosDBNoSQL)
  - [Semantic Kernel, Python](https://github.com/microsoft/semantic-kernel/tree/main/python/semantic_kernel/connectors/memory/azure_cosmosdb_no_sql)

## Next step

> [!div class="nextstepaction"]
> [Use the Azure Cosmos DB lifetime free tier](../free-tier.md)

----------
----------
----------
----------

---
title: Use full-text search (preview)
titleSuffix: Azure Cosmos DB for NoSQL
description: Overview of full text search for querying data using "best matching 25" scoring in Azure Cosmos DB for NoSQL.
author: jcodella
ms.author: jacodel
ms.service: azure-cosmos-db
ms.topic: how-to
ms.date: 03/10/2025
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
---

# Full-text search in Azure Cosmos DB for NoSQL (preview)

Azure Cosmos DB for NoSQL now offers a powerful Full Text Search feature in preview, designed to enhance the search capabilities of your applications.

## Prerequisites

- Azure Cosmos DB for NoSQL account
- [Vector search](vector-search-overview.md) feature enabled

## What is full text search?

Azure Cosmos DB for NoSQL now offers a powerful Full Text Search feature in preview, designed to enhance your data querying capabilities. This feature includes advanced text processing techniques such as stemming, stop word removal, and tokenization, enabling efficient and effective text searches through a specialized text index. Full text search also includes *full text scoring* with a function that evaluates the relevance of documents to a given search query. BM25, or Best Matching 25, considers factors like term frequency, inverse document frequency, and document length to score and rank documents. This helps ensure that the most relevant documents appear at the top of the search results, improving the accuracy and usefulness of text searches.

Full Text Search is ideal for a variety of scenarios, including:

- **E-commerce**: Quickly find products based on descriptions, reviews, and other text attributes.
- **Content management**: Efficiently search through articles, blogs, and documents.
- **Customer support**: Retrieve relevant support tickets, FAQs, and knowledge base articles.
- **User content**: Analyze and search through user-generated content such as posts and comments.
- **RAG for chatbots**: Enhance chatbot responses by retrieving relevant information from large text corpora, improving the accuracy and relevance of answers.
- **Multi-Agent AI apps**: Enable multiple AI agents to collaboratively search and analyze vast amounts of text data, providing comprehensive and nuanced insights.

## How to use full text search

1. Enable the "Full Text & Hybrid Search for NoSQL" preview feature.
2. Configure a container with a full text policy and full text index.
3. Insert your data with text properties.
4. Run hybrid queries against the data.

## Enable the full text and hybrid search for NoSQL preview feature

Full text search, full text scoring, and hybrid search all require enabling the preview feature on your Azure Cosmos DB for NoSQL account before using. Follow the below steps to register:

1. Navigate to your Azure Cosmos DB for NoSQL resource page.
2. Select the "Features" pane under the "Settings" menu item.
3. Select the "Full-Text & Hybrid Search for NoSQL API (preview)" feature.
4. Read the description of the feature to confirm you want to enable it.
5. Select "Enable" to turn on the vector indexing and search capability.

:::image type="content" source="../nosql/media/full-text-search/full-text-search-feature.png" alt-text="Screenshot of full text and hybrid search preview feature in the Azure portal.":::

### Configure container policies and indexes for hybrid search

To use full text search capabilities, you'll first need to define two policies:
- A container-level full text policy that defines what paths will contain text for the new full text query system functions.
- A full text index added to the indexing policy that enables efficient search.

### Full text policy

For every text property you'd like to configure for full text search, you must declare both the `path` of the property with text and the `language` of the text. A simple full text policy can be:

 ```json
{
    "defaultLanguage": "en-US",
    "fullTextPaths": [
        {
            "path": "/text",
            "language": "en-US"
        }
    ]
}
```

Defining multiple text paths is easily done by adding another element to the `fullTextPolicy` array:

 ```json
{
    "defaultLanguage": "en-US",
    "fullTextPaths": [
        {
            "path": "/text1",
            "language": "en-US"
        },
        {
            "path": "/text2",
            "language": "en-US"
        }
    ]
}
```

> [!NOTE]
> English ("en-us" as the language) is the only supported language at this time.

> [!IMPORTANT]
> Wild card characters (*, []) are not currently supported in the full text policy or full text index.

### Full text index

Any full text search operations should make use of a [*full text index*](../index-policy.md#full-text-indexes). A full text index can easily be defined in any Azure Cosmos DB for NoSQL index policy per the example below.

```json
{
    "indexingMode": "consistent",
    "automatic": true,
    "includedPaths": [
        {
            "path": "/*"
        }
    ],
    "excludedPaths": [
        {
            "path": "/\"_etag\"/?"
        },
    ],
    "fullTextIndexes": [
        {
            "path": "/text"
        }
    ]
}
```

Just as with the full text policies, full text indexes can be defined on multiple paths.

```json
{
    "indexingMode": "consistent",
    "automatic": true,
    "includedPaths": [
        {
            "path": "/*"
        }
    ],
    "excludedPaths": [
        {
            "path": "/\"_etag\"/?"
        },
    ],
    "fullTextIndexes": [
        {
            "path": "/text"
        },
        {
            "path": "/text2"
        }
    ]
}
```

### Full text search queries

Full text search and scoring operations are performed using the following system functions in the Azure Cosmos DB for NoSQL query language:

- [`FullTextContains`](../nosql/query/fulltextcontains.md): Returns `true` if a given string is contained in the specified property of a document. This is useful in a `WHERE` clause when you want to ensure specific key words are included in the documents returned by your query.
- [`FullTextContainsAll`](../nosql/query/fulltextcontainsall.md): Returns `true` if *all* of the given strings are contained in the specified property of a document. This is useful in a `WHERE` clause when you want to ensure that multiple key words are included in the documents returned by your query.
- [`FullTextContainsAny`](../nosql/query/fulltextcontainsany.md): Returns `true` if *any* of the given strings are contained in the specified property of a document. This is useful in a `WHERE` clause when you want to ensure that at least one of the key words is included in the documents returned by your query.
- [`FullTextScore`](../nosql/query/fulltextscore.md): Returns a score. This can only be used in an `ORDER BY RANK` clause, where the returned documents are ordered by the rank of the full text score, with most relevant (highest scoring) documents at the top, and least relevant (lowest scoring) documents at the bottom.

Here are a few examples of each function in use.

#### FullTextContains

In this example, we want to obtain the first 10 results where the keyword "bicycle" is contained in the property `c.text`.

```sql
SELECT TOP 10 *
FROM c
WHERE FullTextContains(c.text, "bicycle")
```

#### FullTextContainsAll

In this example, we want to obtain first 10 results where the keywords "red" and "bicycle" are contained in the property `c.text`.

```sql
SELECT TOP 10 *
FROM c
WHERE FullTextContainsAll(c.text, "red", "bicycle")
```

#### FullTextContainsAny

In this example, we want to obtain the first 10 results where the keywords "red" and either "bicycle" or "skateboard"  are contained in the property `c.text`.

```sql
SELECT TOP 10 *
FROM c
WHERE FullTextContains(c.text, "red") AND FullTextContainsAny(c.text, "bicycle", "skateboard")
```

#### FullTextScore

In this example, we want to obtain the first 10 results where "mountain" and "bicycle" are included, and sorted by order of relevance. That is, documents that have these terms more often should appear higher in the list. 

```sql
SELECT TOP 10 *
FROM c
ORDER BY RANK FullTextScore(c.text, ["bicycle", "mountain"])
```

> [!IMPORTANT]
> FullTextScore can only be used in the `ORDER BY RANK` clause and not projected in the `SELECT` statement or in a `WHERE` clause.

## Related content

- [``FullTextContains`` system function](../nosql/query/fulltextcontains.md)
- [``FullTextContainsAll`` system function](../nosql/query/fulltextcontainsall.md)
- [``FullTextContainsAny`` system function](../nosql/query/fulltextcontainsany.md)
- [``FullTextScore`` system function](../nosql/query/fulltextscore.md)
- [``RRF`` system function](../nosql/query/rrf.md)
- [``ORDER BY RANK`` clause](../nosql/query/order-by-rank.md)

-----------
-----------
-----------
-----------


---
title: Use hybrid search (preview)
titleSuffix: Azure Cosmos DB for NoSQL
description: Overview of hybrid search that combines vector search with full-text search scoring in Azure Cosmos DB for NoSQL.
author: jcodella
ms.author: jacodel
ms.service: azure-cosmos-db
ms.topic: how-to
ms.date: 12/03/2024
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
---

# Hybrid search in Azure Cosmos DB for NoSQL (preview)

Azure Cosmos DB for NoSQL now supports a powerful hybrid search capability that combines Vector Search with Full Text Search scoring (BM25) using the Reciprocal Rank Fusion (RRF) function.

## What is hybrid search?

Hybrid search leverages the strengths of both vector-based and traditional keyword-based search methods to deliver more relevant and accurate search results. Hybrid search is easy to do in Azure Cosmos DB for NoSQL due to the ability to store both metadata and vectors within the same document.

Hybrid search in Azure Cosmos DB for NoSQL integrates two distinct search methodologies:

- **Vector search**: Utilizes machine learning models to understand the semantic meaning of queries and documents. This allows for more nuanced and context-aware search results, especially useful for complex queries where traditional keyword search might fall short.
- **Full text search (BM25)**: A well-established algorithm that scores documents based on the presence and frequency of words and terms. BM25 is particularly effective for straightforward keyword searches, providing a robust baseline for search relevance.

The results from vector search and full text search are then combined using the Reciprocal Rank Fusion (RRF) function. RRF is a rank aggregation method that merges the rankings from multiple search algorithms to produce a single, unified ranking. This ensures that the final search results benefit from the strengths of both search approaches and offers multiple benefits.

- **Enhanced Relevance**: By combining semantic understanding with keyword matching, hybrid search delivers more relevant results for a wide range of queries.
- **Improved Accuracy**: The RRF function ensures that the most pertinent results from both search methods are prioritized.
- **Versatility**: Suitable for various use cases including [Retrieval Augmented Generation (RAG)](rag.md) to improve the responses generated by an LLM grounded on your own data.

## How to use hybrid search

1. Enable the [Vector Search in Azure Cosmos DB for NoSQL feature](../nosql/vector-search.md#enable-the-vector-indexing-and-search-feature).
1. Enable the [Full Text & Hybrid Search for NoSQL preview feature](../gen-ai/full-text-search.md#enable-the-full-text-and-hybrid-search-for-nosql-preview-feature).
1. Create a container with a vector policy, full text policy, vector index, and full text index.
1. Insert your data with text and vector properties.
1. Run hybrid queries against the data.

## Configure policies and indexes for hybrid search

> [!IMPORTANT]
> Currently, vector policies and vector indexes are immutable after creation. To make changes, please create a new collection.

### A sample vector policy

 ```json
{
    "vectorEmbeddings": [
        {
            "path":"/vector",
            "dataType":"float32",
            "distanceFunction":"cosine",
            "dimensions":3
        },

}
```

### A sample full text policy

```json
{
    "defaultLanguage": "en-US",
    "fullTextPaths": [
        {
            "path": "/text",
            "language": "en-US"
        }
    ]
}
```

### A sample indexing policy with both full text and vector indexes

```json
{
    "indexingMode": "consistent",
    "automatic": true,
    "includedPaths": [
        {
            "path": "/*"
        }
    ],
    "excludedPaths": [
        {
            "path": "/\"_etag\"/?"
        },
        {
            "path": "/vector/*"
        }
    ],
    "fullTextIndexes": [
        {
            "path": "/text"
        }
    ],
    "vectorIndexes": [
        {
            "path": "/vector",
            "type": "DiskANN"
        }
    ]
}
```

## Hybrid search queries

Hybrid search queries can be executed by leveraging the [`RRF`](../nosql/query/rrf.md) system function in an `ORDER BY RANK` clause that includes both a `VectorDistance` function and `FullTextScore`. For example, a parameterized query to find the top *k* most relevant results would look like:

```sql
SELECT TOP @k *
FROM c
ORDER BY RANK RRF(VectorDistance(c.vector, @queryVector), FullTextScore(c.content, [@searchTerm1, @searchTerm2, ...]))
```

Suppose you have a document that has vector embeddings stored in each document in the property `c.vector` and text data contained in the property c.text. To get the 10 most relevant documents using Hybrid search, the query can be written as:

```sql
SELECT TOP 10 * 
FROM c
ORDER BY RANK RRF(VectorDistance(c.vector, [1,2,3]), FullTextScore(c.text, ["text", "to", "search", "goes" ,"here])
```

## Related content

- [Vector search](../nosql/vector-search.md)
- [VectorDistance system function](../nosql/query/vectordistance.md)
- [FullTextScore system function](../nosql/query/fulltextscore.md)
- [RRF system function](../nosql/query/rrf.md)
- [ORDER BY RANK clause](../nosql/query/order-by-rank.md)


---------------
---------------
---------------


---
title: Integrations for AI apps
titleSuffix: Azure Cosmos DB
description: Integrate Azure Cosmos DB with AI and large language model (LLM) orchestration packages like Semantic Kernel and LangChain.
author: jcodella
ms.service: azure-cosmos-db
ms.topic: how-to
ms.date: 12/03/2024
ms.author: jacodel
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB vCore
---

# Azure Cosmos DB integrations for AI applications

Azure Cosmos DB seamlessly integrates with leading large language model (LLM) orchestration packages like [Semantic Kernel](https://github.com/microsoft/semantic-kernel) and [LangChain](https://www.langchain.com/), enabling developers to harness the power of advanced AI capabilities within their applications. These orchestration packages can streamline the management and use of LLMs, embedding models, and databases, making it even easier to develop Generative AI applications.

| Integration Tool | Description | Azure Cosmos DB for NoSQL | Azure Cosmos DB for MongoDB (vCore) |
| --- | --- | --- | --- |
| **[Semantic Kernel](https://github.com/microsoft/semantic-kernel)** | An open-source framework by Microsoft that combines AI agents with languages like C#, Python, and Java, enabling seamless orchestration of code and AI models. | [Python Connector](/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-nosql-connector?pivots=programming-language-python) <br> [.NET Connector](/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-nosql-connector?pivots=programming-language-csharp) | [Python Connector](/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-mongodb-connector?pivots=programming-language-python) <br> [.NET Connector](/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-mongodb-connector?pivots=programming-language-csharp) |
| **[LangChain](https://www.langchain.com/)** | A framework that simplifies the creation of applications powered by large language models (LLMs), offering tools for context-aware reasoning applications in Python, JavaScript, and Java. | [Python](https://python.langchain.com/docs/integrations/vectorstores/azure_cosmos_db_no_sql/) <br> [JavaScript](https://js.langchain.com/docs/integrations/vectorstores/azure_cosmosdb_nosql/) <br> [Java](https://docs.langchain4j.dev/integrations/embedding-stores/azure-cosmos-nosql/) | [Python](https://python.langchain.com/docs/integrations/vectorstores/azure_cosmos_db/) <br> [JavaScript](https://js.langchain.com/docs/integrations/vectorstores/azure_cosmosdb_mongodb/) <br> [Java](https://docs.langchain4j.dev/integrations/embedding-stores/azure-cosmos-mongo-vcore/) |
| **[LlamaIndex](https://www.llamaindex.ai/)** | A framework for building context-augmented AI applications that can integrate private or domain-specific data with LLMs for complex workflows. | [Python](https://docs.llamaindex.ai/en/stable/examples/vector_stores/AzureCosmosDBNoSqlDemo/) | [Python](https://docs.llamaindex.ai/en/stable/examples/vector_stores/AzureCosmosDBMongoDBvCoreDemo/) |
| **[CosmosAIGraph](https://aka.ms/cosmosaigraph)** | Uses Azure Cosmos DB to create AI-powered knowledge graphs, enabling robust data models and revealing relationships in semi-structured data. | [Quickstart](https://github.com/AzureCosmosDB/CosmosAIGraph/tree/main/impl/docs#quick-start) | [Quickstart](https://github.com/AzureCosmosDB/CosmosAIGraph/tree/main/impl/docs#quick-start) |

## Related content

- [Azure Cosmos DB Samples Gallery](https://aka.ms/AzureCosmosDB/Gallery)
- [Vector Search with Azure Cosmos DB for NoSQL](vector-search-overview.md)
- [Vector Search with Azure Cosmos DB for MongoDB](../mongodb/vcore/vector-search.md)
- [Tokens](tokens.md)
- [Vector Embeddings](vector-embeddings.md)
- [Retrieval Augmented Generated (RAG)](rag.md)
- [30-day Free Trial without Azure subscription](https://azure.microsoft.com/try/cosmosdb/)
- [90-day Free Trial and up to $6,000 in throughput credits with Azure AI Advantage](../ai-advantage.md)

## Next step

> [!div class="nextstepaction"]
> [Use the Azure Cosmos DB lifetime free tier](../free-tier.md)

------------------
------------------
------------------


---
title: Vector similarity search
titleSuffix: Azure Cosmos DB
description: Overview of the vector similarity search functionality in Azure Cosmos DB's various vector search features.
author: wmwxwa
ms.author: wangwilliam
ms.service: azure-cosmos-db
ms.topic: concept-article
ms.date: 12/03/2024
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB vCore
  - ✅ PostgreSQL
---

# Vector search in Azure Cosmos DB

Vector search is a method that helps you find similar items based on their data characteristics rather than by exact matches on a property field. This technique is useful in applications such as searching for similar text, finding related images, making recommendations, or even detecting anomalies. It works by taking the [vector embeddings](vector-embeddings.md) of your data and query, and then measuring the [distance](distance-functions.md) between the data vectors and your query vector. The data vectors that are closest to your query vector are the ones that are found to be most similar semantically.

## Examples

This [interactive visualization](https://openai.com/index/introducing-text-and-code-embeddings/#_1Vr7cWWEATucFxVXbW465e) shows some examples of closeness and distance between vectors.

## Algorithms

Two major types of vector search algorithms are k-nearest neighbors (kNN) and approximate nearest neighbor (ANN). Between [kNN and ANN](knn-vs-ann.md), the latter offers a balance between accuracy and efficiency, making it better suited for large-scale applications. Some well-known ANN algorithms include Inverted File (IVF), Hierarchical Navigable Small World (HNSW), and the state-of-the-art DiskANN.

Using an integrated vector search feature in a fully featured database ([as opposed to a pure vector database](../vector-database.md#integrated-vector-database-vs-pure-vector-database)) offers an efficient way to store, index, and search high-dimensional vector data directly alongside other application data. This approach removes the necessity of migrating your data to costlier alternative vector databases and provides a seamless integration of your AI-driven applications.

## Related content

- [What is a vector database?](../vector-database.md)
- [Retrieval Augmented Generation (RAG)](rag.md)
- [Vector database in Azure Cosmos DB NoSQL](../nosql/vector-search.md)
- [Vector database in Azure Cosmos DB for MongoDB](../mongodb/vcore/vector-search.md)
- LLM [tokens](tokens.md)
- Vector [embeddings](vector-embeddings.md)
- [Distance functions](distance-functions.md)
- [kNN vs ANN vector search algorithms](knn-vs-ann.md)
- [Multi-tenancy for Vector Search](../nosql/multi-tenancy-vector-search.md)

------------------
------------------
------------------

---
title: Retrieval augmented generation
titleSuffix: Azure Cosmos DB
description: Learn about retrieval augmented generation (RAG) in the context of Azure Cosmos DB for NoSQL's vector search capabilities.
author: TheovanKraay
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.topic: concept-article
ms.date: 12/03/2024
ms.author: thvankra
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB vCore
  - ✅ PostgreSQL
---

# Retrieval Augmented Generation (RAG) in Azure Cosmos DB

Retrieval Augmented Generation (RAG) combines the power of large language models (LLMs) with robust information retrieval systems to create more accurate and contextually relevant responses. Unlike traditional generative models that rely solely on pre-trained data, RAG architectures enhance an LLM's capabilities by integrating real-time information retrieval. This augmentation ensures responses are not only generative but also grounded in the most relevant, up-to-date data available.

Azure Cosmos DB, an operational database that supports vector search, stands out as an excellent platform for implementing RAG. Its ability to handle both operational and analytical workloads in a single database, along with advanced features such as multitenancy and hierarchical partition keys, provides a solid foundation for building sophisticated generative AI applications.

## Key Advantages of Using Azure Cosmos DB

### Unified data storage and retrieval

Azure Cosmos DB enables seamless integration of [vector search](../nosql/vector-search.md) capabilities within a unified database system. This means that your operational data and vectorized data coexist, eliminating the need for separate indexing systems. 

### Real-Time data ingestion and querying

Azure Cosmos DB supports real-time ingestion and querying, making it ideal for AI applications. This is crucial for RAG architectures, where the freshness of data can significantly impact the relevance of generated responses.

### Scalability and global distribution

Designed for large-scale applications, Azure Cosmos DB offers global distribution and [instant autoscale](../../cosmos-db/provision-throughput-autoscale.md). This ensures that your RAG-enabled application can handle high query volumes and deliver consistent performance irrespective of user location.

### High availability and reliability

Azure Cosmos DB offers comprehensive SLAs for throughput, latency, and [availability](/azure/reliability/reliability-cosmos-db-nosql). This reliability ensures that your RAG system is always available to generate responses with minimal downtime.

### Multitenancy with hierarchical partition keys

Azure Cosmos DB supports [multitenancy](../nosql/multi-tenancy-vector-search.md) through various performance and security isolation models, making it easier to manage data for different clients or user groups within the same database. This feature is particularly useful for SaaS applications where separation of tenant data is crucial for security and compliance.

### Comprehensive security features

With built-in features such as end-to-end encryption, role-based access control (RBAC), and virtual network (VNet) integration, Azure Cosmos DB ensures that your data remains secure. These security measures are essential for enterprise-grade RAG applications that handle sensitive information.

## Implementing RAG with Azure Cosmos DB

> [!TIP]
> For RAG samples, visit: [AzureDataRetrievalAugmentedGenerationSamples](https://github.com/microsoft/AzureDataRetrievalAugmentedGenerationSamples)

Here's a streamlined process for building a RAG application with Azure Cosmos DB:

1. **Data Ingestion**: Store your documents, images, and other content types in Azure Cosmos DB. Utilize the database's support for vector search to index and retrieve vectorized content.
1. **Query Execution**: When a user submits a query, Azure Cosmos DB can quickly retrieve the most relevant data using its vector search capabilities.
1. **LLM Integration**: Pass the retrieved data to an LLM (e.g., Azure OpenAI) to generate a response. The well-structured data provided by Cosmos DB enhances the quality of the model's output.
1. **Response Generation**: The LLM processes the data and generates a comprehensive response, which is then delivered to the user.

## Related content

- [What is a vector database?](../vector-database.md)
- [Vector database in Azure Cosmos DB NoSQL](../nosql/vector-search.md)
- [Vector database in Azure Cosmos DB for MongoDB](../mongodb/vcore/vector-search.md)
- LLM [tokens](tokens.md)
- Vector [embeddings](vector-embeddings.md)
- [Distance functions](distance-functions.md)
- [kNN vs ANN vector search algorithms](knn-vs-ann.md)
- [Multitenancy for Vector Search](../nosql/multi-tenancy-vector-search.md)

--------------
--------------
--------------


---
title: Semantic cache for large language models
titleSuffix: Azure Cosmos DB
description: Learn how semantic cache, in Azure Cosmos DB, provides a way for you to re-use past prompts and completions to address similar prompts using vector similarity.
author: markjbrown
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.topic: concept-article
ms.date: 12/03/2024
ms.author: mjbrown
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB vCore
  - ✅ PostgreSQL
---

# Introduction to semantic cache

Large language models (LLM) are amazing with their ability to generate completions, or text responses, based upon user prompts. As with any service, they have a compute cost to them. For an LLM, this is typically expressed in [tokens](tokens.md).

A semantic cache provides a way for you to use prior user prompts and LLM completions to address similar user prompts using vector similarity search. A semantic cache can reduce latency and save costs in your GenAI applications as making calls to LLMs is often the most costly and highest latency service in such applications.

In a scenario where an LLM processes a simple prompt, this computational cost is low. However, LLMs don't retain any context between prompts and completions. It's necessary to send some portion of the chat history to the LLM when sending the latest prompt to be processed to give it context. This mechanism is often referred to as a *context window* and is a sliding window of prompts and completions, between a user and an LLM. This extra text increases the compute cost, or tokens, required to process the request. Additionally, as the amount of text increases, so too does the latency for the generating the completion.

In pattern called Retrieval Augmented Generation or [RAG Pattern](rag.md) for short, data from an external source such as a database, is sent with the user prompt and context window to *augment* or ground the LLM. The data from external sources provides extra information to generate a completion. The size of the payloads for RAG Pattern to an LLM can often get rather large. It's not uncommon to consume thousands of tokens and wait for many seconds to generate a completion. In a world where milliseconds counts, waiting for many seconds is often an unacceptable user experience. The cost can also get expensive at high volumes.

The solution typically used to deal with requests with high computational costs and latency is to employ a cache. This scenario isn't different, however, there are differences in how a semantic cache works, and how it's implemented.

## How a semantic cache works

A traditional cache that uses a string equality match for perform a cache lookup on the cache keys A semantic cache uses a *vector query* on the cache keys, which are stored as vectors. To perform a vector search query for the cache lookup, text is converted into a vector embedding and then used to find vectors that are most *similar* in the cache's keys.

The use of a vector query versus key value look up has some advantages. A traditional cache typically returns just a single result for a cache hit. Because a semantic cache uses a query, it can optionally return multiple results to the user. Providing more items to choose from can help reduce compute costs further. It can potentially keep the cache size smaller by reducing the number of items, which need to get generated by the LLM due to cache misses.

## Similarity score

All vector queries return what is referred to as a *similarity score* that represents how close the vectors are to each other in high dimensional space. Values range from 0 (no similarity) to 1 (exact match).

In a vector query, the similarity score for the returned results represents how similar the words or users intent are to what was passed in the WHERE clause. Because a query can return multiple results, the results can be sorted from the most likely to least likely cache results for a user to choose from.

The similarity score is used as a filter for a vector query to limit the results returned to those items most likely to match the users' intent. In practice, setting the similarity score value for a vector query may require some trial and error. Too high, and the cache quickly fills up with multiple responses for similar questions because of repeated cache misses. Too low, and the cache returns too many irrelevant responses that don't match the user's intent.

## Context window

Large language models don't maintain context between requests. To have a *conversation* with an LLM, you have to maintain a context window, or chat history and pass that to the LLM for each request so it can provide a contextually relevant response. 

For a semantic cache to be effective, it needs to have that context as well. In other words, a semantic cache shouldn't just use the text from individual prompts as keys, it should use some portion of the prompts in the chat history as well. Doing so ensures that what gets returned from the cache is also contextually correct, just as it would be if it were generated by an LLM. If a cache didn't have the context from the chat history, users would get unexpected, and likely unacceptable responses.

Here's a simple mental exercise to explain why this is. If you first ask an LLM, "What is the largest lake in North America?", it responds with "Lake Superior" with some facts and figures, then cache the vectorized user prompt and text from the completion.

If you then ask, "What is the second largest?", the LLM is passed the context window of the previous prompt and completion with the follow-up question and correctly responds, "Lake Huron". Then cache the second prompt, "What is the second largest?" and the generated completion, "Lake Huron".

Later, another user in a different session asks, "What is the largest stadium in North America?", the LLM responds with, "Michigan Stadium" with some facts and figures. If that user then asks, "What is the second largest?", the cache finds an exact match for that prompt and return, "Lake Huron", which is incorrect.

For this reason, a semantic cache should operate within a context window. The context window already provides the information necessary for an LLM to generate relevant completions. This makes it a logical choice for how the cache should work as well. 

Implementing this requires first vectorizing the array of prompts from the context window and the last prompt. The vectors are first then used in the WHERE clause in the vector query on the cache key. What is returned is the completion from the same sequence of questions asked previously by another user. As the context window continuously slides forward in the conversation, any sequence of prompts that have high similarity are returned by the cache versus being regenerated by the LLM.

## Maintenance

As with any cache there's the potential for it to grow to enormous size. Keeping a semantic cache requires the same kind of maintenance. There are multiple ways to maintain its size including using a time-to-live (TTL) for cached items, limit the maximum size, or limit the number of items in the cache.

The use of a similarity score also provides a mechanism for keeping the cache lean. The lower the similarity score in the WHERE clause for the vector query on a cache can increase the number of cache hits, reducing the need to generate and cache completions for similar user prompts. However this comes at a cost of potentially returning less relevant results.

Other possible techniques that use a TTL but also preserve frequent cache hits could include keeping a cache hit score on items and implement a mechanism to prune the cache that are based upon cache hit score. This could be implemented using a patch operation to increment a hit-count property on a cache item. Then override the TTL on that item to preserve it in the cache if it reaches some pre-determined threshold of cache hits.

There may also be other considerations in how to maintain a cache beyond just efficiency. It's widely understood users' chat history can allow developers to tune applications that use an LLM to generate completions. Chat history also provides valuable data on users' interactions with these types of applications and can yield significant insights on their sentiment and behaviors. The same is true for cache usage as well.

It may be desirable to keep the entire contents of a cache and use it for further analysis, yet ensure that users only see the most recent cached entries. In scenarios like this, a possible technique may use an extra filter in the WHERE clause for the vector query on the cache that filters for the most recent cached items or sorts them based upon freshness. For example, `WHERE c.lastUpdated > @someDate ORDER BY c.lastUpdated DESC`

## Implementing a semantic cache with Azure Cosmos DB

There are multiple samples you can use to understand how to build your own semantic cache using Azure Cosmos DB.

- [Build a Copilot app using Azure Cosmos DB for NoSQL](https://github.com/AzureCosmosDB/cosmosdb-nosql-copilot)

This C# sample demonstrates many of the concepts necessary to build your own Copilot applications in Azure using Azure Cosmos DB for NoSQL. This sample also comes with a [Hands-On-Lab](https://github.com/AzureCosmosDB/cosmosdb-nosql-copilot/tree/start?tab=readme-ov-file#hands-on-lab-to-build-a-copilot-app-using-azure-cosmos-db-for-nosql-azure-openai-service-azure-app-service-and-semantic-kernel) that walks users step-by-step through these concepts, including [how to implement a semantic cache](https://github.com/AzureCosmosDB/cosmosdb-nosql-copilot/blob/start/lab/lab-guide.md#exercise--implement-a-semantic-cache).

- [Build a Copilot app using Azure Cosmos DB for MongoDB](https://github.com/AzureCosmosDB/cosmosdb-mongo-copilot)

This C# sample demonstrates many of the concepts necessary to build your own Copilot applications in Azure using Azure Cosmos DB for MongoDB. This sample also comes with a [Hands-On-Lab](https://github.com/AzureCosmosDB/cosmosdb-mongo-copilot/tree/start?tab=readme-ov-file#hands-on-lab-to-build-a-copilot-app-with-azure-cosmos-db-for-mongodb-azure-openai-service-and-semantic-kernel) that walks users step-by-step through these concepts, including [how to implement a semantic cache](https://github.com/AzureCosmosDB/cosmosdb-mongo-copilot/blob/start/docs/LABGuide.md#exercise--implement-a-semantic-cache).

## Related content

- [What is a vector database?](../vector-database.md)
- [Vector database in Azure Cosmos DB NoSQL](../nosql/vector-search.md)
- [Vector database in Azure Cosmos DB for MongoDB](../mongodb/vcore/vector-search.md)
- LLM [tokens](tokens.md)
- Vector [embeddings](vector-embeddings.md)
- [Distance functions](distance-functions.md)
- [kNN vs ANN vector search algorithms](knn-vs-ann.md)
- [Multitenancy for Vector Search](../nosql/multi-tenancy-vector-search.md)


------------------
------------------
------------------


---
title: Multitenancy in Azure Cosmos DB
description: Review critical concepts required to build multitenant generative AI applications in Azure Cosmos DB.
author: TheovanKraay
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.topic: concept-article
ms.date: 12/03/2024
ms.author: thvankra
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB vCore
  - ✅ PostgreSQL
---

# Multitenancy for vector search in Azure Cosmos DB

> "OpenAI relies on Cosmos DB to dynamically scale their ChatGPT service – one of the fastest-growing consumer apps ever – enabling high reliability and low maintenance."
> — Satya Nadella

Azure Cosmos DB stands out as the world's first full-featured serverless operational database with vector search, offering unparalleled scalability and performance. By using Azure Cosmos DB, users can enhance their vector search capabilities, ensuring high reliability and low maintenance for multitenant applications.

Multitenancy enables a single instance of a database to serve multiple customers, or tenants, simultaneously. This approach efficiently shares infrastructure and operational overhead, resulting in cost savings and simplified management. It's a crucial design consideration for SaaS applications and some internal enterprise solutions.

Multitenancy introduces complexity. Your system must scale efficiently to maintain high performance across all tenants, who may have unique workloads, requirements, and service-level agreements (SLAs).

Imagine a fictional AI-assisted research platform called ResearchHub. Serving thousands of companies and individual researchers, ResearchHub manages varying user bases, data scales, and SLAs. Ensuring low query latency and high performance is vital for sustaining an excellent user experience.

Azure Cosmos DB, with its [DiskANN vector index](../index-policy.md#vector-indexes) capability, simplifies multitenant design, providing efficient data storage and access mechanisms for high-performance applications.

## Multi-tenancy models in Azure Cosmos DB

In Azure Cosmos DB, we recommend two primary approaches to managing multi-tenancy: partition key-per-tenant or account-per-tenant, each with its own set of benefits and trade-offs.

### 1. Partition key-per-tenant

For a higher density of tenants and lower isolation, the partition key-per-tenant model is effective. Each tenant is assigned a unique partition key within a given container, allowing logical separation of data. This strategy works best when each tenant has roughly the same workload volume. If there is significant skew, customers should consider isolating those tenants in their own account. Additionally, if a single tenant has more than 20GB of data, [hierarchical partition keys (HPK)](#hierarchical-partitioning-enhanced-data-organization) should be used. For vector search in particular, quantizedFlat index may perform very well if vector search queries can be focused to a particular partition or sets of partitions.

**Benefits:**
- **Cost Efficiency:** Sharing a single Cosmos DB account across multiple tenants reduces overhead.
- **Scalability:** Can manage a large number of tenants, each isolated within their partition key.
- **Simplified Management:** Fewer Cosmos DB accounts to manage.
- **Hierarchical Partition Keys (HPK):** Optimizes data organization and query performance in multitenant apps with a high number of tenants.

**Drawbacks:**
- **Resource Contention:** Shared resources can lead to contention during peak usage.
- **Limited Isolation:** Logical but not physical isolation, which may not meet strict isolation requirements.
- **Less Flexibility:** Reduced flexibility per tenant for enabling account-level features like geo-replication, point-in-time restore (PITR), and customer-managed keys (CMK).

### Hierarchical partitioning: enhanced data organization

[Hierarchical partitioning](../hierarchical-partition-keys.md) builds on the partition key-per-tenant model, adding deeper levels of data organization. This method involves creating multiple levels of partition keys for more granular data management. The lowest level of  hierarchical partitioning should have high cardinality. Typically, it is recommended to use an ID/guid for this level to ensure continuous scalability beyond 20GB per tenant.

**Advantages:**
- **Optimized Queries:** More precise targeting of subpartitions at the parent partition level reduces query latency.
- **Improved Scalability:** Facilitates deeper data segmentation for easier scaling.
- **Better Resource Allocation:** Evenly distributes workloads, minimizing bottlenecks for high tenant counts.

**Considerations:**
- If applications have very few tenants and use hierarchical partitioning, this can lead to bottlenecks since all documents with the same first-level key will write to the same physical partition(s).

**Example:**
ResearchHub can stratify data within each tenant’s partition by organizing it at various levels such as "DepartmentId" and "ResearcherId," facilitating efficient management and queries.

![ResearchHub AI Data Stratification](../media/gen-ai/multi-tenant/hpk.png)

### 2. Account-per-tenant

For maximum isolation, the account-per-tenant model is preferable. Each tenant gets a dedicated Cosmos DB account, ensuring complete separation of resources.

**Benefits:**
- **High Isolation:** No contention or interference due to dedicated resources.
- **Custom SLAs:** Resources and SLAs can be tailored to individual tenant needs.
- **Enhanced Security:** Physical data isolation ensures robust security.
- **Flexibility:** Tenants can enable account-level features like geo-replication, point-in-time restore (PITR), and customer-managed keys (CMK) as needed.

**Drawbacks:**
- **Increased Management:** Higher complexity in managing multiple Cosmos DB accounts.
- **Higher Costs:** More accounts mean higher infrastructure costs.

## Security isolation with customer-managed keys

Azure Cosmos DB enables [customer-managed keys](../how-to-setup-customer-managed-keys.md) for data encryption, adding an extra layer of security for multitenant environments.

**Steps to Implement:**
- **Set Up Azure Key Vault:** Securely store your encryption keys.
- **Link to Cosmos DB:** Associate your Key Vault with your Cosmos DB account.
- **Rotate Keys Regularly:** Enhance security by routinely updating your keys.

Using customer-managed keys ensures each tenant's data is encrypted uniquely, providing robust security and compliance.

![ResearchHub AI Account-per-tenant](../media/gen-ai/multi-tenant/account.png)

## Other isolation models

### Container and database isolation

In addition to the partition key-per-tenant and account-per-tenant models, Azure Cosmos DB provides other isolation methods such as container isolation and database isolation. These approaches offer varying degrees of performance isolation, though they don't provide the same level of security isolation as the account-per-tenant model.

#### Container isolation

In the container isolation model, each tenant is assigned a separate container within a shared Cosmos DB account. This model allows for some level of isolation in terms of performance and resource allocation.

**Benefits:**
- **Better Performance Isolation:** Containers can be allocated specific performance resources, minimizing the impact of one tenant’s workload on another.
- **Easier Management:** Managing multiple containers within a single account is generally easier than managing multiple accounts.
- **Cost Efficiency:** Similar to the partition key-per-tenant model, this method reduces the overhead of multiple accounts.

**Drawbacks:**
- **Limited Security Isolation:** Unlike separate accounts, containers within the same account don't provide physical data isolation. So, this model may not meet stringent security requirements.
- **Resource Contention:** Heavy workloads in one container can still affect others if resource limits are breached.

#### Database isolation

The database isolation model assigns each tenant a separate database within a shared Cosmos DB account. This provides enhanced isolation in terms of resource allocation and management.

**Benefits:**
- **Enhanced Performance:** Separate databases reduce the risk of resource contention, offering better performance isolation.
- **Flexible Resource Allocation:** Resources can be allocated and managed at the database level, providing tailored performance capabilities.
- **Centralized Management:** Easier to manage compared to multiple accounts, yet offering more isolation than container-level separation.

**Drawbacks:**
- **Limited Security Isolation:** Similar to container isolation, having separate databases within a single account does not provide physical data isolation.
- **Complexity:** Managing multiple databases can be more complex than managing containers, especially as the number of tenants grows.

While container and database isolation models don't offer the same level of security isolation as the account-per-tenant model, they can still be useful for achieving performance isolation and flexible resource management. These methods are beneficial for scenarios where cost efficiency and simplified management are priorities, and stringent security isolation is not a critical requirement.

By carefully evaluating the specific needs and constraints of your multitenant application, you can choose the most suitable isolation model in Azure Cosmos DB, balancing performance, security, and cost considerations to achieve the best results for your tenants.

## Real-world implementation considerations

When designing a multitenant system with Cosmos DB, consider these factors:

- **Tenant Workload:** Evaluate data size and activity to select the appropriate isolation model.
- **Performance Requirements:** Align your architecture with defined SLAs and performance metrics.
- **Cost Management:** Balance infrastructure costs against the need for isolation and performance.
- **Scalability:** Plan for growth by choosing scalable models.

### Practical implementation in Azure Cosmos DB

**Partition Key-Per-Tenant:**
- **Assign Partition Keys:** Unique keys for each tenant ensure logical separation.
- **Store Data:** Tenant data is confined to respective partition keys.
- **Optimize Queries:** Use partition keys for efficient, targeted queries.

**Hierarchical Partitioning:**
- **Create Multi-Level Keys:** Further organize data within tenant partitions.
- **Targeted Queries:** Enhance performance with precise sub-partition targeting.
- **Manage Resources:** Distribute workloads evenly to prevent bottlenecks.

**Account-Per-Tenant:**
- **Provide Separate Accounts:** Each tenant gets a dedicated Cosmos DB account.
- **Customize Resources:** Tailor performance and SLAs to tenant requirements.
- **Ensure Security:** Physical data isolation offers robust security and compliance.

## Best practices for using Azure Cosmos DB with vector search

Azure Cosmos DB's support for DiskANN vector index capability makes it an excellent choice for applications that require fast, high-dimensional searches, such as AI-assisted research platforms like ResearchHub. Here’s how you can leverage these capabilities:

**Efficient Storage and Retrieval:**
- **Vector Indexing:** Use the DiskANN vector index to efficiently store and retrieve high-dimensional vectors. This is useful for applications that involve similarity searches in large datasets, such as image recognition or document similarity.
- **Performance Optimization:** DiskANN’s vector search capabilities enable quick, accurate searches, ensuring low latency and high performance, which is critical for maintaining a good user experience.

**Scaling Across Tenants:**
- **Partition Key-Per-Tenant:** Utilize partition keys to logically isolate tenant data while benefiting from Cosmos DB’s scalable infrastructure.
- **Hierarchical Partitioning:** Implement hierarchical partitioning to further segment data within each tenant’s partition, improving query performance and resource distribution.

**Security and Compliance:**
- **Customer-Managed Keys:** Implement customer-managed keys for data encryption at rest, ensuring each tenant’s data is securely isolated.
- **Regular Key Rotation:** Enhance security by regularly rotating encryption keys stored in Azure Key Vault.

### Real-world example: implementing ResearchHub

**Partition Key-Per-Tenant:**
- **Assign Partition Keys:** Each organization (tenant) is assigned a unique partition key.
- **Data Storage:** All researchers’ data for a tenant is stored within its partition, ensuring logical separation.
- **Query Optimization:** Queries are executed using the tenant's partition key, enhancing performance by isolating data access.

**Hierarchical Partitioning:**
- **Multi-Level Partition Keys:** Data within a tenant’s partition is further segmented by "DepartmentId" and "ResearcherId" or other relevant attributes.
- **Granular Data Management:** This hierarchical approach allows ResearchHub to manage and query data more efficiently, reducing latency, and improving response times.

**Account-Per-Tenant:**
- **Separate Cosmos DB Accounts:** High-profile clients or those with sensitive data are provided individual Cosmos DB accounts.
- **Custom Configurations:** Resources and SLAs are tailored to meet the specific needs of each tenant, ensuring optimal performance and security.
- **Enhanced Data Security:** Physical separation of data with customer-managed encryption keys ensures robust security compliance.

## Conclusion

Multi-tenancy in Azure Cosmos DB, especially with its DiskANN vector index capability, offers a powerful solution for building scalable, high-performance AI applications. Whether you choose partition key-per-tenant, hierarchical partitioning, or account-per-tenant models, you can effectively balance cost, security, and performance. By using these models and best practices, you can ensure that your multitenant application meets the diverse needs of your customers, delivering an exceptional user experience.

Azure Cosmos DB provides the tools necessary to build a robust, secure, and scalable multitenant environment. With the power of DiskANN vector indexing, you can deliver fast, high-dimensional searches that drive your AI applications.

## Vector database solutions

[Azure PostgreSQL Server pgvector Extension](../../postgresql/flexible-server/how-to-use-pgvector.md)

:::image type="content" source="../media/vector-search/azure-databases-and-ai-search.png" lightbox="../media/vector-search/azure-databases-and-ai-search.png" alt-text="Diagram of Vector indexing services for Azure PostgreSQL.":::

## Related content

- [30-day Free Trial without Azure subscription](https://azure.microsoft.com/try/cosmosdb/)
- [Multitenancy and Azure Cosmos DB](https://aka.ms/CosmosMultitenancy)

## Next step

> [!div class="nextstepaction"]
> [Use the Azure Cosmos DB lifetime free tier](../free-tier.md)


------------------
------------------
------------------


---
title: Build a RAG Chatbot
titleSuffix: Azure Cosmos DB for NoSQL
description: Build a retrieval augmented generation (RAG) chatbot in Python using Azure Cosmos DB for NoSQL's vector search capabilities.
author: TheovanKraay
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.topic: how-to
ms.date: 12/03/2024
ms.author: thvankra
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
---

# Build a RAG chatbot with Azure Cosmos DB NoSQL API

In this guide, we demonstrate how to build a [RAG Pattern](../gen-ai/rag.md) application using a subset of the Movie Lens dataset. This sample leverages the Python SDK for Azure Cosmos DB for NoSQL to perform vector search for RAG, store and retrieve chat history, and store vectors of the chat history to use as a semantic cache. Azure OpenAI is used to generate embeddings and Large Language Model (LLM) completions.

At the end, we create a simple UX using Gradio to allow users to type in questions and display responses generated by Azure OpenAI or served from the cache. The responses will also display an elapsed time to show the impact caching has on performance versus generating a response.

> [!TIP]
> You can find the full Python notebook sample [here](https://aka.ms/CosmosPythonRAGQuickstart).
>
> For more RAG samples, visit: [AzureDataRetrievalAugmentedGenerationSamples](https://github.com/microsoft/AzureDataRetrievalAugmentedGenerationSamples)

> [!IMPORTANT]
> This sample requires you to set up accounts for Azure Cosmos DB for NoSQL and Azure OpenAI. To get started, visit:
>
> - [Azure Cosmos DB for NoSQL Python Quickstart](../nosql/quickstart-python.md)
> - [Azure Cosmos DB for NoSQL Vector Search](../nosql/vector-search.md)
> - [Azure OpenAI](/azure/ai-services/openai)
>

## 1. Install Required Packages

Install the necessary Python packages to interact with Azure Cosmos DB and other services.

```bash
! pip install --user python-dotenv
! pip install --user aiohttp
! pip install --user openai
! pip install --user gradio
! pip install --user ijson
! pip install --user nest_asyncio
! pip install --user tenacity
# Note: ensure you have azure-cosmos version 4.7 or higher installed
! pip install --user azure-cosmos
```

## 2. Initialize Your Client Connection

Populate `sample_env_file.env` with the appropriate credentials for Azure Cosmos DB and Azure OpenAI.

```env
cosmos_uri = "https://<replace with cosmos db account name>.documents.azure.com:443/"
cosmos_key = "<replace with cosmos db account key>"
cosmos_database_name = "database"
cosmos_collection_name = "vectorstore"
cosmos_vector_property_name = "vector"
cosmos_cache_database_name = "database"
cosmos_cache_collection_name = "vectorcache"
openai_endpoint = "<replace with azure openai endpoint>"
openai_key = "<replace with azure openai key>"
openai_type = "azure"
openai_api_version = "2023-05-15"
openai_embeddings_deployment = "<replace with azure openai embeddings deployment name>"
openai_embeddings_model = "<replace with azure openai embeddings model - e.g. text-embedding-3-large"
openai_embeddings_dimensions = "1536"
openai_completions_deployment = "<replace with azure openai completions deployment name>"
openai_completions_model = "<replace with azure openai completions model - e.g. gpt-35-turbo>"
storage_file_url = "https://cosmosdbcosmicworks.blob.core.windows.net/fabcondata/movielens_dataset.json"
```

```python
# Import the required libraries
import time
import json
import uuid
import urllib 
import ijson
import zipfile
from dotenv import dotenv_values
from openai import AzureOpenAI
from azure.core.exceptions import AzureError
from azure.cosmos import PartitionKey, exceptions
from time import sleep
import gradio as gr

# Cosmos DB imports
from azure.cosmos import CosmosClient

# Load configuration
env_name = "sample_env_file.env"
config = dotenv_values(env_name)

cosmos_conn = config['cosmos_uri']
cosmos_key = config['cosmos_key']
cosmos_database = config['cosmos_database_name']
cosmos_collection = config['cosmos_collection_name']
cosmos_vector_property = config['cosmos_vector_property_name']
comsos_cache_db = config['cosmos_cache_database_name']
cosmos_cache = config['cosmos_cache_collection_name']

# Create the Azure Cosmos DB for NoSQL async client for faster data loading
cosmos_client = CosmosClient(url=cosmos_conn, credential=cosmos_key)

openai_endpoint = config['openai_endpoint']
openai_key = config['openai_key']
openai_api_version = config['openai_api_version']
openai_embeddings_deployment = config['openai_embeddings_deployment']
openai_embeddings_dimensions = int(config['openai_embeddings_dimensions'])
openai_completions_deployment = config['openai_completions_deployment']

# Movies file url
storage_file_url = config['storage_file_url']

# Create the OpenAI client
openai_client = AzureOpenAI(azure_endpoint=openai_endpoint, api_key=openai_key, api_version=openai_api_version)
```

## 3. Create a Database and Containers with Vector Policies

This function takes a database object, a collection name, the name of the document property that stores vectors, and the number of vector dimensions used for the embeddings.

```python
db = cosmos_client.create_database_if_not_exists(cosmos_database)

# Create the vector embedding policy to specify vector details
vector_embedding_policy = {
    "vectorEmbeddings": [ 
        { 
            "path":"/" + cosmos_vector_property,
            "dataType":"float32",
            "distanceFunction":"cosine",
            "dimensions":openai_embeddings_dimensions
        }, 
    ]
}

# Create the vector index policy to specify vector details
indexing_policy = {
    "includedPaths": [ 
    { 
        "path": "/*" 
    } 
    ], 
    "excludedPaths": [ 
    { 
        "path": "/\"_etag\"/?",
        "path": "/" + cosmos_vector_property + "/*",
    } 
    ], 
    "vectorIndexes": [ 
        {
            "path": "/"+cosmos_vector_property, 
            "type": "quantizedFlat" 
        }
    ]
} 

# Create the data collection with vector index (note: this creates a container with 10000 RUs to allow fast data load)
try:
    movies_container = db.create_container_if_not_exists(id=cosmos_collection, 
                                                  partition_key=PartitionKey(path='/id'),
                                                  indexing_policy=indexing_policy, 
                                                  vector_embedding_policy=vector_embedding_policy,
                                                  offer_throughput=10000) 
    print('Container with id \'{0}\' created'.format(movies_container.id)) 

except exceptions.CosmosHttpResponseError: 
    raise 

# Create the cache collection with vector index
try:
    cache_container = db.create_container_if_not_exists(id=cosmos_cache, 
                                                  partition_key=PartitionKey(path='/id'), 
                                                  indexing_policy=indexing_policy,
                                                  vector_embedding_policy=vector_embedding_policy,
                                                  offer_throughput=1000) 
    print('Container with id \'{0}\' created'.format(cache_container.id)) 

except exceptions.CosmosHttpResponseError: 
    raise
```

## 4. Generate Embeddings from Azure OpenAI

This function vectorizes the user input for vector search. Ensure the dimensionality and model used match the sample data provided, or else regenerate vectors with your desired model.

```python
from tenacity import retry, stop_after_attempt, wait_random_exponential 
import logging
@retry(wait=wait_random_exponential(min=2, max=300), stop=stop_after_attempt(20))
def generate_embeddings(text):
    try:        
        response = openai_client.embeddings.create(
            input=text,
            model=openai_embeddings_deployment,
            dimensions=openai_embeddings_dimensions
        )
        embeddings = response.model_dump()
        return embeddings['data'][0]['embedding']
    except Exception as e:
        # Log the exception with traceback for easier debugging
        logging.error("An error occurred while generating embeddings.", exc_info=True)
        raise
```

## 5. Load Data from the JSON File

Extract the prevectorized MovieLens dataset from the zip file (see its location in notebook repo [here](https://github.com/microsoft/AzureDataRetrievalAugmentedGenerationSamples/tree/main/DataSet/Movies)).

```python
# Unzip the data file
with zipfile.ZipFile("../../DataSet/Movies/MovieLens-4489-256D.zip", 'r') as zip_ref:
    zip_ref.extractall("/Data")
zip_ref.close()

# Load the data file
data = []
with open('/Data/MovieLens-4489-256D.json', 'r') as d:
    data = json.load(d)

# View the number of documents in the data (4489)
len(data)
```

## 6. Store Data in Azure Cosmos DB

Upsert data into Azure Cosmos DB for NoSQL. Records are written asynchronously.

```python
#The following code to get raw movies data is commented out in favour of
#getting prevectorized data. If you want to vectorize the raw data from
#storage_file_url, uncomment the below, and set vectorizeFlag=True

#data = urllib.request.urlopen(storage_file_url)
#data = json.load(data)

vectorizeFlag=False

import asyncio
import time
from concurrent.futures import ThreadPoolExecutor

async def generate_vectors(items, vector_property):
    # Create a thread pool executor for the synchronous generate_embeddings
    loop = asyncio.get_event_loop()
    
    # Define a function to call generate_embeddings using run_in_executor
    async def generate_embedding_for_item(item):
        try:
            # Offload the sync generate_embeddings to a thread
            vectorArray = await loop.run_in_executor(None, generate_embeddings, item['overview'])
            item[vector_property] = vectorArray
        except Exception as e:
            # Log or handle exceptions if needed
            logging.error(f"Error generating embedding for item: {item['overview'][:50]}...", exc_info=True)
    
    # Create tasks for all the items to generate embeddings concurrently
    tasks = [generate_embedding_for_item(item) for item in items]
    
    # Run all the tasks concurrently and wait for their completion
    await asyncio.gather(*tasks)
    
    return items

async def insert_data(vectorize=False):
    start_time = time.time()  # Record the start time
    
    # If vectorize flag is True, generate vectors for the data
    if vectorize:
        print("Vectorizing data, please wait...")
        global data
        data = await generate_vectors(data, "vector")

    counter = 0
    tasks = []
    max_concurrency = 5  # Adjust this value to control the level of concurrency
    semaphore = asyncio.Semaphore(max_concurrency)
    print("Starting doc load, please wait...")
    
    def upsert_item_sync(obj):
        movies_container.upsert_item(body=obj)
    
    async def upsert_object(obj):
        nonlocal counter
        async with semaphore:
            await asyncio.get_event_loop().run_in_executor(None, upsert_item_sync, obj)
            # Progress reporting
            counter += 1
            if counter % 100 == 0:
                print(f"Sent {counter} documents for insertion into collection.")
    
    for obj in data:
        tasks.append(asyncio.create_task(upsert_object(obj)))
    
    # Run all upsert tasks concurrently within the limits set by the semaphore
    await asyncio.gather(*tasks)
    
    end_time = time.time()  # Record the end time
    duration = end_time - start_time  # Calculate the duration
    print(f"All {counter} documents inserted!")
    print(f"Time taken: {duration:.2f} seconds ({duration:.3f} milliseconds)")

# Run the async function with the vectorize flag set to True or False as needed
await insert_data(vectorizeFlag)  # or await insert_data() for default
```

## 7. Perform Vector Search

This function defines a vector search over the movies data and chat cache collections.

```python
def vector_search(container, vectors, similarity_score=0.02, num_results=5):
    results = container.query_items(
        query='''
        SELECT TOP @num_results c.overview, VectorDistance(c.vector, @embedding) as SimilarityScore 
        FROM c
        WHERE VectorDistance(c.vector,@embedding) > @similarity_score
        ORDER BY VectorDistance(c.vector,@embedding)
        ''',
        parameters=[
            {"name": "@embedding", "value": vectors},
            {"name": "@num_results", "value": num_results},
            {"name": "@similarity_score", "value": similarity_score}
        ],
        enable_cross_partition_query=True,
        populate_query_metrics=True
    )
    results = list(results)
    formatted_results = [{'SimilarityScore': result.pop('SimilarityScore'), 'document': result} for result in results]

    return formatted_results
```

## 8. Get Recent Chat History

This function provides conversational context to the LLM, allowing it to better have a conversation with the user.

```python
def get_chat_history(container, completions=3):
    results = container.query_items(
        query='''
        SELECT TOP @completions *
        FROM c
        ORDER BY c._ts DESC
        ''',
        parameters=[
            {"name": "@completions", "value": completions},
        ],
        enable_cross_partition_query=True
    )
    results = list(results)
    return results
```

## 9. Chat Completion Functions

Define the functions to handle the chat completion process, including caching responses.

```python
def generate_completion(user_prompt, vector_search_results, chat_history):
    system_prompt = '''
    You are an intelligent assistant for movies. You are designed to provide helpful answers to user questions about movies in your database.
    You are friendly, helpful, and informative and can be lighthearted. Be concise in your responses, but still friendly.
     - Only answer questions related to the information provided below. Provide at least 3 candidate movie answers in a list.
     - Write two lines of whitespace between each answer in the list.
    '''

    messages = [{'role': 'system', 'content': system_prompt}]
    for chat in chat_history:
        messages.append({'role': 'user', 'content': chat['prompt'] + " " + chat['completion']})
    messages.append({'role': 'user', 'content': user_prompt})
    for result in vector_search_results:
        messages.append({'role': 'system', 'content': json.dumps(result['document'])})

    response = openai_client.chat.completions.create(
        model=openai_completions_deployment,
        messages=messages,
        temperature=0.1
    )    
    return response.model_dump()

def chat_completion(cache_container, movies_container, user_input):
    print("starting completion")
    # Generate embeddings from the user input
    user_embeddings = generate_embeddings(user_input)
    # Query the chat history cache first to see if this question has been asked before
    cache_results = get_cache(container=cache_container, vectors=user_embeddings, similarity_score=0.99, num_results=1)
    if len(cache_results) > 0:
        print("Cached Result\n")
        return cache_results[0]['completion'], True
        
    else:
        # Perform vector search on the movie collection
        print("New result\n")
        search_results = vector_search(movies_container, user_embeddings)

        print("Getting Chat History\n")
        # Chat history
        chat_history = get_chat_history(cache_container, 3)
        # Generate the completion
        print("Generating completions \n")
        completions_results = generate_completion(user_input, search_results, chat_history)

        print("Caching response \n")
        # Cache the response
        cache_response(cache_container, user_input, user_embeddings, completions_results)

        print("\n")
        # Return the generated LLM completion
        return completions_results['choices'][0]['message']['content'], False
```

## 10. Cache Generated Responses

Save the user prompts and generated completions to the cache for faster future responses.

```python
def cache_response(container, user_prompt, prompt_vectors, response):
    chat_document = {
        'id': str(uuid.uuid4()),
        'prompt': user_prompt,
        'completion': response['choices'][0]['message']['content'],
        'completionTokens': str(response['usage']['completion_tokens']),
        'promptTokens': str(response['usage']['prompt_tokens']),
        'totalTokens': str(response['usage']['total_tokens']),
        'model': response['model'],
        'vector': prompt_vectors
    }
    container.create_item(body=chat_document)

def get_cache(container, vectors, similarity_score=0.0, num_results=5):
    # Execute the query
    results = container.query_items(
        query= '''
        SELECT TOP @num_results *
        FROM c
        WHERE VectorDistance(c.vector,@embedding) > @similarity_score
        ORDER BY VectorDistance(c.vector,@embedding)
        ''',
        parameters=[
            {"name": "@embedding", "value": vectors},
            {"name": "@num_results", "value": num_results},
            {"name": "@similarity_score", "value": similarity_score},
        ],
        enable_cross_partition_query=True, populate_query_metrics=True)
    results = list(results)
    return results
```

## 11. Create a Simple UX in Gradio

Build a user interface using Gradio for interacting with the AI application.

```python
chat_history = []

with gr.Blocks() as demo:
    chatbot = gr.Chatbot(label="Cosmic Movie Assistant")
    msg = gr.Textbox(label="Ask me about movies in the Cosmic Movie Database!")
    clear = gr.Button("Clear")

    def user(user_message, chat_history):
        start_time = time.time()
        response_payload, cached = chat_completion(cache_container, movies_container, user_message)
        end_time = time.time()
        elapsed_time = round((end
        time - start_time) * 1000, 2)
        details = f"\n (Time: {elapsed_time}ms)"
        if cached:
            details += " (Cached)"
        chat_history.append([user_message, response_payload + details])
        
        return gr.update(value=""), chat_history
    
    msg.submit(user, [msg, chatbot], [msg, chatbot], queue=False)
    clear.click(lambda: None, None, chatbot, queue=False)

# Launch the Gradio interface
demo.launch(debug=True)

# Be sure to run this cell to close or restart the Gradio demo
demo.close()
```

## Vector Database Solutions

[Azure PostgreSQL Server pgvector Extension](../../postgresql/flexible-server/how-to-use-pgvector.md)

## Related content

- [30-day Free Trial without Azure subscription](https://azure.microsoft.com/try/cosmosdb/)
- [90-day Free Trial and up to $6,000 in throughput credits with Azure AI Advantage](../ai-advantage.md)

## Next step

> [!div class="nextstepaction"]
> [Use the Azure Cosmos DB lifetime free tier](../free-tier.md)

---------------------
---------------------
---------------------
---------------------

---
title: Ingest and vectorize document files
titleSuffix: Azure Cosmos DB for NoSQL
description: Ingest document files into Azure Cosmos DB for NoSQL for use in vector indexing and similarity search solutions.
author: jcodella
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.custom:
  - ignite-2024
ms.topic: how-to
ms.date: 12/03/2024
ms.author: jacodel
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
---

# Load and process document files into Azure Cosmos DB for search

> [!NOTE]
> Document ingestion for Azure Cosmos DB is in private preview. If you're interested to join the preview, we encourage you to join the wait list by signing this form: https://aka.ms/Doc2CDBSignup

We introduce Doc2CDB for Azure Cosmos DB, a powerful accelerator designed to streamline the extraction, preprocessing, and management of large volumes of text data for vector similarity search. This solution uses the advanced vector indexing capabilities of Azure Cosmos DB and is powered by Azure AI Services to provide a robust and efficient pipeline that’s easily to set up and perfect for many use cases including:

- Vector Similarity Search over Text Data. Extract and vectorize text from document data to store in Azure Cosmos DB, makes it easy for you to perform semantic search to find documents that are contextually related to your queries. This allows them to discover relevant information that might not be found through traditional keyword searches, facilitating more comprehensive data retrieval.

- Retrieval-Augmented Generation (RAG) over Documents. Personalize your Small and Large Language Models to your data with RAG. By extracting text from document files, chunking and vectorizing the data, then storing it in Azure Cosmos DB, you’re then set up to empower the chatbot to generate more accurate and contextually relevant responses to your scenarios. When you ask a question, the chatbot retrieves the most relevant text chunks through vector search and uses them to generate an answer, grounded in your document data.

:::image type="content" source="../media/gen-ai/document-ingestion/document-ingestion-pipeline.png" alt-text="Diagram of the Cosmos AI Graph infrastructure, components, and flow.":::

## The end-to-end pipeline

Doc2CDB includes several key stages in its pipeline:
1. File Upload to Azure Blob Storage
   - The process begins with uploading documents to Azure Blob Storage. This stage ensures that your files are securely stored and easily accessible for further processing. This is compatible with PDFs, Microsoft Office documents (DOCX, XLSX, PPTX, HTML), and Images (JPEG, PNG, BMP, TIFF, HEIF).
2. Text Extraction
   - Once the files are uploaded, the next step is text extraction. This involves parsing text data and performing OCR on documents using Azure Document Intelligence, to extract text that can be processed and indexed in Azure Cosmos DB. This stage is crucial for preparing the data for subsequent processing.
3. Text Chunking
   - After extraction, the raw text is broken down into manageable chunks. This chunking process is essential for enabling Small and Large Language Models (SLMs/LLMs) in Azure AI to process the text efficiently. By dividing the text into smaller pieces, we ensure that the data is more accessible and easier to handle.
4. Text Embedding
   - In this stage, Azure OpenAI Service’s text-3-embedding-large model is used to produce vector embeddings of the text chunks. These embeddings capture the semantic meaning of the text, allowing for more sophisticated and accurate searches. The embeddings are a critical component for enabling advanced search capabilities.
5. Text Storage
   - Finally, each text chunk, along with its corresponding vector embedding, is stored in an Azure Cosmos DB for NoSQL container as a unique document. This container is configured to perform efficient vector searches and, eventually, full-text searches. By using Azure Cosmos DB’s powerful vector indexing and search capabilities, users can quickly and easily retrieve relevant information from their text data.

## Benefits of the Doc2CDB solution accelerator

- Scalability: Handle large volumes of text data with ease, thanks to the scalable nature of Azure AI services and Azure Cosmos DB
- Efficiency: Streamline the text processing pipeline, reducing the time and effort required to manage and search text data. This is preconfigured for you
- Advanced Search Capabilities: Utilize ultra-fast and efficient Vector Indexing in Azure Cosmos DB perform vector search to find the most semantically relevant data from your documents

## Get started

The Doc2CDB accelerator designed to help you parse, process, and store your document data more easily to take advantage of Azure Cosmos DB’s rich query language and powerful Vector Similarity Search.  Visit https://aka.ms/Doc2CDB and give it a try today!

## Related content

- [Vector Search with Azure Cosmos DB for NoSQL](vector-search-overview.md)
- [Tokens](tokens.md)
- [Vector Embeddings](vector-embeddings.md)
- [Retrieval Augmented Generated (RAG)](rag.md)
- [30-day Free Trial without Azure subscription](https://azure.microsoft.com/try/cosmosdb/)
- [90-day Free Trial and up to $6,000 in throughput credits with Azure AI Advantage](../ai-advantage.md)

## Next step

> [!div class="nextstepaction"]
> [Use the Azure Cosmos DB lifetime free tier](../free-tier.md)

---------------------
---------------------
---------------------
---------------------


---
title: AI knowledge graphs
description: Create AI knowledge graphs using Azure Cosmos DB for NoSQL to allow AI apps to manage and query complex data relationships.
author: jcodella
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.topic: how-to
ms.date: 12/03/2024
ms.author: jacodel
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
---

# Retrieval Augmented Generated (RAG) with vector search and knowledge graphs using Azure Cosmos DB

[CosmosAIGraph](https://aka.ms/cosmosaigraph) is an innovative solution that applies the power of Azure Cosmos DB to create AI-powered knowledge graphs. This technology integrates advanced graph database capabilities with AI to provide a robust platform for managing and querying complex data relationships. By utilizing Cosmos DB's scalability and performance in both document and vector form, Cosmos AI Graph enables the creation of sophisticated data models that can answer various data questions and uncover hidden relationships and concepts in semi-structured data.

## Questions knowledge graphs help to answer

- **Complex Relationship Queries**:
  - *Question*: "What are the direct and indirect connections between Person A and Person B within a social network?"
  - *Explanation*: Graph RAG can traverse the graph to find all paths and relationships between two nodes, providing a detailed map of connections, which is difficult for Vector Search as it doesn't have authoritative/curated view of relationships between entities.

- **Hierarchical Data Queries**:
  - *Question*: "What is the organizational hierarchy from the CEO down to the entry-level employees in this company?"
  - *Explanation*: Graph RAG can efficiently navigate hierarchical structures, identifying parent-child relationships and levels within the hierarchy, whereas Vector Search is more suited for finding similar items rather than understanding hierarchical relationships.

- **Contextual Path Queries**:
  - *Question*: "What are the steps involved in the supply chain from raw material procurement to the final product delivery?"
  - *Explanation*: Graph RAG can follow the specific paths and dependencies within a supply chain graph, providing a step-by-step breakdown. Vector Search, while excellent at finding similar items, lacks the capability to follow and understand the sequence of steps in a process.

When it comes to [Retrieval Augmented Generation (RAG)](rag.md), combining **Knowledge Graphs** and **Vector Search** can offer powerful capabilities that expand the range of questions that can be answered about the data. Graph RAG enhances the retrieval process by using the structured relationships within a graph, making it ideal for applications that require contextual understanding and complex querying, such as knowledge management systems and personalized content delivery. On the other hand, Vector Search excels in handling unstructured data and finding similarities based on vector embeddings, which is useful for tasks like image and document retrieval. Together, these technologies can provide a comprehensive solution that combines the strengths of both structured and unstructured data processing.

:::image type="content" source="../media/gen-ai/cosmos-ai-graph/cosmos-ai-graph-architecture.png" alt-text="Diagram of the Cosmos AI Graph infrastructure, components, and flow.":::

## OmniRAG

CosmosAIGraph features *OmniRAG*, a versatile approach to data retrieval that dynamically selects the most suitable method — be it database queries, vector matching, or knowledge graph traversal — to answer user queries effectively and with utmost accuracy, as it likely will gather more context and more authoritative conext than any one of these sources could on its own. The key to this dynamic selection is the user intent - determined from the user question using simple utterance analysis and/or AI. This ensures that each query is addressed using the optimal technique, enhancing accuracy and efficiency. For instance, a user query about hierarchical relationships would utilize graph traversal, while a query about similar documents would employ vector search, all within a unified framework provided by CosmosAIGraph. Moreover, with the help of orchestration of within RAG process, more than one source could be used to collect the context for AI, for example the graph could be consulted with first and then for each of the entities found the actual database records could be pulled as well and if no results were found, vector search would likely return closely matching results. This holistic approach maximizes the strengths of each retrieval method, delivering comprehensive and contextually relevant answers.

### Example user questions and strategy used

| User Questions | Strategy |
| --- | --- |
| What is the Python Flask Library | DB RAG |
| What are its dependencies | Graph Rag |
| What is the Python Flask Library | Database RAG |
| What are its dependencies | Graph RAG |
| Who is the author | DB RAG |
| What other libraries did she write | Graph RAG |
| Display a graph of all her libraries and their dependencies | Graph RAG |

## Get started

CosmosAIGraph applies Azure Cosmos DB to create AI-powered graphs and knowledge graphs, enabling sophisticated data models for applications like recommendation systems and fraud detection. It combines traditional database, vector database, and graph database capabilities with AI to manage and query complex data relationships efficiently. Get started [here!](https://aka.ms/cosmosaigraph)

## Related content

- [CosmosAIGraph on Azure Cosmos DB TV - YouTube](https://www.youtube.com/watch?v=0alvRmEgIpQ)
- [Vector Search with Azure Cosmos DB for NoSQL](vector-search-overview.md)
- [Tokens](tokens.md)
- [Vector Embeddings](vector-embeddings.md)
- [Retrieval Augmented Generated (RAG)](rag.md)
- [30-day Free Trial without Azure subscription](https://azure.microsoft.com/try/cosmosdb/)
- [90-day Free Trial and up to $6,000 in throughput credits with Azure AI Advantage](../ai-advantage.md)

## Next step

> [!div class="nextstepaction"]
> [Use the Azure Cosmos DB lifetime free tier](../free-tier.md)

---------------------
---------------------
---------------------
---------------------

---
title: AI agents and solutions
titleSuffix: Azure Cosmos DB
description: Learn about key concepts for agents and step through the implementation of an AI agent memory system.
author: wmwxwa
ms.author: wangwilliam
ms.service: azure-cosmos-db
ms.custom:
  - ignite-2024
ms.topic: concept-article
ms.date: 12/03/2024
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL ✅ MongoDB vCore
---

# AI agents in Azure Cosmos DB

AI agents are designed to perform specific tasks, answer questions, and automate processes for users. These agents vary widely in complexity. They range from simple chatbots, to copilots, to advanced AI assistants in the form of digital or robotic systems that can run complex workflows autonomously.

This article provides conceptual overviews and detailed implementation samples for AI agents.

## What are AI agents?

Unlike standalone large language models (LLMs) or rule-based software/hardware systems, AI agents have these common features:

- **Planning**: AI agents can plan and sequence actions to achieve specific goals. The integration of LLMs has revolutionized their planning capabilities.
- **Tool usage**: Advanced AI agents can use various tools, such as code execution, search, and computation capabilities, to perform tasks effectively. AI agents often use tools through function calling.
- **Perception**: AI agents can perceive and process information from their environment, to make them more interactive and context aware. This information includes visual, auditory, and other sensory data.
- **Memory**: AI agents have the ability to remember past interactions (tool usage and perception) and behaviors (tool usage and planning). They store these experiences and even perform self-reflection to inform future actions. This memory component allows for continuity and improvement in agent performance over time.

> [!NOTE]
> The usage of the term *memory* in the context of AI agents is different from the concept of computer memory (like volatile, nonvolatile, and persistent memory).

### Copilots

Copilots are a type of AI agent. They work alongside users rather than operating independently. Unlike fully automated agents, copilots provide suggestions and recommendations to assist users in completing tasks.

For instance, when a user is writing an email, a copilot might suggest phrases, sentences, or paragraphs. The user might also ask the copilot to find relevant information in other emails or files to support the suggestion (see [retrieval-augmented generation](vector-database.md#retrieval-augmented-generation)). The user can accept, reject, or edit the suggested passages.

### Autonomous agents

Autonomous agents can operate more independently. When you set up autonomous agents to assist with email composition, you could enable them to perform the following tasks:

- Consult existing emails, chats, files, and other internal and public information that's related to the subject matter.
- Perform qualitative or quantitative analysis on the collected information, and draw conclusions that are relevant to the email.
- Write the complete email based on the conclusions and incorporate supporting evidence.
- Attach relevant files to the email.
- Review the email to ensure that all the incorporated information is factually accurate and that the assertions are valid.
- Select the appropriate recipients for **To**, **Cc**, and **Bcc**, and look up their email addresses.
- Schedule an appropriate time to send the email.
- Perform follow-ups if responses are expected but not received.

You can configure the agents to perform each of the preceding tasks with or without human approval.

### Multi-agent systems

A popular strategy for achieving performant autonomous agents is the use of multi-agent systems. In multi-agent systems, multiple autonomous agents, whether in digital or robotic form, interact or work together to achieve individual or collective goals. Agents in the system can operate independently and possess their own knowledge or information. Each agent might also have the capability to perceive its environment, make decisions, and execute actions based on its objectives.

Multi-agent systems have these key characteristics:

- **Autonomous**: Each agent functions independently. It makes its own decisions without direct human intervention or control by other agents.
- **Interactive**: Agents communicate and collaborate with each other to share information, negotiate, and coordinate their actions. This interaction can occur through various protocols and communication channels.
- **Goal-oriented**: Agents in a multi-agent system are designed to achieve specific goals, which can be aligned with individual objectives or a shared objective among the agents.
- **Distributed**: Multi-agent systems operate in a distributed manner, with no single point of control. This distribution enhances the system's robustness, scalability, and resource efficiency.

A multi-agent system provides the following advantages over a copilot or a single instance of LLM inference:

- **Dynamic reasoning**: Compared to chain-of-thought or tree-of-thought prompting, multi-agent systems allow for dynamic navigation through various reasoning paths.
- **Sophisticated abilities**: Multi-agent systems can handle complex or large-scale problems by conducting thorough decision-making processes and distributing tasks among multiple agents.
- **Enhanced memory**: Multi-agent systems with memory can overcome the context windows of LLMs to enable better understanding and information retention.

## Implementation of AI agents

### Reasoning and planning

Complex reasoning and planning are the hallmark of advanced autonomous agents. Popular frameworks for autonomous agents incorporate one or more of the following methodologies (with links to arXiv archive pages) for reasoning and planning:

- [Self-Ask](https://arxiv.org/abs/2210.03350)

  Improve on chain of thought by having the model explicitly ask itself (and answer) follow-up questions before answering the initial question.

- [Reason and Act (ReAct)](https://arxiv.org/abs/2210.03629)

  Use LLMs to generate both reasoning traces and task-specific actions in an interleaved manner. Reasoning traces help the model induce, track, and update action plans, along with handling exceptions. Actions allow the model to connect with external sources, such as knowledge bases or environments, to gather additional information.

- [Plan and Solve](https://arxiv.org/abs/2305.04091)

  Devise a plan to divide the entire task into smaller subtasks, and then carry out the subtasks according to the plan. This approach mitigates the calculation errors, missing-step errors, and semantic misunderstanding errors that are often present in zero-shot chain-of-thought prompting.

- [Reflect/Self-critique](https://arxiv.org/abs/2303.11366)

  Use *reflexion* agents that verbally reflect on task feedback signals. These agents maintain their own reflective text in an episodic memory buffer to induce better decision-making in subsequent trials.

### Frameworks

Various frameworks and tools can facilitate the development and deployment of AI agents.

For tool usage and perception that don't require sophisticated planning and memory, some popular LLM orchestrator frameworks are LangChain, LlamaIndex, Prompt Flow, and Semantic Kernel.

For advanced and autonomous planning and execution workflows, [AutoGen](https://microsoft.github.io/autogen/) propelled the multi-agent wave that began in late 2022. OpenAI's [Assistants API](https://platform.openai.com/docs/assistants/overview) allows its users to create agents natively within the GPT ecosystem. [LangChain Agents](https://python.langchain.com/v0.1/docs/modules/agents/) and [LlamaIndex Agents](https://docs.llamaindex.ai/en/stable/use_cases/agents/) also emerged around the same time.

> [!TIP]
> The [implementation sample](#implementation-sample) later in this article shows how to build a simple multi-agent system by using one of the popular frameworks and a unified agent memory system.

### AI agent memory system

The prevalent practice for experimenting with AI-enhanced applications from 2022 through 2024 has been using standalone database management systems for various data workflows or types. For example, you can use an in-memory database for caching, a relational database for operational data (including tracing/activity logs and LLM conversation history), and a [pure vector database](vector-database.md#integrated-vector-database-vs-pure-vector-database) for embedding management.

However, this practice of using a complex web of standalone databases can hurt an AI agent's performance. Integrating all these disparate databases into a cohesive, interoperable, and resilient memory system for AI agents is its own challenge.

Also, many of the frequently used database services are not optimal for the speed and scalability that AI agent systems need. These databases' individual weaknesses are exacerbated in multi-agent systems.

#### In-memory databases

In-memory databases are excellent for speed but might struggle with the large-scale data persistence that AI agents need.

#### Relational databases

Relational databases are not ideal for the varied modalities and fluid schemas of data that agents handle. Relational databases require manual efforts and even downtime to manage provisioning, partitioning, and sharding.

#### Pure vector databases

Pure vector databases tend to be less effective for transactional operations, real-time updates, and distributed workloads. The popular pure vector databases nowadays typically offer:

- No guarantee on reads and writes.
- Limited ingestion throughput.
- Low availability (below 99.9%, or an annualized outage of 9 hours or more).
- One consistency level (eventual).
- A resource-intensive in-memory vector index.
- Limited options for multitenancy.
- Limited security.

## Characteristics of a robust AI agent memory system

Just as efficient database management systems are critical to the performance of software applications, it's critical to provide LLM-powered agents with relevant and useful information to guide their inference. Robust memory systems enable organizing and storing various kinds of information that the agents can retrieve at inference time.

Currently, LLM-powered applications often use [retrieval-augmented generation](vector-database.md#retrieval-augmented-generation) that uses basic semantic search or vector search to retrieve passages or documents. [Vector search](vector-database.md#vector-search) can be useful for finding general information. But vector search might not capture the specific context, structure, or relationships that are relevant for a particular task or domain.

For example, if the task is to write code, vector search might not be able to retrieve the syntax tree, file system layout, code summaries, or API signatures that are important for generating coherent and correct code. Similarly, if the task is to work with tabular data, vector search might not be able to retrieve the schema, the foreign keys, the stored procedures, or the reports that are useful for querying or analyzing the data.

Weaving together a web of standalone in-memory, relational, and vector databases (as described [earlier](#ai-agent-memory-system)) is not an optimal solution for the varied data types. This approach might work for prototypical agent systems. However, it adds complexity and performance bottlenecks that can hamper the performance of advanced autonomous agents.

A robust memory system should have the following characteristics.

### Multimodal

AI agent memory systems should provide collections that store metadata, relationships, entities, summaries, or other types of information that can be useful for various tasks and domains. These collections can be based on the structure and format of the data, such as documents, tables, or code. Or they can be based on the content and meaning of the data, such as concepts, associations, or procedural steps.

Memory systems aren't just critical to AI agents. They're also important for the humans who develop, maintain, and use these agents.

For example, humans might need to supervise agents' planning and execution workflows in near real time. While supervising, humans might interject with guidance or make in-line edits of agents' dialogues or monologues. Humans might also need to audit the reasoning and actions of agents to verify the validity of the final output.

Human/agent interactions are likely in natural or programming languages, whereas agents "think," "learn," and "remember" through embeddings. This difference poses another requirement on memory systems' consistency across data modalities.

### Operational

Memory systems should provide memory banks that store information that's relevant for the interaction with the user and the environment. Such information might include chat history, user preferences, sensory data, decisions made, facts learned, or other operational data that's updated with high frequency and at high volumes.

These memory banks can help the agents remember short-term and long-term information, avoid repeating or contradicting themselves, and maintain task coherence. These requirements must hold true even if the agents perform a multitude of unrelated tasks in succession. In advanced cases, agents might also test numerous branch plans that diverge or converge at different points.

### Sharable but also separable

At the macro level, memory systems should enable multiple AI agents to collaborate on a problem or process different aspects of the problem by providing shared memory that's accessible to all the agents. Shared memory can facilitate the exchange of information and the coordination of actions among the agents.

At the same time, the memory system must allow agents to preserve their own persona and characteristics, such as their unique collections of prompts and memories.

## Building a robust AI agent memory system

The preceding characteristics require AI agent memory systems to be highly scalable and swift. Painstakingly weaving together disparate in-memory, relational, and vector databases (as described [earlier](#ai-agent-memory-system)) might work for early-stage AI-enabled applications. However, this approach adds complexity and performance bottlenecks that can hamper the performance of advanced autonomous agents.

In place of all the standalone databases, Azure Cosmos DB can serve as a unified solution for AI agent memory systems. Its robustness successfully [enabled OpenAI's ChatGPT service](https://www.youtube.com/watch?v=6IIUtEFKJec&t) to scale dynamically with high reliability and low maintenance. Powered by an atom-record-sequence engine, it's the world's first globally distributed [NoSQL](distributed-nosql.md), [relational](distributed-relational.md), and [vector database](vector-database.md) service that offers a serverless mode. AI agents built on top of Azure Cosmos DB offer speed, scale, and simplicity.

### Speed

Azure Cosmos DB provides single-digit millisecond latency. This capability makes it suitable for processes that require rapid data access and management. These processes include caching (both traditional and semantic caching, transactions, and operational workloads.

Low latency is crucial for AI agents that need to perform complex reasoning, make real-time decisions, and provide immediate responses. In addition, the service's [use of the DiskANN algorithm](nosql/vector-search.md#enable-the-vector-indexing-and-search-feature) provides accurate and fast vector search with minimal memory consumption.

### Scale

Azure Cosmos DB is engineered for global distribution and horizontal scalability. It offers support for multiple-region I/O and multitenancy.

The service helps ensure that memory systems can expand seamlessly and keep up with rapidly growing agents and associated data. The [availability guarantee in its service-level agreement (SLA)](https://www.microsoft.com/licensing/docs/view/Service-Level-Agreements-SLA-for-Online-Services) translates to less than 5 minutes of downtime per year. Pure vector database services, by contrast, come with 9 hours or more of downtime. This availability provides a solid foundation for mission-critical workloads. At the same time, the various service models in Azure Cosmos DB, like [Reserved Capacity](reserved-capacity.md) or Serverless, can help reduce financial costs.

### Simplicity

Azure Cosmos DB can simplify data management and architecture by integrating multiple database functionalities into a single, cohesive platform.

Its integrated vector database capabilities can store, index, and query embeddings alongside the corresponding data in natural or programming languages. This capability enables greater data consistency, scale, and performance.

Its flexibility supports the varied modalities and fluid schemas of the metadata, relationships, entities, summaries, chat history, user preferences, sensory data, decisions, facts learned, or other operational data involved in agent workflows. The database automatically indexes all data without requiring schema or index management, which helps AI agents perform complex queries quickly and efficiently.

Azure Cosmos DB is fully managed, which eliminates the overhead of database administration tasks like scaling, patching, and backups. Without this overhead, developers can focus on building and optimizing AI agents without worrying about the underlying data infrastructure.

### Advanced features

Azure Cosmos DB incorporates advanced features such as change feed, which allows tracking and responding to changes in data in real time. This capability is useful for AI agents that need to react to new information promptly.

Additionally, the built-in support for multi-master writes enables high availability and resilience to help ensure continuous operation of AI agents, even after regional failures.

The five available [consistency levels](consistency-levels.md) (from strong to eventual) can also cater to various distributed workloads, depending on the scenario requirements.

> [!TIP]
> You can choose from two Azure Cosmos DB APIs to build your AI agent memory system:
>
> - Azure Cosmos DB for NoSQL, which offers 99.999% availability guarantee and provides [three vector search algorithms](nosql/vector-search.md): IVF, HNSW, and DiskANN
> - vCore-based Azure Cosmos DB for MongoDB, which offers 99.995% availability guarantee and provides [two vector search algorithms](mongodb/vcore/vector-search.md): IVF and HNSW (DiskANN is upcoming)
>
> For information about the availability guarantees for these APIs, see the [service SLAs](https://www.microsoft.com/licensing/docs/view/Service-Level-Agreements-SLA-for-Online-Services).

## Implementation sample

This section explores the implementation of an autonomous agent to process traveler inquiries and bookings in a travel application for a cruise line.

Chatbots are a long-standing concept, but AI agents are advancing beyond basic human conversation to carry out tasks based on natural language. These tasks traditionally required coded logic. The AI travel agent in this implementation sample uses the LangChain Agent framework for agent planning, tool usage, and perception.

The AI travel agent's [unified memory system](#characteristics-of-a-robust-ai-agent-memory-system) uses the [vector database](vector-database.md) and document store capabilities of Azure Cosmos DB to address traveler inquiries and facilitate trip bookings. Using Azure Cosmos DB for this purpose helps ensure speed, scale, and simplicity, as described [earlier](#building-a-robust-ai-agent-memory-system).

The sample agent operates within a Python FastAPI back end. It supports user interactions through a React JavaScript user interface.

### Prerequisites

- An Azure subscription. If you don't have one, you can [try Azure Cosmos DB for free](try-free.md) for 30 days without creating an Azure account. The free trial doesn't require a credit card, and no commitment follows the trial period.
- An account for the OpenAI API or Azure OpenAI Service.
- A vCore cluster in Azure Cosmos DB for MongoDB. You can create one by following [this quickstart](mongodb/vcore/quickstart-portal.md).
- An integrated development environment, such as Visual Studio Code.
- Python 3.11.4 installed in the development environment.

### Download the project

All of the code and sample datasets are available in [this GitHub repository](https://github.com/jonathanscholtes/Travel-AI-Agent-React-FastAPI-and-Cosmos-DB-Vector-Store). The repository includes these folders:

- *loader*: This folder contains Python code for loading sample documents and vector embeddings in Azure Cosmos DB.
- *api*: This folder contains the Python FastAPI project for hosting the AI travel agent.
- *web*: This folder contains code for the React web interface.

### Load travel documents into Azure Cosmos DB

The GitHub repository contains a Python project in the *loader* directory. It's intended for loading the sample travel documents into Azure Cosmos DB.

#### Set up the environment

Set up your Python virtual environment in the *loader* directory by running the following command:

```shell
python -m venv venv
```

Activate your environment and install dependencies in the *loader* directory:

```shell
venv\Scripts\activate
python -m pip install -r requirements.txt
```

Create a file named *.env* in the *loader* directory, to store the following environment variables:

```env
OPENAI_API_KEY="<your OpenAI key>"
MONGO_CONNECTION_STRING="mongodb+srv:<your connection string from Azure Cosmos DB>"
```

#### Load documents and vectors

The Python file *main.py* serves as the central entry point for loading data into Azure Cosmos DB. This code processes the sample travel data from the GitHub repository, including information about ships and destinations. The code also generates travel itinerary packages for each ship and destination, so that travelers can book them by using the AI agent. The CosmosDBLoader tool is responsible for creating collections, vector embeddings, and indexes in the Azure Cosmos DB instance.

Here are the contents of *main.py*:

```python
from cosmosdbloader import CosmosDBLoader
from itinerarybuilder import ItineraryBuilder
import json

cosmosdb_loader = CosmosDBLoader(DB_Name='travel')

#read in ship data
with open('documents/ships.json') as file:
        ship_json = json.load(file)

#read in destination data
with open('documents/destinations.json') as file:
        destinations_json = json.load(file)

builder = ItineraryBuilder(ship_json['ships'],destinations_json['destinations'])

# Create five itinerary packages
itinerary = builder.build(5)

# Save itinerary packages to Cosmos DB
cosmosdb_loader.load_data(itinerary,'itinerary')

# Save destinations to Cosmos DB
cosmosdb_loader.load_data(destinations_json['destinations'],'destinations')

# Save ships to Cosmos DB, create vector store
collection = cosmosdb_loader.load_vectors(ship_json['ships'],'ships')

# Add text search index to ship name
collection.create_index([('name', 'text')])
```

Load the documents, load the vectors, and create indexes by running the following command from the *loader* directory:

```shell
python main.py
```

Here's the output of *main.py*:

```output
--build itinerary--
--load itinerary--
--load destinations--
--load vectors ships--
```

### Build the AI travel agent by using Python FastAPI

The AI travel agent is hosted in a back end API through Python FastAPI, which facilitates integration with the front-end user interface. The API project processes agent requests by grounding the LLM prompts against the data layer, specifically the vectors and documents in Azure Cosmos DB.

The agent makes use of various tools, particularly the Python functions provided at the API service layer. This article focuses on the code necessary for AI agents within the API code.

The API project in the GitHub repository is structured as follows:

- *Data modeling components* use Pydantic models.
- *Web layer components* are responsible for routing requests and managing communication.
- *Service layer components* are responsible for primary business logic and interaction with the data layer, the LangChain Agent, and agent tools.
- *Data layer components* are responsible for interacting with Azure Cosmos DB for MongoDB document storage and vector search.

### Set up the environment for the API

We used Python version 3.11.4 for the development and testing of the API.

Set up your Python virtual environment in the *api* directory:

```shell
python -m venv venv
```

Activate your environment and install dependencies by using the *requirements* file in the *api* directory:

```shell
venv\Scripts\activate
python -m pip install -r requirements.txt
```

Create a file named *.env* in the *api* directory, to store your environment variables:

```env
OPENAI_API_KEY="<your Open AI key>"
MONGO_CONNECTION_STRING="mongodb+srv:<your connection string from Azure Cosmos DB>"
```

Now that you've configured the environment and set up variables, run the following command from the *api* directory to initiate the server:

```shell
python app.py
```

The FastAPI server starts on the localhost loopback 127.0.0.1 port 8000 by default. You can access the Swagger documents by using the following localhost address: `http://127.0.0.1:8000/docs`.

### Use a session for the AI agent memory

It's imperative for the travel agent to be able to reference previously provided information within the ongoing conversation. This ability is commonly known as *memory* in the context of LLMs.

To achieve this objective, use the chat message history that's stored in the Azure Cosmos DB instance. The history for each chat session is stored through a session ID to ensure that only messages from the current conversation session are accessible. This necessity is the reason behind the existence of a `Get Session` method in the API. It's a placeholder method for managing web sessions to illustrate the use of chat message history.

Select **Try it out** for `/session/`.

:::image type="content" source="media/gen-ai/ai-agent/fastapi-get-session.png" lightbox="media/gen-ai/ai-agent/fastapi-get-session.png" alt-text="Screenshot of the use of the Get Session method in Python FastAPI, with the button for trying it out.":::

```json
{
  "session_id": "0505a645526f4d68a3603ef01efaab19"
}
```

For the AI agent, you only need to simulate a session. The stubbed-out method merely returns a generated session ID for tracking message history. In a practical implementation, this session would be stored in Azure Cosmos DB and potentially in React `localStorage`.

Here are the contents of *web/session.py*:

```python
@router.get("/")
def get_session():
    return {'session_id':str(uuid.uuid4().hex)}
```

### Start a conversation with the AI travel agent

Use the session ID that you obtained from the previous step to start a new dialogue with the AI agent, so you can validate its functionality. Conduct the test by submitting the following phrase: "I want to take a relaxing vacation."

Select **Try it out** for `/agent/agent_chat`.

:::image type="content" source="media/gen-ai/ai-agent/fastapi-agent-chat.png" lightbox="media/gen-ai/ai-agent/fastapi-agent-chat.png" alt-text="Screenshot of the use of the Agent Chat method in Python FastAPI, with the button for trying it out.":::

Use this example parameter:

```json
{
  "input": "I want to take a relaxing vacation.",
  "session_id": "0505a645526f4d68a3603ef01efaab19"
}
```

The initial execution results in a recommendation for the Tranquil Breeze Cruise and the Fantasy Seas Adventure Cruise, because the agent anticipates that they're the most relaxing cruises available through the vector search. These documents have the highest score for `similarity_search_with_score` called in the data layer of the API, `data.mongodb.travel.similarity_search()`.

The similarity search scores appear as output from the API for debugging purposes. Here's the output after a call to `data.mongodb.travel.similarity_search()`:

```output
0.8394561085977978
0.8086545112328692
2
```

> [!TIP]
> If documents are not being returned for vector search, modify the `similarity_search_with_score` limit or the score filter value as needed (`[doc for doc, score  in docs if score >=.78]`) in `data.mongodb.travel.similarity_search()`.

Calling `agent_chat` for the first time creates a new collection named `history` in Azure Cosmos DB to store the conversation by session. This call enables the agent to access the stored chat message history as needed. Subsequent executions of `agent_chat` with the same parameters produce varying results, because it draws from memory.

### Walk through the AI agent

When you're integrating the AI agent into the API, the web search components are responsible for initiating all requests. The web search components are followed by the search service, and finally the data components.

In this specific case, you use a MongoDB data search that connects to Azure Cosmos DB. The layers facilitate the exchange of model components, with the AI agent and the AI agent tool code residing in the service layer. This approach enables the seamless interchangeability of data sources. It also extends the capabilities of the AI agent with additional, more intricate functionalities or tools.

:::image type="content" source="media/gen-ai/ai-agent/travel-ai-agent-fastapi-layers.png" lightbox="media/gen-ai/ai-agent/travel-ai-agent-fastapi-layers.png" alt-text="Diagram of the FastAPI layers of the AI travel agent.":::

#### Service layer

The service layer forms the cornerstone of core business logic. In this particular scenario, the service layer plays a crucial role as the repository for the LangChain Agent code. It facilitates the seamless integration of user prompts with Azure Cosmos DB data, conversation memory, and agent functions for the AI agent.

The service layer employs a singleton pattern module for handling agent-related initializations in the *init.py* file. Here are the contents of *service/init.py*:

```python
from dotenv import load_dotenv
from os import environ
from langchain.globals import set_llm_cache
from langchain_openai import ChatOpenAI
from langchain_mongodb.chat_message_histories import MongoDBChatMessageHistory
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.runnables.history import RunnableWithMessageHistory
from langchain.agents import AgentExecutor, create_openai_tools_agent
from service import TravelAgentTools as agent_tools

load_dotenv(override=False)

chat : ChatOpenAI | None=None
agent_with_chat_history : RunnableWithMessageHistory | None=None

def LLM_init():
    global chat,agent_with_chat_history
    chat = ChatOpenAI(model_name="gpt-3.5-turbo-16k",temperature=0)
    tools = [agent_tools.vacation_lookup, agent_tools.itinerary_lookup, agent_tools.book_cruise ]

    prompt = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            "You are a helpful and friendly travel assistant for a cruise company. Answer travel questions to the best of your ability providing only relevant information. In order to book a cruise you will need to capture the person's name.",
        ),
        MessagesPlaceholder(variable_name="chat_history"),
        ("user", "Answer should be embedded in html tags. {input}"),
         MessagesPlaceholder(variable_name="agent_scratchpad"),
    ]
    )

    #Answer should be embedded in HTML tags. Only answer questions related to cruise travel, If you can not answer respond with \"I am here to assist with your travel questions.\". 


    agent = create_openai_tools_agent(chat, tools, prompt)
    agent_executor  = AgentExecutor(agent=agent, tools=tools, verbose=True)

    agent_with_chat_history = RunnableWithMessageHistory(
        agent_executor,
        lambda session_id: MongoDBChatMessageHistory( database_name="travel",
                                                 collection_name="history",
                                                   connection_string=environ.get("MONGO_CONNECTION_STRING"),
                                                   session_id=session_id),
        input_messages_key="input",
        history_messages_key="chat_history",
)

LLM_init()
```

The *init.py* file initiates the loading of environment variables from an *.env* file by using the `load_dotenv(override=False)` method. Then, a global variable named `agent_with_chat_history` is instantiated for the agent. This agent is intended for use by *TravelAgent.py*.

The `LLM_init()` method is invoked during module initialization to configure the AI agent for conversation via the API web layer. The OpenAI `chat` object is instantiated through the GPT-3.5 model and incorporates specific parameters such as model name and temperature. The `chat` object, tools list, and prompt template are combined to generate `AgentExecutor`, which operates as the AI travel agent.

The agent with history, `agent_with_chat_history`, is established through `RunnableWithMessageHistory` with chat history (`MongoDBChatMessageHistory`). This action enables it to maintain a complete conversation history via Azure Cosmos DB.

#### Prompt

The LLM prompt initially began with the simple statement "You are a helpful and friendly travel assistant for a cruise company." However, testing showed that you could obtain more consistent results by including the instruction "Answer travel questions to the best of your ability, providing only relevant information. To book a cruise, capturing the person's name is essential." The results appear in HTML format to enhance the visual appeal of the web interface.

#### Agent tools

[Tools](#what-are-ai-agents) are interfaces that an agent can use to interact with the world, often through function calling.

When you're creating an agent, you must furnish it with a set of tools that it can use. The `@tool` decorator offers the most straightforward approach to defining a custom tool.

By default, the decorator uses the function name as the tool name, although you can replace it by providing a string as the first argument. The decorator uses the function's docstring as the tool's description, so it requires the provisioning of a docstring.

Here are the contents of *service/TravelAgentTools.py*:

```python
from langchain_core.tools import tool
from langchain.docstore.document import Document
from data.mongodb import travel
from model.travel import Ship


@tool
def vacation_lookup(input:str) -> list[Document]:
    """find information on vacations and trips"""
    ships: list[Ship] = travel.similarity_search(input)
    content = ""

    for ship in ships:
        content += f" Cruise ship {ship.name}  description: {ship.description} with amenities {'/n-'.join(ship.amenities)} "

    return content

@tool
def itinerary_lookup(ship_name:str) -> str:
    """find ship itinerary, cruise packages and destinations by ship name"""
    it = travel.itnerary_search(ship_name)
    results = ""

    for i in it:
        results += f" Cruise Package {i.Name} room prices: {'/n-'.join(i.Rooms)} schedule: {'/n-'.join(i.Schedule)}"

    return results


@tool
def book_cruise(package_name:str, passenger_name:str, room: str )-> str:
    """book cruise using package name and passenger name and room """
    print(f"Package: {package_name} passenger: {passenger_name} room: {room}")

    # LLM defaults empty name to John Doe 
    if passenger_name == "John Doe":
        return "In order to book a cruise I need to know your name."
    else:
        if room == '':
            return "which room would you like to book"            
        return "Cruise has been booked, ref number is 343242"
```

The *TravelAgentTools.py* file defines three tools:

- `vacation_lookup` conducts a vector search against Azure Cosmos DB. It uses `similarity_search` to retrieve relevant travel-related material.
- `itinerary_lookup` retrieves cruise package details and schedules for a specified cruise ship.
- `book_cruise` books a cruise package for a passenger.

Specific instructions ("In order to book a cruise I need to know your name") might be necessary to ensure the capture of the passenger's name and room number for booking the cruise package, even though you included such instructions in the LLM prompt.

#### AI agent

The fundamental concept that underlies agents is to use a language model for selecting a sequence of actions to execute.

Here are the contents of *service/TravelAgent.py*:

```python
from .init import agent_with_chat_history
from model.prompt import PromptResponse
import time
from dotenv import load_dotenv

load_dotenv(override=False)


def agent_chat(input:str, session_id:str)->str:

    start_time = time.time()

    results=agent_with_chat_history.invoke(
    {"input": input},
    config={"configurable": {"session_id": session_id}},
    )

    return  PromptResponse(text=results["output"],ResponseSeconds=(time.time() - start_time))
```

The *TravelAgent.py* file is straightforward, because `agent_with_chat_history` and its dependencies (tools, prompt, and LLM) are initialized and configured in the *init.py* file. This file calls the agent by using the input received from the user, along with the session ID for conversation memory. Afterward, `PromptResponse` (model/prompt) is returned with the agent's output and response time.

## AI agent integration with the React user interface

With the successful loading of the data and accessibility of the AI agent through the API, you can now complete the solution by establishing a web user interface (by using React) for your travel website. Using the capabilities of React helps illustrate the seamless integration of the AI agent into a travel site. This integration enhances the user experience with a conversational travel assistant for inquiries and bookings.

### Set up the environment for React

Install Node.js and the dependencies before testing the React interface.

Run the following command from the *web* directory to perform a clean installation of project dependencies. The installation might take some time.

```shell
npm ci
```

Next, create a file named *.env* within the *web* directory to facilitate the storage of environment variables. Include the following details in the newly created *.env* file:

`REACT_APP_API_HOST=http://127.0.0.1:8000`

Now, run the following command from the *web* directory to initiate the React web user interface:

```shell
npm start
```

Running the previous command opens the React web application.

### Walk through the React web interface

The web project of the GitHub repository is a straightforward application to facilitate user interaction with the AI agent. The primary components required to converse with the agent are *TravelAgent.js* and *ChatLayout.js*. The *Main.js* file serves as the central module or user landing page.

:::image type="content" source="media/gen-ai/ai-agent/main.png" lightbox="media/gen-ai/ai-agent/main.png" alt-text="Screenshot of the React JavaScript web interface.":::

#### Main

The main component serves as the central manager of the application. It acts as the designated entry point for routing. Within the render function, it produces JSX code to delineate the main page layout. This layout encompasses placeholder elements for the application, such as logos and links, a section that houses the travel agent component, and a footer that contains a sample disclaimer about the application's nature.

Here are the contents of *main.js*:

```javascript
import React, {  Component } from 'react'
import { Stack, Link, Paper } from '@mui/material'
import TravelAgent from './TripPlanning/TravelAgent'

import './Main.css'

class Main extends Component {
  constructor() {
    super()

  }

  render() {
    return (
      <div className="Main">
        <div className="Main-Header">
          <Stack direction="row" spacing={5}>
            <img src="/mainlogo.png" alt="Logo" height={'120px'} />
            <Link
              href="#"
              sx={{ color: 'white', fontWeight: 'bold', fontSize: 18 }}
              underline="hover"
            >
              Ships
            </Link>
            <Link
              href="#"
              sx={{ color: 'white', fontWeight: 'bold', fontSize: 18 }}
              underline="hover"
            >
              Destinations
            </Link>
          </Stack>
        </div>
        <div className="Main-Body">
          <div className="Main-Content">
            <Paper elevation={3} sx={{p:1}} >
            <Stack
              direction="row"
              justifyContent="space-evenly"
              alignItems="center"
              spacing={2}
            >
              
                <Link href="#">
                  <img
                    src={require('./images/destinations.png')} width={'400px'} />
                </Link>
                <TravelAgent ></TravelAgent>
                <Link href="#">
                  <img
                    src={require('./images/ships.png')} width={'400px'} />
                </Link>
              
              </Stack>
              </Paper>
          </div>
        </div>
        <div className="Main-Footer">
          <b>Disclaimer: Sample Application</b>
          <br />
          Please note that this sample application is provided for demonstration
          purposes only and should not be used in production environments
          without proper validation and testing.
        </div>
      </div>
    )
  }
}

export default Main
```

#### Travel agent

The travel agent component has a straightforward purpose: capturing user inputs and displaying responses. It plays a key role in managing the integration with the back-end AI agent, primarily by capturing sessions and forwarding user prompts to the FastAPI service. The resulting responses are stored in an array for display, facilitated by the chat layout component.

Here are the contents of *TripPlanning/TravelAgent.js*:

```javascript
import React, { useState, useEffect } from 'react'
import { Button, Box, Link, Stack, TextField } from '@mui/material'
import SendIcon from '@mui/icons-material/Send'
import { Dialog, DialogContent } from '@mui/material'
import ChatLayout from './ChatLayout'
import './TravelAgent.css'

export default function TravelAgent() {
  const [open, setOpen] = React.useState(false)
  const [session, setSession] = useState('')
  const [chatPrompt, setChatPrompt] = useState(
    'I want to take a relaxing vacation.',
  )
  const [message, setMessage] = useState([
    {
      message: 'Hello, how can I assist you today?',
      direction: 'left',
      bg: '#E7FAEC',
    },
  ])

  const handlePrompt = (prompt) => {
    setChatPrompt('')
    setMessage((message) => [
      ...message,
      { message: prompt, direction: 'right', bg: '#E7F4FA' },
    ])
    console.log(session)
    fetch(process.env.REACT_APP_API_HOST + '/agent/agent_chat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ input: prompt, session_id: session }),
    })
      .then((response) => response.json())
      .then((res) => {
        setMessage((message) => [
          ...message,
          { message: res.text, direction: 'left', bg: '#E7FAEC' },
        ])
      })
  }

  const handleSession = () => {
    fetch(process.env.REACT_APP_API_HOST + '/session/')
      .then((response) => response.json())
      .then((res) => {
        setSession(res.session_id)
      })
  }

  const handleClickOpen = () => {
    setOpen(true)
  }

  const handleClose = (value) => {
    setOpen(false)
  }

  useEffect(() => {
    if (session === '') handleSession()
  }, [])

  return (
    <Box>
      <Dialog onClose={handleClose} open={open} maxWidth="md" fullWidth="true">
        <DialogContent>
          <Stack>
            <Box sx={{ height: '500px' }}>
              <div className="AgentArea">
                <ChatLayout messages={message} />
              </div>
            </Box>
            <Stack direction="row" spacing={0}>
              <TextField
                sx={{ width: '80%' }}
                variant="outlined"
                label="Message"
                helperText="Chat with AI Travel Agent"
                defaultValue="I want to take a relaxing vacation."
                value={chatPrompt}
                onChange={(event) => setChatPrompt(event.target.value)}
              ></TextField>
              <Button
                variant="contained"
                endIcon={<SendIcon />}
                sx={{ mb: 3, ml: 3, mt: 1 }}
                onClick={(event) => handlePrompt(chatPrompt)}
              >
                Submit
              </Button>
            </Stack>
          </Stack>
        </DialogContent>
      </Dialog>
      <Link href="#" onClick={() => handleClickOpen()}>
        <img src={require('.././images/planvoyage.png')} width={'400px'} />
      </Link>
    </Box>
  )
}
```

Select **Effortlessly plan your voyage** to open the travel assistant.

#### Chat layout

The chat layout component oversees the arrangement of the chat. It systematically processes the chat messages and implements the formatting specified in the `message` JSON object.

Here are the contents of *TripPlanning/ChatLayout.js*:

```javascript
import React from 'react'
import {  Box, Stack } from '@mui/material'
import parse from 'html-react-parser'
import './ChatLayout.css'

export default function ChatLayout(messages) {
  return (
    <Stack direction="column" spacing="1">
      {messages.messages.map((obj, i = 0) => (
        <div className="bubbleContainer" key={i}>
          <Box
            key={i++}
            className="bubble"
            sx={{ float: obj.direction, fontSize: '10pt', background: obj.bg }}
          >
            <div>{parse(obj.message)}</div>
          </Box>
        </div>
      ))}
    </Stack>
  )
}
```

User prompts are on the right side and colored blue. Responses from the AI travel agent are on the left side and colored green. As the following image shows, the HTML-formatted responses are accounted for in the conversation.

:::image type="content" source="media/gen-ai/ai-agent/chat-screenshot.png" lightbox="media/gen-ai/ai-agent/chat-screenshot.png" alt-text="Screenshot of a chat with an AI agent.":::

When your AI agent is ready to go into production, you can use semantic caching to improve query performance by 80% and to reduce LLM inference and API call costs. To implement semantic caching, see [this post on the Stochastic Coder blog](https://stochasticcoder.com/2024/03/22/improve-llm-performance-using-semantic-cache-with-cosmos-db/).

:::image type="content" source="media/gen-ai/ai-agent/semantic-caching.png" lightbox="media/gen-ai/ai-agent/semantic-caching.png" alt-text="Diagram of semantic caching for AI agents.":::

## Related content

- [30-day free trial without an Azure subscription](https://azure.microsoft.com/try/cosmosdb/)
- [90-day free trial and up to $6,000 in throughput credits with Azure AI Advantage](ai-advantage.md)
- [Azure Cosmos DB lifetime free tier](free-tier.md)

-------------
-------------
-------------
-------------

---
title: AI-enhanced advertisement generation
titleSuffix: Azure Cosmos DB for MongoDB vCore
description: Demonstrates the use of Azure Cosmos DB for MongoDB vCore's vector similarity search and OpenAI embeddings to generate advertising content.
author: khelanmodi
ms.author: khelanmodi
ms.service: azure-cosmos-db
ms.subservice: mongodb-vcore
ms.topic: concept-article
ms.date: 12/03/2024
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ MongoDB vCore
---

# AI-Enhanced advertisement generation using Azure Cosmos DB for MongoDB vCore

In this guide, we demonstrate how to create dynamic advertising content that resonates with your audience, using our personalized AI assistant, Heelie. Utilizing Azure Cosmos DB for MongoDB vCore, we harness the [vector similarity search](./vector-search.md) functionality to semantically analyze and match inventory descriptions with advertisement topics. The process is made possible by generating vectors for inventory descriptions using OpenAI embeddings, which significantly enhance their semantic depth. These vectors are then stored and indexed within the Cosmos DB for MongoDB vCore resource. When generating content for advertisements, we vectorize the advertisement topic to find the best-matching inventory items. This is followed by a retrieval augmented generation (RAG) process, where the top matches are sent to OpenAI to craft a compelling advertisement. The entire codebase for the application is available in a [GitHub repository](https://aka.ms/adgen) for your reference.

## Features

- **Vector Similarity Search**: Uses Azure Cosmos DB for MongoDB vCore's powerful vector similarity search to improve semantic search capabilities, making it easier to find relevant inventory items based on the content of advertisements.
- **OpenAI Embeddings**: Utilizes the cutting-edge embeddings from OpenAI to generate vectors for inventory descriptions. This approach allows for more nuanced and semantically rich matches between the inventory and the advertisement content.
- **Content Generation**: Employs OpenAI's advanced language models to generate engaging, trend-focused advertisements. This method ensures that the content is not only relevant but also captivating to the target audience.

## Prerequisites

- Azure OpenAI: Let's setup the Azure OpenAI resource. Access to this service is currently available by application only. You can apply for access to Azure OpenAI by completing the form at https://aka.ms/oai/access. Once you have access, complete the following steps:
  - Create an Azure OpenAI resource following this [quickstart](/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).
  - Deploy a `completions` and an `embeddings` model.
    - For more information on `completions`, go [here](/azure/ai-services/openai/how-to/completions).
    - For more information on `embeddings`, go [here](/azure/ai-services/openai/how-to/embeddings).
  - Note down your endpoint, key, and deployment names.
- Cosmos DB for MongoDB vCore resource: Let's start by creating an Azure Cosmos DB for MongoDB vCore resource for free following this [quick start](./quickstart-portal.md) guide.
  - Note down the connection details.
- Python environment (>= 3.9 version) with packages such as `numpy`, `openai`, `pymongo`, `python-dotenv`, `azure-core`, `azure-cosmos`, `tenacity`, and `gradio`.
- Download the [data file](https://github.com/jayanta-mondal/ignite-demo/blob/main/data/shoes_with_vectors.json) and save it in a designated data folder.

## Running the Script

Before we dive into the exciting part of generating AI-enhanced advertisements, we need to set up our environment. This setup involves installing the necessary packages to ensure our script runs smoothly. Here’s a step-by-step guide to get everything ready.

### 1.1 Install Necessary Packages

Firstly, we need to install a few Python packages. Open your terminal and run the following commands:

```bash
 pip install numpy
 pip install openai==1.2.3
 pip install pymongo
 pip install python-dotenv
 pip install azure-core
 pip install azure-cosmos
 pip install tenacity
 pip install gradio
 pip show openai
```

### 1.2 Setting Up the OpenAI and Azure Client

After installing the necessary packages, the next step involves setting up our OpenAI and Azure clients for the script, which is crucial for authenticating our requests to the OpenAI API and Azure services.

```python
import json
import time
import openai

from dotenv import dotenv_values
from openai import AzureOpenAI

# Configure the API to use Azure as the provider
openai.api_type = "azure"
openai.api_key = "<AZURE_OPENAI_API_KEY>"  # Replace with your actual Azure OpenAI API key
openai.api_base = "https://<OPENAI_ACCOUNT_NAME>.openai.azure.com/"  # Replace with your OpenAI account name
openai.api_version = "2023-06-01-preview"

# Initialize the AzureOpenAI client with your API key, version, and endpoint
client = AzureOpenAI(
    api_key=openai.api_key,
    api_version=openai.api_version,
    azure_endpoint=openai.api_base
)
```

## Solution architecture

![solution architecture](./media/tutorial-adgen/architecture.png)

## 2. Creating Embeddings and Setting up Cosmos DB

After setting up our environment and OpenAI client, we move to the core part of our AI-enhanced advertisement generation project. The following code creates vector embeddings from text descriptions of products and sets up our database in Azure Cosmos DB for MongoDB vCore to store and search these embeddings.

### 2.1 Create Embeddings

To generate compelling advertisements, we first need to understand the items in our inventory. We do this by creating vector embeddings from descriptions of our items, which allows us to capture their semantic meaning in a form that machines can understand and process. Here's how you can create vector embeddings for an item description using Azure OpenAI:

```python
import openai

def generate_embeddings(text):
    try:
        response = client.embeddings.create(
            input=text, model="text-embedding-ada-002")
        embeddings = response.data[0].embedding
        return embeddings
    except Exception as e:
        print(f"An error occurred: {e}")
        return None

embeddings = generate_embeddings("Shoes for San Francisco summer")

if embeddings is not None:
    print(embeddings)
```

The function takes a text input — like a product description — and uses the `client.embeddings.create` method from the OpenAI API to generate a vector embedding for that text. We're using the `text-embedding-ada-002` model here, but you can choose other models based on your requirements. If the process is successful, it prints the generated embeddings; otherwise, it handles exceptions by printing an error message.

## 3. Connect and set up Cosmos DB for MongoDB vCore

With our embeddings ready, the next step is to store and index them in a database that supports vector similarity search. Azure Cosmos DB for MongoDB vCore is a perfect fit for this task because it's purpose built to store your transactional data and perform vector search all in one place. 

### 3.1 Set up the connection

To connect to Cosmos DB, we use the pymongo library, which allows us to interact with MongoDB easily. The following code snippet establishes a connection with our Cosmos DB for MongoDB vCore instance:

```python
import pymongo

# Replace <USERNAME>, <PASSWORD>, and <VCORE_CLUSTER_NAME> with your actual credentials and cluster name
mongo_conn = "mongodb+srv://<USERNAME>:<PASSWORD>@<VCORE_CLUSTER_NAME>.mongocluster.cosmos.azure.com/?tls=true&authMechanism=SCRAM-SHA-256&retrywrites=false&maxIdleTimeMS=120000"
mongo_client = pymongo.MongoClient(mongo_conn)
```

Replace `<USERNAME>`, `<PASSWORD>`, and `<VCORE_CLUSTER_NAME>` with your actual MongoDB username, password, and vCore cluster name, respectively.

## 4. Setting Up the Database and Vector Index in Cosmos DB

Once you've established a connection to Azure Cosmos DB, the next steps involve setting up your database and collection, and then creating a vector index to enable efficient vector similarity searches. Let's walk through these steps.

### 4.1 Set Up the Database and Collection

First, we create a database and a collection within our Cosmos DB instance. Here’s how:

```python
DATABASE_NAME = "AdgenDatabase"
COLLECTION_NAME = "AdgenCollection"

mongo_client.drop_database(DATABASE_NAME)
db = mongo_client[DATABASE_NAME]
collection = db[COLLECTION_NAME]

if COLLECTION_NAME not in db.list_collection_names():
    # Creates a unsharded collection that uses the DBs shared throughput
    db.create_collection(COLLECTION_NAME)
    print("Created collection '{}'.\n".format(COLLECTION_NAME))
else:
    print("Using collection: '{}'.\n".format(COLLECTION_NAME))
```

### 4.2 Create the vector index

To perform efficient vector similarity searches within our collection, we need to create a vector index. Cosmos DB supports different types of [vector indexes](./vector-search.md), and here we discuss two: IVF and HNSW.

### IVF

IVF stands for Inverted File Index, is the default vector indexing algorithm, which works on all cluster tiers. It's an approximate nearest neighbors (ANN) approach that uses clustering to speeding up the search for similar vectors in a dataset. To create an IVF index, use the following command:

```javascript
db.runCommand({
  'createIndexes': 'COLLECTION_NAME',
  'indexes': [
    {
      'name': 'vectorSearchIndex',
      'key': {
        "contentVector": "cosmosSearch"
      },
      'cosmosSearchOptions': {
        'kind': 'vector-ivf',
        'numLists': 1,
        'similarity': 'COS',
        'dimensions': 1536
      }
    }
  ]
});
```

> [!IMPORTANT]
> **You can only create one index per vector property.** That is, you cannot create more than one index that points to the same vector property. If you want to change the index type (e.g., from IVF to HNSW) you must drop the index first before creating a new index.

### HNSW

HNSW stands for Hierarchical Navigable Small World, a graph-based data structure that partitions vectors into clusters and subclusters. With HNSW, you can perform fast approximate nearest neighbor search at higher speeds with greater accuracy. HNSW is an approximate (ANN) method. Here's how to set it up:

```javascript
db.runCommand(
{ 
    "createIndexes": "ExampleCollection",
    "indexes": [
        {
            "name": "VectorSearchIndex",
            "key": {
                "contentVector": "cosmosSearch"
            },
            "cosmosSearchOptions": { 
                "kind": "vector-hnsw", 
                "m": 16, # default value 
                "efConstruction": 64, # default value 
                "similarity": "COS", 
                "dimensions": 1536
            } 
        } 
    ] 
}
)
```

> [!NOTE]
> HNSW indexing is only available on M40 cluster tiers and higher.

## 5. Insert data to the collection

Now insert the inventory data, which includes descriptions and their corresponding vector embeddings, into the newly created collection. To insert data into our collection, we use the `insert_many()` method provided by the `pymongo` library. The method allows us to insert multiple documents into the collection at once. Our data is stored in a JSON file, which we'll load and then insert into the database.

Download the [shoes_with_vectors.json](https://github.com/jayanta-mondal/ignite-demo/blob/main/data/shoes_with_vectors.json) file from the GitHub repository and store it in a `data` directory within your project folder.

```python
data_file = open(file="./data/shoes_with_vectors.json", mode="r") 
data = json.load(data_file)
data_file.close()

result = collection.insert_many(data)

print(f"Number of data points added: {len(result.inserted_ids)}")
```

## 6. Vector Search in Cosmos DB for MongoDB vCore

With our data successfully uploaded, we can now apply the power of vector search to find the most relevant items based on a query. The vector index we created earlier enables us to perform semantic searches within our dataset.

### 6.1 Conducting a Vector Search

To perform a vector search, we define a function `vector_search` that takes a query and the number of results to return. The function generates a vector for the query using the `generate_embeddings` function we defined earlier, then uses Cosmos DB's `$search` functionality to find the closest matching items based on their vector embeddings.

```python
# Function to assist with vector search
def vector_search(query, num_results=3):
    
    query_vector = generate_embeddings(query)

    embeddings_list = []
    pipeline = [
        {
            '$search': {
                "cosmosSearch": {
                    "vector": query_vector,
                    "numLists": 1,
                    "path": "contentVector",
                    "k": num_results
                },
                "returnStoredSource": True }},
        {'$project': { 'similarityScore': { '$meta': 'searchScore' }, 'document' : '$$ROOT' } }
    ]
    results = collection.aggregate(pipeline)
    return results
```

## 6.2 Perform vector search query

Finally, we execute our vector search function with a specific query and process the results to display them:

```python
query = "Shoes for Seattle sweater weather"
results = vector_search(query, 3)

print("\nResults:\n")
for result in results: 
    print(f"Similarity Score: {result['similarityScore']}")  
    print(f"Title: {result['document']['name']}")  
    print(f"Price: {result['document']['price']}")  
    print(f"Material: {result['document']['material']}") 
    print(f"Image: {result['document']['img_url']}") 
    print(f"Purchase: {result['document']['purchase_url']}\n")
```

## 7. Generating Ad content with GPT-4 and DALL.E

We combine all developed components to craft compelling ads, employing OpenAI's GPT-4 for text and DALL·E 3 for images. Together with vector search results, they form a complete ad. We also introduce Heelie, our intelligent assistant, tasked with creating engaging ad taglines. Through the upcoming code, you see Heelie in action, enhancing our ad creation process.

```python
from openai import OpenAI

def generate_ad_title(ad_topic):
    system_prompt = '''
    You are Heelie, an intelligent assistant for generating witty and cativating tagline for online advertisement.
        - The ad campaign taglines that you generate are short and typically under 100 characters.
    '''

    user_prompt = f'''Generate a catchy, witty, and short sentence (less than 100 characters) 
                    for an advertisement for selling shoes for {ad_topic}'''
    messages=[
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": user_prompt},
    ]

    response = client.chat.completions.create(
        model="gpt-4",
        messages=messages
    )
    
    return response.choices[0].message.content

def generate_ad_image(ad_topic):
    daliClient = OpenAI(
        api_key="<DALI_API_KEY>"
    )

    image_prompt = f'''
        Generate a photorealistic image of an ad campaign for selling {ad_topic}. 
        The image should be clean, with the item being sold in the foreground with an easily identifiable landmark of the city in the background.
        The image should also try to depict the weather of the location for the time of the year mentioned.
        The image should not have any generated text overlay.
    '''

    response = daliClient.images.generate(
        model="dall-e-3",
        prompt= image_prompt,
        size="1024x1024",
        quality="standard",
        n=1,
        )

    return response.data[0].url

def render_html_page(ad_topic):

    # Find the matching shoes from the inventory
    results = vector_search(ad_topic, 4)
    
    ad_header = generate_ad_title(ad_topic)
    ad_image_url = generate_ad_image(ad_topic)


    with open('./data/ad-start.html', 'r', encoding='utf-8') as html_file:
        html_content = html_file.read()

    html_content += f'''<header>
            <h1>{ad_header}</h1>
        </header>'''    

    html_content += f'''
            <section class="ad">
            <img src="{ad_image_url}" alt="Base Ad Image" class="ad-image">
        </section>'''

    for result in results: 
        html_content += f''' 
        <section class="product">
            <img src="{result['document']['img_url']}" alt="{result['document']['name']}" class="product-image">
            <div class="product-details">
                <h3 class="product-title" color="gray">{result['document']['name']}</h2>
                <p class="product-price">{"$"+str(result['document']['price'])}</p>
                <p class="product-description">{result['document']['description']}</p>
                <a href="{result['document']['purchase_url']}" class="buy-now-button">Buy Now</a>
            </div>
        </section>
        '''

    html_content += '''</article>
                    </body>
                    </html>'''

    return html_content
```

## 8. Putting it all together

To make our advertisement generation interactive, we employ Gradio, a Python library for creating simple web UIs. We define a UI that allows users to input ad topics and then dynamically generates and displays the resulting advertisement.

```python
import gradio as gr

css = """
    button { background-color: purple; color: red; }
    <style>
    </style>
"""

with gr.Blocks(css=css, theme=gr.themes.Default(spacing_size=gr.themes.sizes.spacing_sm, radius_size="none")) as demo:
    subject = gr.Textbox(placeholder="Ad Keywords", label="Prompt for Heelie!!")
    btn = gr.Button("Generate Ad")
    output_html = gr.HTML(label="Generated Ad HTML")

    btn.click(render_html_page, [subject], output_html)

    btn = gr.Button("Copy HTML")

if __name__ == "__main__":
    demo.launch()   
```

## Output

![Output screen](./media/tutorial-adgen/result.png)

## Next step

> [!div class="nextstepaction"]
> [Check out our Solution Accelerator for more samples](../../solutions.md)

-----------------
-----------------
-----------------
-----------------

---
title: Try free with Azure AI Advantage
titleSuffix: Azure Cosmos DB
description: Try Azure Cosmos DB free with the Azure AI Advantage offer. Innovate with a full, integrated stack purpose-built for AI-powered applications.
author: wmwxwa
ms.author: wangwilliam
ms.service: azure-cosmos-db
ms.custom:
  - ignite-2023
ms.topic: overview
ms.date: 12/03/2024
ms.collection:
  - ce-skilling-ai-copilot
appliesto:
  - ✅ NoSQL
  - ✅ MongoDB
  - ✅ MongoDB vCore
  - ✅ Cassanda
  - ✅ Gremlin
  - ✅ Table
  - ✅ PostgreSQL
---

# Try Azure Cosmos DB free with Azure AI Advantage

Azure offers a full, integrated stack purpose-built for AI-powered applications. If you build your AI application stack on Azure using Azure Cosmos DB, your design can lead to solutions that get to market faster, experience lower latency, and have comprehensive built-in security.

There are many benefits when using Azure Cosmos DB and Azure AI together:

- Manage provisioned throughput to scale seamlessly as your app grows

- Rely on world-class infrastructure and security to grow your business while safeguarding your data

- Enhance the reliability of your generative AI applications by using the speed of Azure Cosmos DB to retrieve and process data

## The offer

The Azure AI Advantage offer is for existing Azure AI and GitHub Copilot customers who want to use Azure Cosmos DB as part of their solution stack. With this offer, you get:

- Free 40,000 [RU/s](request-units.md) of Azure Cosmos DB throughput (equivalent of up to $6,000) for 90 days.

- Funding to implement a new AI application using Azure Cosmos DB and/or Azure Kubernetes Service. For more information, speak to your Microsoft representative.

If you decide that Azure Cosmos DB is right for you, you can receive up to 63% discount on [Azure Cosmos DB prices through Reserved Capacity](reserved-capacity.md).

## Get started

Get started with this offer by ensuring that you have the prerequisite services before applying.

1. Make sure that you have an Azure account with an active subscription. If you don't already have an account, [create an account for free](https://azure.microsoft.com/free).

1. Ensure that you previously used one of the qualifying services in your subscription:

    - Azure AI Services

        - Azure OpenAI Service

        - Azure Machine Learning

        - Azure AI Search

    - GitHub Copilot

1. Create a new Azure Cosmos DB account using one of the following APIs:

    - API for NoSQL

    - API for MongoDB RU

    - API for Apache Cassandra

    - API for Apache Gremlin

    - API for Table

    > [!IMPORTANT]
    > The Azure Cosmos DB account must have been created within 30 days of registering for the offer.

1. Register for the Azure AI Advantage offer: <https://aka.ms/AzureAIAdvantageSignupForm>

1. The team reviews your registration and follows up via e-mail.

## After the offer

After 90 days, your Azure Cosmos DB account will continue to run at [standard pricing rates](https://azure.microsoft.com/pricing/details/cosmos-db/).

## Related content

- [Build & modernize AI application reference architecture](https://github.com/Azure/Build-Modern-AI-Apps)

---------------------
---------------------
---------------------
---------------------

---
title: Quickstart - Azure SDK for .NET
titleSuffix: Azure Cosmos DB for NoSQL
description: Deploy a .NET web application that uses the Azure SDK for .NET to interact with Azure Cosmos DB for NoSQL data in this quickstart.
author: seesharprun
ms.author: sidandrews
ms.service: azure-cosmos-db
ms.subservice: nosql
ms.devlang: csharp
ms.topic: quickstart-sdk
ms.date: 02/26/2025
ms.custom: devx-track-csharp, devx-track-dotnet, devx-track-extended-azdevcli
appliesto:
  - ✅ NoSQL
# CustomerIntent: As a developer, I want to learn the basics of the .NET library so that I can build applications with Azure Cosmos DB for NoSQL.
---

# Quickstart: Use Azure Cosmos DB for NoSQL with Azure SDK for .NET

[!INCLUDE[Developer Quickstart selector](includes/quickstart/dev-selector.md)]

In this quickstart, you deploy a basic Azure Cosmos DB for NoSQL application using the Azure SDK for .NET. Azure Cosmos DB for NoSQL is a schemaless data store allowing applications to store unstructured data in the cloud. Query data in your containers and perform common operations on individual items using the Azure SDK for .NET.

[API reference documentation](/dotnet/api/microsoft.azure.cosmos) | [Library source code](https://github.com/Azure/azure-cosmos-dotnet-v3) | [Package (NuGet)](https://www.nuget.org/packages/Microsoft.Azure.Cosmos) | [Azure Developer CLI](/azure/developer/azure-developer-cli/overview)

## Prerequisites

- Azure Developer CLI
- Docker Desktop
- .NET 9.0

If you don't have an Azure account, create a [free account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F) before you begin.

## Initialize the project

Use the Azure Developer CLI (`azd`) to create an Azure Cosmos DB for NoSQL account and deploy a containerized sample application. The sample application uses the client library to manage, create, read, and query sample data.

1. Open a terminal in an empty directory.

1. If you're not already authenticated, authenticate to the Azure Developer CLI using `azd auth login`. Follow the steps specified by the tool to authenticate to the CLI using your preferred Azure credentials.

    ```azurecli
    azd auth login
    ```

1. Use `azd init` to initialize the project.

    ```azurecli
    azd init --template cosmos-db-nosql-dotnet-quickstart
    ```

1. During initialization, configure a unique environment name.

1. Deploy the Azure Cosmos DB account using `azd up`. The Bicep templates also deploy a sample web application.

    ```azurecli
    azd up
    ```

1. During the provisioning process, select your subscription, desired location, and target resource group. Wait for the provisioning process to complete. The process can take **approximately five minutes**.

1. Once the provisioning of your Azure resources is done, a URL to the running web application is included in the output.

    ```output
    Deploying services (azd deploy)
    
      (✓) Done: Deploying service web
    - Endpoint: <https://[container-app-sub-domain].azurecontainerapps.io>
    
    SUCCESS: Your application was provisioned and deployed to Azure in 5 minutes 0 seconds.
    ```

1. Use the URL in the console to navigate to your web application in the browser. Observe the output of the running app.

:::image type="content" source="media/quickstart-dotnet/running-application.png" alt-text="Screenshot of the running web application.":::

### Install the client library

The client library is available through NuGet, as the `Microsoft.Azure.Cosmos` package.

1. Open a terminal and navigate to the `/src/web` folder.

    ```bash
    cd ./src/web
    ```

1. If not already installed, install the `Microsoft.Azure.Cosmos` package using `dotnet add package`.

    ```bash
    dotnet add package Microsoft.Azure.Cosmos --version 3.*
    ```

1. Also, install the `Azure.Identity` package if not already installed.

    ```bash
    dotnet add package Azure.Identity --version 1.12.*
    ```

1. Open and review the **src/web/Cosmos.Samples.NoSQL.Quickstart.Web.csproj** file to validate that the `Microsoft.Azure.Cosmos` and `Azure.Identity` entries both exist.

## Object model

| Name | Description |
| --- | --- |
| <xref:Microsoft.Azure.Cosmos.CosmosClient> | This class is the primary client class and is used to manage account-wide metadata or databases. |
| <xref:Microsoft.Azure.Cosmos.Database> | This class represents a database within the account. |
| <xref:Microsoft.Azure.Cosmos.Container> | This class is primarily used to perform read, update, and delete operations on either the container or the items stored within the container. |
| <xref:Microsoft.Azure.Cosmos.PartitionKey> | This class represents a logical partition key. This class is required for many common operations and queries. |

## Code examples

- [Authenticate the client](#authenticate-the-client)
- [Get a database](#get-a-database)
- [Get a container](#get-a-container)
- [Create an item](#create-an-item)
- [Get an item](#read-an-item)
- [Query items](#query-items)

The sample code in the template uses a database named `cosmicworks` and container named `products`. The `products` container contains details such as name, category, quantity, a unique identifier, and a sale flag for each product. The container uses the `/category` property as a logical partition key.

### Authenticate the client

This sample creates a new instance of the `CosmosClient` class and authenticates using a `DefaultAzureCredential` instance.

```csharp
DefaultAzureCredential credential = new();

CosmosClient client = new(
    accountEndpoint: "<azure-cosmos-db-nosql-account-endpoint>",
    tokenCredential: new DefaultAzureCredential()
);
```

### Get a database

Use `client.GetDatabase` to retrieve the existing database named *`cosmicworks`*.

```csharp
Database database = client.GetDatabase("cosmicworks");
```

### Get a container

Retrieve the existing *`products`* container using `database.GetContainer`.

```csharp
Container container = database.GetContainer("products");
```

### Create an item

Build a C# record type with all of the members you want to serialize into JSON. In this example, the type has a unique identifier, and fields for category, name, quantity, price, and sale.

```csharp
public record Product(
    string id,
    string category,
    string name,
    int quantity,
    decimal price,
    bool clearance
);
```

Create an item in the container using `container.UpsertItem`. This method "upserts" the item effectively replacing the item if it already exists.

```csharp
Product item = new(
    id: "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
    category: "gear-surf-surfboards",
    name: "Yamba Surfboard",
    quantity: 12,
    price: 850.00m,
    clearance: false
);

ItemResponse<Product> response = await container.UpsertItemAsync<Product>(
    item: item,
    partitionKey: new PartitionKey("gear-surf-surfboards")
);
```

### Read an item

Perform a point read operation by using both the unique identifier (`id`) and partition key fields. Use `container.ReadItem` to efficiently retrieve the specific item.

```csharp
ItemResponse<Product> response = await container.ReadItemAsync<Product>(
    id: "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
    partitionKey: new PartitionKey("gear-surf-surfboards")
);
```

### Query items

Perform a query over multiple items in a container using `container.GetItemQueryIterator`. Find all items within a specified category using this parameterized query:

```nosql
SELECT * FROM products p WHERE p.category = @category
```

```csharp
string query = "SELECT * FROM products p WHERE p.category = @category"

var query = new QueryDefinition(query)
  .WithParameter("@category", "gear-surf-surfboards");

using FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(
    queryDefinition: query
);
```

Parse the paginated results of the query by looping through each page of results using `feed.ReadNextAsync`. Use `feed.HasMoreResults` to determine if there are any results left at the start of each loop.

```csharp
List<Product> items = new();
while (feed.HasMoreResults)
{
    FeedResponse<Product> response = await feed.ReadNextAsync();
    foreach (Product item in response)
    {
        items.Add(item);
    }
}
```

### Explore your data

Use the Visual Studio Code extension for Azure Cosmos DB to explore your NoSQL data. You can perform core database operations including, but not limited to:

- Performing queries using a scrapbook or the query editor
- Modifying, updating, creating, and deleting items
- Importing bulk data from other sources
- Managing databases and containers

For more information, see [How-to use Visual Studio Code extension to explore Azure Cosmos DB for NoSQL data](../visual-studio-code-extension.md?pivots=api-nosql).

## Clean up resources

When you no longer need the sample application or resources, remove the corresponding deployment and all resources.

```azurecli
azd down
```

## Related content

- [Node.js Quickstart](quickstart-nodejs.md)
- [Python Quickstart](quickstart-python.md)
- [Java Quickstart](quickstart-java.md)
- [Go Quickstart](quickstart-go.md)
- [Rust Quickstart](quickstart-go.md)

-------------------------
------------------------
----------------------
--------------------
-------------------
------------------
-----------------
----------------
---------------
--------------
-------------
------------
-----------
----------
---------
--------
-------
------
-----
----
---
--
-