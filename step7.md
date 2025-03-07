### Hands-On Lab: Creating and Using an AI Agent with Bing Grounding in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service with Bing Grounding in a C# console application.

#### Prerequisites
- Azure account with necessary permissions
- .NET SDK installed
- An IDE or text editor like Visual Studio or Visual Studio Code

#### Step-by-Step Guide

1. **Create a New C# Console Application**
   Open your terminal or command prompt and run the following command to create a new C# console application:
   ```
   dotnet new console -n AzureAIAgentApp
   cd AzureAIAgentApp
   ```

2. **Add Necessary NuGet Packages**
   Add the required NuGet packages for Azure AI Agent Service and Azure Identity:
   ```
   dotnet add package Azure.AI.Projects --version 1.0.0-beta.2
   dotnet add package Azure.Identity --version 1.13.1
   ```

3. **Import Namespaces**
   Import the necessary namespaces for the Azure SDKs at the top of your `Program.cs` file:
   ```csharp
   using System;
   using System.Collections.Generic;
   using System.Threading.Tasks;
   using Azure.Core;
   using Azure.Identity;
   using Azure.AI.Projects;
   using Azure.Core.Pipeline;
   ```

4. **Define Connection String**
   Define the connection string for the Azure AI Agent Service:
   ```csharp
   var connectionString = "Your Azure AI Agent Service Connection String";
   ```

5. **Create Custom Headers Policy**
   Create a custom headers policy to add the `x-ms-enable-preview` header to requests:
   ```csharp
   internal class CustomHeadersPolicy : HttpPipelineSynchronousPolicy
   {
       public override void OnSendingRequest(HttpMessage message)
       {
           message.Request.Headers.Add("x-ms-enable-preview", "true");
       }
   }
   ```

6. **Initialize AIProjectClient with Custom Headers**
   Create an instance of `AIProjectClient` with the custom headers policy:
   ```csharp
   var clientOptions = new AIProjectClientOptions();
   clientOptions.AddPolicy(new CustomHeadersPolicy(), HttpPipelinePosition.PerCall);
   var projectClient = new AIProjectClient(connectionString, new DefaultAzureCredential(), clientOptions);
   ```

7. **Retrieve Bing Connection**
   Retrieve the Bing connection using the `AIProjectClient`:
   ```csharp
   ConnectionResponse bingConnection = await projectClient.GetConnectionsClient().GetConnectionAsync("kinfey-bing-search");
   var connectionId = bingConnection.Id;
   ```

8. **Initialize AgentsClient**
   Create an instance of `AgentsClient` using the `AIProjectClient`:
   ```csharp
   AgentsClient agentClient = projectClient.GetAgentsClient();
   ```

9. **Create Bing Grounding Tool Definition**
   Create a Bing grounding tool definition with the retrieved connection ID:
   ```csharp
   ToolConnectionList connectionList = new ToolConnectionList
   {
       ConnectionList = { new ToolConnection(connectionId) }
   };
   BingGroundingToolDefinition bingGroundingTool = new BingGroundingToolDefinition(connectionList);
   ```

10. **Create an Agent**
    Create an agent using the `AgentsClient` with the Bing grounding tool:
    ```csharp
    Azure.Response<Agent> agentResponse = await agentClient.CreateAgentAsync(
        model: "gpt-4",
        name: "web-search-assistant",
        instructions: @"
            You are a web search agent.
            Your only tool is search_tool - use it to find information.
            You make only one search call at a time.
            Once you have the results, you never do calculations based on them.",
        tools: new List<ToolDefinition> { bingGroundingTool }
    );
    Agent agent = agentResponse.Value;
    ```

11. **Create a Communication Thread**
    Create a communication thread for the agent:
    ```csharp
    Azure.Response<AgentThread> threadResponse = await agentClient.CreateThreadAsync();
    AgentThread thread = threadResponse.Value;
    ```

12. **Send a Message to the Agent**
    Send a message to the thread asking about Microsoft:
    ```csharp
    Azure.Response<ThreadMessage> messageResponse = await agentClient.CreateMessageAsync(
        thread.Id,
        MessageRole.User,
        "What's Microsoft?"
    );
    ThreadMessage message = messageResponse.Value;
    ```

13. **Execute a Run**
    Create and execute a run for the agent to process the message:
    ```csharp
    Azure.Response<ThreadRun> runResponse = await agentClient.CreateRunAsync(thread, agent);

    do
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        runResponse = await agentClient.GetRunAsync(thread.Id, runResponse.Value.Id);
    } while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
    ```

14. **Retrieve and Display Messages**
    Retrieve and display messages from the thread after the run is completed:
    ```csharp
    Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await agentClient.GetMessagesAsync(thread.Id);
    IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

    foreach (ThreadMessage threadMessage in messages)
    {
        Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
        foreach (MessageContent contentItem in threadMessage.ContentItems)
        {
            if (contentItem is MessageTextContent textItem)
            {
                Console.Write(textItem.Text);
            }
            else if (contentItem is MessageImageFileContent imageFileItem)
            {
                Console.Write($"<image from ID: {imageFileItem.FileId}>");
            }
            Console.WriteLine();
        }
    }
    ```

15. **Insert Your Connection String**
    Replace `"Your Azure AI Agent Service Connection String"` with your actual Azure AI Agent Service connection string.

16. **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

This guide walks you through creating a C# console application that uses Azure AI Agent Service with Bing Grounding to search for information and retrieve results.