### Hands-On Lab: Creating and Using an AI Agent in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service in a C# console application to generate and save blog content.

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

6. **Create an Agent**
   Create an agent using the `AgentsClient`. The agent will use the GPT-4 model and have specific instructions to act as a Python programming assistant:
   ```csharp
   Azure.Response<Agent> agentResponse = await client.CreateAgentAsync(
       model: "gpt-4o-mini",
       name: "code-agent",
       instructions: "You are a personal python assistant. Write and run code to answer questions.",
       tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition() }
   );
   Agent agent = agentResponse.Value;
   ```

7. **Create a Communication Thread**
   Create a communication thread for the agent:
   ```csharp
   Azure.Response<AgentThread> threadResponse = await client.CreateThreadAsync();
   AgentThread thread = threadResponse.Value;
   ```

8. **Send a Message to the Agent**
   Send a message to the thread with specific instructions for the agent:
   ```csharp
   Azure.Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
       thread.Id,
       MessageRole.User,
       @"
           You are my Python programming assistant. Generate code and execute it according to the following requirements:
           1. Save file as blog-{YYMMDDHHMMSS}.md
           2. Give me the download link for this file
       "
   );
   ThreadMessage message = messageResponse.Value;
   ```

9. **Execute a Run**
   Create and execute a run for the agent to process the message:
   ```csharp
   Azure.Response<ThreadRun> runResponse = await client.CreateRunAsync(thread.Id, agent.Id);
   ThreadRun run = runResponse.Value;

   do
   {
       await Task.Delay(TimeSpan.FromMilliseconds(500));
       runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);
   } while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
   ```

10. **Retrieve and Display Messages**
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

                if (textItem.Annotations is not null && textItem.Annotations.Count > 0)
                {
                    if (textItem.Annotations[0] is MessageTextFilePathAnnotation pathItem)
                    {
                        Console.Write($"[Download file]({pathItem.Text})");

                        Azure.Response<AgentFile> agentfile = await client.GetFileAsync(pathItem.FileId);
                        Azure.Response<BinaryData> fileBytes = await client.GetFileContentAsync(pathItem.FileId);

                        var mdfile = System.IO.Path.GetFileName(agentfile.Value.Filename);
                        using var stream = System.IO.File.OpenWrite($"./blog/{mdfile}");
                        fileBytes.Value.ToStream().CopyTo(stream);
                    }
                }
            }
            Console.WriteLine();
        }
    }
    ```

11. **Insert Your Connection String**
    Replace `"Your Azure AI Agent Service Connection String"` with your actual Azure AI Agent Service connection string.

12. **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

This guide walks you through creating a C# console application that uses Azure AI Agent Service to generate and save blog content.