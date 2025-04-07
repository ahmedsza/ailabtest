### Hands-On Lab: Creating and Using an AI Agent with Bing Grounding in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service with Bing Grounding in a C# console application.

#### Prerequisites
- Pre-requisites are documented in the [PreReq](prereq/prereq.md) document.

#### Step-by-Step Guide

1. **Create a New C# Console Application**

    Open your terminal or command prompt and run the following command to create a new C# console application:
    ```
    dotnet new console -n AzureAIAgent7
    cd AzureAIAgent7
    ```

1. **Add Necessary NuGet Packages**

    Add the required NuGet packages for Azure AI Agent Service and Azure Identity:
    ```
    dotnet add package Azure.AI.Projects --version 1.0.0-beta.6
    dotnet add package Azure.Identity
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```

1. **Add Application Settings**

    Create a new file named `appsettings.json` in the root of your project and add the following content, Replace the BINGCONNECTIONNAME with your Bing Connection Name:
    ```json
    {
        "AzureAI": {
            "ProjectConnectionString": "<your-connection-string>",
            "ModelName": "<your-model-name>",
            "BingConnectionName": "<your-bing-connection-name>"
        }
    }
    ```
    **NOTE**: 
    - Replace `<your-connection-string>` with your actual Azure AI Project connection string.
    - Replace `<your-model-name>` with the name of the model you want to use (e.g., "gpt-4o").
    - Replace `<your-bing-connection-name>` with the name of your Bing connection in your AI Project's management center.

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

1. **Initialize the Project and Agents Client**

	Initialize the AIProjectClient and AgentsClient using your Azure AI Project connection string:
	```csharp
    // Set up the project and agents client
    AIProjectClient projectClient = new AIProjectClient(
        configuration["AzureAI:ProjectConnectionString"],
        new DefaultAzureCredential());
    AgentsClient client = projectClient.GetAgentsClient();
	```

1. **Create a Bing Grounding Tool**

    Create a Bing Grounding tool using the connection name
	```csharp
    // Create a connection to the Bing Connection
    ConnectionResponse bingConnection = await projectClient.GetConnectionsClient().GetConnectionAsync(configuration["AzureAI:BingConnectionName"]);
    var bingGroundingTool = new BingGroundingToolDefinition(new ToolConnectionList
    {
        ConnectionList = { new ToolConnection(bingConnection.Id) }
    });
	```

1. **Create and Configure the Agent**

    Create and configure the web search agent:
    ```csharp
    // Create an agent with the Bing Grounding tool
    Agent agent = await client.CreateAgentAsync(
        model: configuration["AzureAI:ModelName"],
        name: "ai-lab-agent7",
        instructions: @"
            You are a web search agent.
            Your only tool is search_tool - use it to find information.
            You make only one search call at a time.
            Once you have the results, you never do calculations based on them.",
        tools: [bingGroundingTool]
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
            What's Microsoft'?
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

1. **Delete the Thread and Agent**

    After processing, delete the thread and agent to clean up resources:
    ```csharp
    // Clean up resources
    await client.DeleteThreadAsync(thread.Id);
    await client.DeleteAgentAsync(agent.Id);
    ```

1. **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

This guide walks you through creating a C# console application that uses Azure AI Agent Service with Bing Grounding to search for information and retrieve results.
