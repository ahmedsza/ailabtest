### Hands-On Lab: Creating and Using an AI Agent in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service in a C# console application to generate and save blog content.

#### Prerequisites
- Azure account with necessary permissions
- .NET SDK installed
- An IDE or text editor like Visual Studio or Visual Studio Code
- Azure AI Project connection string
- Deployed chat completion model, such as gpt-4o, in Azure AI Project

#### Step-by-Step Guide

1. **Create a New C# Console Application**

    Open your terminal or command prompt and run the following command to create a new C# console application:
    ```
    dotnet new console -n AzureAIAgent5
    cd AzureAIAgent5
    ```

2. **Create Necessary User Secrets**

    Create user secrets by updating `"Your Azure AI Project Connection String"` with your actual connection string and running the following from the command line:
    ```
    dotnet user-secrets init
    dotnet user-secrets set "AzureAI:ProjectConnectionString" "Your Azure AI Project Connection String"
    ```

3. **Add Application Settings**

    Create a new file named `appsettings.json` in the root of your project and add the following content:
    ```json
    {
        "AzureAI": {
            "ModelName": "gpt-4o"
        }
    }
    ```

4. **Add appsettings.json to the Project**

    Ensure that `appsettings.json` is included in your project. You can do this by right-clicking on the project in Visual Studio and selecting "Add" > "Existing Item..." and then selecting `appsettings.json`.
    Alternatively, you can add it manually in the `.csproj` file by adding the following lines:
    ```xml
    <ItemGroup>
        <None Update="appsettings.json">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </None>
    </ItemGroup>
    ```

4. **Add Necessary NuGet Packages**

    Add the required NuGet packages for Azure AI Agent Service and Azure Identity:
    ```
    dotnet add package Azure.AI.Projects --version 1.0.0-beta.6
    dotnet add package Azure.Identity
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.UserSecrets
    ```

5. **Import Namespaces**

    Delete the contents of `Program.cs` and import the necessary namespaces for the Azure SDKs at the top of your `Program.cs` file:
    ```csharp
    using Azure.AI.Projects;
    using Azure.Identity;
    using Microsoft.Extensions.Configuration;
    ```

6. **Load Configuration Settings**

    Load the configuration settings from `appsettings.json` and user secrets in your `Program.cs` file:
    ```csharp
    // Load environment variables
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddUserSecrets<Program>()
        .Build();
    ```

7. **Initialize the Project Client**

	Initialize the AIProjectClient using your Azure AI Project connection string:
	```csharp
    // Set up the project client
    AgentsClient client = new AgentsClient(
        configuration["AzureAI:ProjectConnectionString"],
        new DefaultAzureCredential());
	```

8. **Create and Configure the Agent**

	Create and configure the code interpreter agent:
	```csharp
    // Create a Code Interpreter tool
    var codeInterpreter = new CodeInterpreterToolDefinition();

    // Create an agent with the Code Interpreter tool
    Agent agent = await client.CreateAgentAsync(
        model: configuration["AzureAI:ModelName"],
        name: "ai-lab-agent5",
        instructions: "You are a helpful agent",
        tools: [codeInterpreter]
    );
	```

9. **Create a Thread and Message**

	Create a thread for communication and send a message with instructions for the agent:
	```csharp
    // Create a thread for our interaction with the agent
    AgentThread thread = await client.CreateThreadAsync();

    // Create a message to send to the agent on the created thread
    ThreadMessage message = await client.CreateMessageAsync(
        thread.Id,
        MessageRole.User,
        @"
            You are my Python programming assistant. Generate code and execute it according to the following requirements:

            1. Save 'this is blog' to blog-{YYMMDDHHMMSS}.md
            2. Give me the download link for this file
        "
    );
	```

10. **Execute the Run**

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

11. **Display the Response Message and Save File**

	Display the response message, retrieve the generated file and save it locally:
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

            // Save the file generated by the assistant
            foreach (var annotation in lastMessage.Annotations.OfType<MessageTextFilePathAnnotation>())
            {
                AgentFile agentFile = await client.GetFileAsync(annotation.FileId);
                BinaryData fileBytes = await client.GetFileContentAsync(annotation.FileId);

                var filePath = Path.Combine("./blog", Path.GetFileName(agentFile.Filename));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await File.WriteAllBytesAsync(filePath, fileBytes.ToArray());
            }
        }
    }
	```

12. **Delete the Thread and Agent**

    After processing, delete the thread and agent to clean up resources:
    ```csharp
    // Clean up resources
    await client.DeleteThreadAsync(thread.Id);
    await client.DeleteAgentAsync(agent.Id);
    ```

13. **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

This guide walks you through creating a C# console application that uses Azure AI Agent Service to generate and save blog content.