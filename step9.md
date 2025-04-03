### Hands-On Lab: Creating and Using an AI Agent with Semantic Kernel and Bing Grounding in a C# Console Application

#### Objective
Learn how to create and use an AI agent using Azure AI Agent Service with Semantic Kernel and Bing Grounding in a C# console application.


## Prerequisites
- An Azure account
- Visual Studio or Visual Studio Code
- .NET SDK installed
- Azure AI Project created. Will require
  - connection string
  - endpoint URL
  - key
- Bing Grounding Setup. See [Grounding with Bing Search Setup](step8_grounding.md)
- Azure OpenAI deployment, such as gpt-4o, in Azure AI Project. Note that gpt-4o-mini is not supported with Bing Grounding. 
- Appropriate permissions to Azure AI Project
- Logged into appropriate Azure subscription via Azure CLI

#### Step-by-Step Guide

1. **Create a New C# Console Application**
   - Open your terminal or command prompt and run the following command to create a new C# console application:

   ```
   dotnet new console -n AzureSKMultiAgent
   cd AzureSKMultiAgent

   ```
   - In Visual Studio, open the AzureSKMultiAgent.csproj .
   - In Visual Studio Code, open the folder `AzureSKMultiAgent`. You can use the command `code .` to open the project in VS Code.

2. **Add Necessary NuGet Packages**
   
   - Add the required NuGet packages for Microsoft Semantic Kernel, Azure AI Agent Service, and Azure Identity:
   ```
   dotnet add package Microsoft.SemanticKernel.Agents.Abstractions --version 1.32.0-alpha
   dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.32.0-alpha
   dotnet add package Microsoft.SemanticKernel.Agents.OpenAI --version 1.32.0-alpha
   dotnet add package Microsoft.SemanticKernel.Connectors.AzureOpenAI --version 1.32.0-alpha
   dotnet add package Azure.AI.Projects --version 1.0.0-beta.2
   dotnet add package Azure.Identity --version 1.13.1

   ```

3. **Import Namespaces**
   - Open the solution in Visual Studio or Visual Studio Code.
   - Remove any code in Program.cs
   - Import the necessary namespaces for the Azure SDKs at the top of your `Program.cs` file:
   ```csharp
   using System;
   using System.ComponentModel;
   using System.Collections.Generic;
   using System.Threading.Tasks;
   using Azure.Core;
   using Azure.Identity;
   using Azure.AI.Projects;
   using Azure.Core.Pipeline;
   using Microsoft.SemanticKernel;
   using Microsoft.SemanticKernel.Agents;
   using Microsoft.SemanticKernel.Agents.Chat;
   using Microsoft.SemanticKernel.ChatCompletion;
   using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

   ```

4. **Define Program Class Structure**
    ```csharp
    public partial class Program
    {
         const string HostName = "SeachAssistant";
         const string HostInstructions = "Search information ";

         // Main method and other code will go here
    }

    // ApprovalTerminationStrategy class will go here

    // Custom headers policy class will go here

    // Search plugin class will go here

    // Save plugin class will go here





    ```
    - Creates a partial class to implement the main program logic
    - Defines constants for the assistant's name and instructions

5. **Set Up Main Method**
    - In Program class , add the following code to define the entry point of the application:
    ```csharp
    public static async Task Main(string[] args)
    {
         var deployment = "gpt-4o";
         var endpoint = "Your AOAI endpoint";
         var key = "Your AOAI Key";
    }
    ```
    - Defines the entry point of the application
    - Configures Azure OpenAI deployment settings

6. **Initialize Semantic Kernel**
    ```csharp
    var kernel = Kernel.CreateBuilder()
         .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
         .Build();
      // code comes below 
    ```
    - Creates a new kernel instance
    - Adds Azure OpenAI chat completion capability



7. **Create Custom Headers Policy Class**
   - Note the code below is a seperate class in Program.cs. It does not go into the Program class
   - Create a custom headers policy to add the `x-ms-enable-preview` header to requests:
   ```csharp
   internal class CustomHeadersPolicy : HttpPipelineSynchronousPolicy
   {
       public override void OnSendingRequest(HttpMessage message)
       {
           message.Request.Headers.Add("x-ms-enable-preview", "true");
       }
   }
   ```

### Task 8: Define Search Plugin
- This is seperate class that you can define at the end of the file. 
- This class will be responsible for using Bing to perform searches. Let's break down the implementation of the Search Plugin into manageable steps:

1. **Create the Plugin Class Structure**
    ```csharp
    public sealed class SearchPlugin
    {
         [KernelFunction, Description("Search by Bing")]
         public async Task<string> Search([Description("search Item")] string searchItem)
         {
             // Implementation of the search functionality will go here
         }
    }
    ```
    - Creates a sealed class that will handle search functionality
    - Defines a method decorated with KernelFunction attribute for Semantic Kernel integration

2. **Set Up Azure AI Project Client**
    ```csharp
    var connectionString = "Your Azure AI Agent Service Connection String";
    var clientOptions = new AIProjectClientOptions();
    clientOptions.AddPolicy(new CustomHeadersPolicy(), HttpPipelinePosition.PerCall);
    var projectClient = new AIProjectClient(connectionString, new DefaultAzureCredential(), clientOptions);
    ```
    - Initializes the client with connection string
    - Configures client options with custom headers
    - Creates the project client instance

3. **Get Bing Connection**
    ```csharp
    var BING_CONNECTION_NAME = "ENTER_BING_CONNECTION_NAME"; 
    ConnectionResponse bingConnection = await projectClient.GetConnectionsClient().GetConnectionAsync(BING_CONNECTION_NAME);
    var connectionId = bingConnection.Id;
    ```
    - Retrieves the Bing search connection
    - Stores the connection ID for later use

4. **Configure Agent Tools**
    ```csharp
    AgentsClient agentClient = projectClient.GetAgentsClient();
    ToolConnectionList connectionList = new ToolConnectionList { ConnectionList = { new ToolConnection(connectionId) } };
    BingGroundingToolDefinition bingGroundingTool = new BingGroundingToolDefinition(connectionList);
    ```
    - Sets up the agent client
    - Creates connection list for tools
    - Configures Bing grounding tool

5. **Create Agent Instance**
    ```csharp
    Azure.Response<Azure.AI.Projects.Agent> agentResponse = await agentClient.CreateAgentAsync(
         model: "gpt-4o",
         name: "my-assistant",
         instructions: "You are a helpful assistant.",
         tools: new List<ToolDefinition> { bingGroundingTool });
    Azure.AI.Projects.Agent agent = agentResponse.Value;
    ```
    - Creates an AI agent with specified model and configuration

6. **Create and Manage Thread**
    ```csharp
    Azure.Response<AgentThread> threadResponse = await agentClient.CreateThreadAsync();
    AgentThread thread = threadResponse.Value;
    Azure.Response<ThreadMessage> messageResponse = await agentClient.CreateMessageAsync(
         thread.Id,
         MessageRole.User,
         "what is the current Microsoft stock price?");
    ```
    - Creates a new thread for conversation
    - Adds initial message to the thread

7. **Execute and Monitor Search**
    ```csharp
    Azure.Response<ThreadRun> runResponse = await agentClient.CreateRunAsync(thread, agent);
    // Poll until completion
    do
    {
         await Task.Delay(TimeSpan.FromMilliseconds(500));
         runResponse = await agentClient.GetRunAsync(thread.Id, runResponse.Value.Id);
    }
    while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
    ```
    - Initiates the search operation
    - Monitors progress until completion

8. **Process and Return Results**
    ```csharp
    Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await agentClient.GetMessagesAsync(thread.Id);
    IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

        string searchResult = "";

        foreach (ThreadMessage threadMessage in messages)
        {
            Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");

            if(threadMessage.Role.ToString().ToLower() == "assistant")
            {
                foreach (MessageContent contentItem in threadMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        Console.Write(textItem.Text);
                        searchResult = textItem.Text;
                    }
                    break;
                }
            }
        }

        return searchResult;
    
    ```
    - Retrieves search results
    - Processes and formats the response

#### Important Note: Remember to 
- replace the connection string placeholder with your actual Azure AI Agent Service connection string before running the code.
- replace the Bing connection name placeholder with your actual Bing connection name before running the code.

### Complete SearchPlugin code
```csharp
public sealed class SearchPlugin
{
    [KernelFunction, Description("Search by Bing")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public async Task<string> Search([Description("search Item")] string searchItem)
    {
        var connectionString = "ENTER_YOUR_AI_CONNECTIONSTRING";
        var clientOptions = new AIProjectClientOptions();
        clientOptions.AddPolicy(new CustomHeadersPolicy(), HttpPipelinePosition.PerCall);
        var projectClient = new AIProjectClient(connectionString, new DefaultAzureCredential(), clientOptions);
        var BING_CONNECTION_NAME = "ENTER_BING_CONNECTION_NAME";
        ConnectionResponse bingConnection = await projectClient.GetConnectionsClient().GetConnectionAsync(BING_CONNECTION_NAME);
        var connectionId = bingConnection.Id;

        AgentsClient agentClient = projectClient.GetAgentsClient();

        ToolConnectionList connectionList = new ToolConnectionList
        {
            ConnectionList = { new ToolConnection(connectionId) }
        };
        BingGroundingToolDefinition bingGroundingTool = new BingGroundingToolDefinition(connectionList);

        Azure.Response<Azure.AI.Projects.Agent> agentResponse = await agentClient.CreateAgentAsync(
            model: "gpt-4o",
            name: "my-assistant",
            instructions: "You are a helpful assistant.",
            tools: new List<ToolDefinition> { bingGroundingTool });
        Azure.AI.Projects.Agent agent = agentResponse.Value;

        Azure.Response<AgentThread> threadResponse = await agentClient.CreateThreadAsync();
        AgentThread thread = threadResponse.Value;

        Azure.Response<ThreadMessage> messageResponse = await agentClient.CreateMessageAsync(
            thread.Id,
            MessageRole.User,
            "what is the current Microsoft stock price?");
        ThreadMessage message = messageResponse.Value;

        Azure.Response<ThreadRun> runResponse = await agentClient.CreateRunAsync(thread, agent);

        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            runResponse = await agentClient.GetRunAsync(thread.Id, runResponse.Value.Id);
        }
        while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);

        Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await agentClient.GetMessagesAsync(thread.Id);
        IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

        string searchResult = "";

        foreach (ThreadMessage threadMessage in messages)
        {
            Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");

            if(threadMessage.Role.ToString().ToLower() == "assistant")
            {
                foreach (MessageContent contentItem in threadMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        Console.Write(textItem.Text);
                        searchResult = textItem.Text;
                    }
                    break;
                }
            }
        }

        return searchResult;
    }
}
```


### Step 9: Create the Save Plugin

Let's implement the Save Plugin functionality with the following steps:

1. **Create Save Plugin Class Structure**
    ```csharp
    public sealed class SavePlugin
    {
        [KernelFunction, Description("Save blog")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
        public async Task<string> Save([Description("save blog content")] string content)
        {
            // Implementation details will follow
        }
    }
    ```

2. **Initialize Agent Client**
   ```csharp
   Console.Write("###" + content);
   var connectionString = "YOUR_CONNECTION_STRING";
   AgentsClient client = new AgentsClient(connectionString, new DefaultAzureCredential());
   ```
   - Creates agent client with connection string
   - Uses default Azure credentials

3. **Create Python Assistant Agent**
   ```csharp
   Azure.Response<Azure.AI.Projects.Agent> agentResponse = await client.CreateAgentAsync(
       model: "gpt-4o-mini",
       name: "code-agent",
       instructions: "You are a personal python assistant. Write and run code to answer questions.",
       tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition() });
   Azure.AI.Projects.Agent agent = agentResponse.Value;
   ```
   - Creates a specialized Python assistant agent
   - Configures with code interpreter capabilities

4. **Create Communication Thread**
   ```csharp
   Azure.Response<AgentThread> threadResponse = await client.CreateThreadAsync();
   AgentThread thread = threadResponse.Value;
   ```
   - Establishes new communication thread

5. **Send Save Instructions**
   ```csharp
   Azure.Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
       thread.Id,
       MessageRole.User,
       @"You are my Python programming assistant. Generate code and execute it according to the following requirements
           1. Save" + content + @"file as blog-{YYMMDDHHMMSS}.md
           2. give me the download this file link
       ");
   ```
   - Sends specific instructions for file saving

6. **Execute and Monitor Run**
   ```csharp
   Azure.Response<ThreadRun> runResponse = await client.CreateRunAsync(thread.Id, agent.Id);
   do
   {
       await Task.Delay(TimeSpan.FromMilliseconds(500));
       runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);
   }
   while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
   ```
   - Initiates the run
   - Monitors until completion

7. **Process Results and Save File**
   ```csharp
   Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(thread.Id);
   IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;
   foreach (ThreadMessage threadMessage in messages)
   {
       foreach (MessageContent contentItem in threadMessage.ContentItems)
       {
           if (contentItem is MessageTextContent textItem && textItem.Annotations?.Count > 0)
           {
               if (textItem.Annotations[0] is MessageTextFilePathAnnotation pathItem)
               {
                  Azure.Response<AgentFile> agentfile = await client.GetFileAsync(pathItem.FileId);
                Azure.Response<System.BinaryData> fileBytes = await client.GetFileContentAsync(pathItem.FileId);
                var mdfile = System.IO.Path.GetFileName(agentfile.Value.Filename);
                using System.IO.FileStream stream = System.IO.File.OpenWrite($"./blog/{mdfile}");
                fileBytes.Value.ToStream().CopyTo(stream);
               }
           }
       }
   }
      return "Saved";
   ```
   - Retrieves generated file
   - Saves to local blog directory

Remember to replace "YOUR_CONNECTION_STRING" with your actual Azure AI Agent Service connection string.

### Full code for Save Plugin
```csharp
public sealed class SavePlugin
{
    [KernelFunction, Description("Save blog")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public async Task<string> Save([Description("save blog content")] string content)
    {
        Console.Write("###" + content);
var connectionString = "YOUR_CONNECTION_STRING";
AgentsClient client = new AgentsClient(connectionString, new DefaultAzureCredential());
Azure.Response<Azure.AI.Projects.Agent> agentResponse = await client.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: "code-agent",
    instructions: "You are a personal python assistant. Write and run code to answer questions.",
    tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition() });
Azure.AI.Projects.Agent agent = agentResponse.Value;
Azure.Response<AgentThread> threadResponse = await client.CreateThreadAsync();
AgentThread thread = threadResponse.Value;
Azure.Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
    thread.Id,
    MessageRole.User,
    @"You are my Python programming assistant. Generate code and execute it according to the following requirements
        1. Save" + content + @"file as blog-{YYMMDDHHMMSS}.md
        2. give me the download this file link
    ");
    Azure.Response<ThreadRun> runResponse = await client.CreateRunAsync(thread.Id, agent.Id);
do
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);
}
while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
Azure.Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(thread.Id);
IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

foreach (ThreadMessage threadMessage in messages)
{
    foreach (MessageContent contentItem in threadMessage.ContentItems)
    {
        if (contentItem is MessageTextContent textItem && textItem.Annotations?.Count > 0)
        {
            if (textItem.Annotations[0] is MessageTextFilePathAnnotation pathItem)
            {
                Azure.Response<AgentFile> agentfile = await client.GetFileAsync(pathItem.FileId);
                Azure.Response<System.BinaryData> fileBytes = await client.GetFileContentAsync(pathItem.FileId);
                var mdfile = System.IO.Path.GetFileName(agentfile.Value.Filename);
                using System.IO.FileStream stream = System.IO.File.OpenWrite($"./blog/{mdfile}");
                fileBytes.Value.ToStream().CopyTo(stream);
            }
        }
    }
}
    return "Saved";
    }
}
```

### Step 10 : Incorporate the plugins into the program

1. #### define the variables
In Program.cs in the main method, add the following code to incorporate the plugins into the program:
```csharp   
const string SearchHostName = "Search";
const string SearchHostInstructions = "You are a search expert, help me use tools to find relevant knowledge";
```
2. #### Create the Search Plugin
```csharp
#pragma warning disable SKEXP0110

ChatCompletionAgent search_agent =
            new()
            {
                Name = SearchHostName,
                Instructions = SearchHostInstructions,
                Kernel = kernel,
                Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            };
```
3. #### Create the Save Plugin
```csharp
const string SaveHostName = "SaveBlog";
const string SavehHostInstructions = "Save blog content. Respond with 'Saved' to when your blog are saved.";
#pragma warning disable SKEXP0110

ChatCompletionAgent save_blog_agent =
            new()
            {
                Name = SaveHostName,
                Instructions = SavehHostInstructions,
                Kernel = kernel,
                Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            };
```

4. #### Create the Write Blog Agent
```csharp
private const string WriteBlogName = "WriteBlog";
private const string WriteBlogInstructions =
        """
        You are a blog writer, please help me write a blog based on bing search content.
        """;
#pragma warning disable SKEXP0110

ChatCompletionAgent write_blog_agent =
            new()
            {
                Name = WriteBlogName,
                Instructions = WriteBlogInstructions,
                Kernel = kernel
            };
   ```
5. #### Add the Plugins to the Kernel  
  
```csharp   

#pragma warning disable SKEXP0110

KernelPlugin search_plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
search_agent.Kernel.Plugins.Add(search_plugin);

#pragma warning disable SKEXP0110

KernelPlugin save_blog_plugin = KernelPluginFactory.CreateFromType<SavePlugin>();
save_blog_agent.Kernel.Plugins.Add(save_blog_plugin);

```


6. #### Define a termination strategy
This class is defined as a nested class inside program class
```csharp

 #pragma warning disable SKEXP0110   
 using System.Threading;
    


private sealed class ApprovalTerminationStrategy : TerminationStrategy
{
        // Terminate when the final message contains the term "approve"
        protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("Saved", StringComparison.OrdinalIgnoreCase) ?? false);
}
```

7. #### Create the Agent Group Chat.
This code is in the main method
```csharp
#pragma warning disable SKEXP0110

AgentGroupChat chat =
            new(search_agent, write_blog_agent,save_blog_agent)
            {
                ExecutionSettings =
                    new()
                    {
                        TerminationStrategy =
                            new ApprovalTerminationStrategy()
                            {
                                // Only the art-director may approve.
                                Agents = [save_blog_agent],
                                // Limit total number of turns
                                MaximumIterations = 10,
                            }
                    }
            };
```
8. #### Start the Chat
```csharp
 #pragma warning disable SKEXP0110

ChatMessageContent input = new(AuthorRole.User, """
                    I am writing a blog about GraphRAG. Search for the following 2 questions and write a Chinese blog based on the search results ,save it           
                        1. What is Microsoft GraphRAG?
                        2. Vector-based RAG vs GraphRAG
                    """);
chat.AddChatMessage(input);
```

9. #### Process the results
```csharp
#pragma warning disable SKEXP0110   
#pragma warning disable SKEXP0001
      


await foreach (ChatMessageContent content in chat.InvokeAsync())
{
    Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
}
```



1.  **Run the Application**
    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```

To validate you should see some output, and a markdown file should be created in \bin\Debug\net9.0\blog folder.

Congratulations! You have successfully created a C# console application that uses Azure AI Agent Service with Semantic Kernel and Bing Grounding to search for information, write a blog based on the search results, and save the blog content.