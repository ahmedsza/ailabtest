### Hands-On Lab: Creating and Using an AI Agent in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service in a C# console application to upload files, create a vector store, and perform file searches.

#### Prerequisites
- Pre-requisites are documented in the [PreReq](prereq/prereq.md) document.

#### Step-by-Step Guide

1. **Create a New C# Console Application**

    Open your terminal or command prompt and run the following command to create a new C# console application:
    ```
    dotnet new console -n AzureAIAgent6
    cd AzureAIAgent6
    ```

1. **Prepare a Sample File**

    Create a folder `data` in the AzureAIAgent5 directory. Copy intro_rag.md from the repo into the data directory.

1. **Add Necessary NuGet Packages**

    Add the required NuGet packages for Azure AI Agent Service and Azure Identity:
    ```
    dotnet add package Azure.AI.Projects --version 1.0.0-beta.6
    dotnet add package Azure.Identity
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```

1. **Add Application Settings**

    Create a new file named `appsettings.json` in the root of your project and add the following content:
    ```json
    {
        "AzureAI": {
            "ProjectConnectionString": "<your-connection-string>",
            "ModelName": "<your-model-name>"
        }
    }
    ```
    **NOTE**: 
    - Replace `<your-connection-string>` with your actual Azure AI Project connection string.
    - Replace `<your-model-name>` with the name of the model you want to use (e.g., "gpt-4o").

1. **Add appsettings.json to the Project**

    Ensure that `appsettings.json` is included in your project. You can do this by right-clicking on the project in Visual Studio and selecting "Add" > "Existing Item..." and then selecting `appsettings.json`.
    Alternatively, you can add it manually in the `.csproj` file by adding the following lines:
    ```xml
    <ItemGroup>
        <None Update="appsettings.json">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </None>
    </ItemGroup>
    ```



1. **Import Namespaces**

    Delete the contents of `Program.cs` and import the necessary namespaces for the Azure SDKs at the top of your `Program.cs` file:
    ```csharp
    using Azure.AI.Projects;
    using Azure.Identity;
    using Microsoft.Extensions.Configuration;
    ```

1. **Load Configuration Settings**

    Load the configuration settings from `appsettings.json` and user secrets in your `Program.cs` file:
    ```csharp
    // Load environment variables
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    ```

1. **Initialize the Project Client**

	Initialize the AIProjectClient using your Azure AI Project connection string:
	```csharp
    // Set up the project client
    AgentsClient client = new AgentsClient(
        configuration["AzureAI:ProjectConnectionString"],
        new DefaultAzureCredential());
	```

1. **Upload File and Create Vector Store**

   Upload the local file to Azure and create a vector store:
	```csharp
    // Upload the local file to Azure
    AgentFile uploadedAgentFile = await client.UploadFileAsync(
        filePath: "./data/intro_rag.md",
        purpose: AgentFilePurpose.Agents
    );

    // Create a vector store with the uploaded file
    VectorStore vectorStore = await client.CreateVectorStoreAsync(
        fileIds: new List<string> { uploadedAgentFile.Id },
        name: "sample_vector_store"
    );
    Console.WriteLine($"Created vector store, vector store ID: {vectorStore.Id}");
	```

1. **Create a File Search Tool**

    Create a file search tool using the created vector store:
    ```csharp
    // Create a File Search tool, using the vector store as a data source
    FileSearchToolDefinition fileSearchTool = new FileSearchToolDefinition();
    FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
    fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);
    ```

1. **Create and Configure the Agent**

    Create and configure the file search agent:
    ```csharp
    // Create an agent with the File Search tool
    Agent agent = await client.CreateAgentAsync(
        model: configuration["AzureAI:ModelName"],
        name: "ai-lab-agent6",
        instructions: "You are a helpful agent",
        tools: [fileSearchTool],
        toolResources: new ToolResources() { FileSearch = fileSearchToolResource }
    );
    ```

1. **Create a Thread and Message**

	Create a thread for communication and send a message with instructions for the agent:
	```csharp
    // Create a thread for our interaction with the agent
    AgentThread thread = await client.CreateThreadAsync();

    // Create a message to send to the agent on the created thread
    ThreadMessage message = await client.CreateMessageAsync(
        thread.Id,
        MessageRole.User,
        @"
            What is GraphRAG?
        "
    );
	```

1. **Execute the Run**

	Create and execute the run to process the message:
	```csharp
    // Process the message with the agent, asynchronously
    ThreadRun run = await client.CreateRunAsync(thread.Id, agent.Id);
    do
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        run = await client.GetRunAsync(thread.Id, run.Id);
    } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
    Console.WriteLine($"Run finished with status: {run.Status}");
	```

1. **Display the Response Message**

	Display the response message:
	```csharp
    // Check the status of the run
    if (run.Status == RunStatus.Failed)
    {
        Console.WriteLine($"Run failed with error: {run.LastError}");
    }
    else
    {
        // Get the response messages
        Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(thread.Id);
        IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

        // Print the last message from the assistant
        var lastMessage = messages.Last(m => m.Role == MessageRole.Agent)?.ContentItems[0] as MessageTextContent;
        if (lastMessage is not null)
        {
            Console.WriteLine($"Last message: {lastMessage.Text}");
        }
    }
	```

1. **Delete the Vector Store, Thread and Agent**

    After processing, delete the vector store, thread and agent to clean up resources:
    ```csharp
    // Clean up resources
    await client.DeleteVectorStoreAsync(vectorStore.Id);
    await client.DeleteThreadAsync(thread.Id);
    await client.DeleteAgentAsync(agent.Id);
    ```

1. **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

This guide walks you through creating a C# console application that uses Azure AI Agent Service to upload files, create a vector store, and perform file searches.