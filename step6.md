### Hands-On Lab: Creating and Using an AI Agent in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service in a C# console application to upload files, create a vector store, and perform file searches.

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
   ```

4. **Define Connection String**
   Define the connection string for the Azure AI Agent Service:
   ```csharp
   var connectionString = "Your Azure AI Agent Service Connection String";
   ```

5. **Initialize AgentsClient**
   Create an instance of `AgentsClient` with the connection string and default Azure credentials:
   ```csharp
   AgentsClient client = new AgentsClient(connectionString, new DefaultAzureCredential());
   ```

6. **Upload a File**
   Upload a file to the Azure AI Agent Service:
   ```csharp
   Azure.Response<AgentFile> uploadAgentFileResponse = await client.UploadFileAsync(
       filePath: "./data/intro_rag.md",
       purpose: AgentFilePurpose.Agents
   );
   AgentFile uploadedAgentFile = uploadAgentFileResponse.Value;
   ```

7. **Create a Vector Store**
   Create a vector store using the uploaded file:
   ```csharp
   VectorStore vectorStore = await client.CreateVectorStoreAsync(
       fileIds: new List<string> { uploadedAgentFile.Id },
       name: "my_vector_store"
   );
   ```

8. **Create a File Search Tool Resource**
   Create a file search tool resource with the vector store ID:
   ```csharp
   FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
   fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);
   ```

9. **Create an Agent**
   Create an agent using the `AgentsClient` with the file search tool resource:
   ```csharp
   Azure.Response<Agent> agentResponse = await client.CreateAgentAsync(
       model: "gpt-4o-mini",
       name: "RAG Agent",
       instructions: "You are a helpful agent that can help fetch data from files you know about.",
       tools: new List<ToolDefinition> { new FileSearchToolDefinition() },
       toolResources: new ToolResources() { FileSearch = fileSearchToolResource }
   );
   Agent agent = agentResponse.Value;
   ```

10. **Create a Communication Thread**
    Create a communication thread for the agent:
    ```csharp
    Azure.Response<AgentThread> threadResponse = await client.CreateThreadAsync();
    AgentThread thread = threadResponse.Value;
    ```

11. **Send a Message to the Agent**
    Send a message to the thread with specific instructions for the agent:
    ```csharp
    Azure.Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
        thread.Id,
        MessageRole.User,
        "Can you introduce GraphRAG?"
    );
    ThreadMessage message = messageResponse.Value;
    ```

12. **Execute a Run**
    Create and execute a run for the agent to process the message:
    ```csharp
    Azure.Response<ThreadRun> runResponse = await client.CreateRunAsync(thread, agent);

    do
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);
    } while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
    ```

13. **Retrieve and Display Messages**
    Retrieve and display messages from the thread after the run is completed:
    ```csharp
    Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(thread.Id);
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

14. **Insert Your Connection String**
    Replace `"Your Azure AI Agent Service Connection String"` with your actual Azure AI Agent Service connection string.

15. **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

This guide walks you through creating a C# console application that uses Azure AI Agent Service to upload files, create a vector store, and perform file searches.