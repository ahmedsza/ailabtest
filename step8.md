# Azure AI Agent Service with Microsoft Semantic Kernel

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

## Step 1: Create a New Console Application

1. **Open Visual Studio or Visual Studio Code.**
   - If using Visual Studio, select "Create a new project" and choose "Console App (.NET Core)".
   - If using Visual Studio Code, open the terminal and run:
     ```bash
     dotnet new console -o AzureAIAgentApp
     cd AzureAIAgentApp
     ```

2. **Open the project.**
   - In Visual Studio, open the AzureAIAgentApp.csproj .
   - In Visual Studio Code, open the folder `AzureAIAgentApp`. You can use the command `code .` to open the project in VS Code.

## Step 2: Install NuGet Packages



Install the required packages by running the following commands:

   ```bash
   dotnet add package Microsoft.SemanticKernel.Agents.Abstractions --version 1.32.0-alpha
   dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.32.0-alpha
   dotnet add package Microsoft.SemanticKernel.Agents.OpenAI --version 1.32.0-alpha
   dotnet add package Microsoft.SemanticKernel.Connectors.AzureOpenAI --version 1.32.0-alpha
   dotnet add package Azure.AI.Projects --version 1.0.0-beta.2
   dotnet add package Azure.Identity --version 1.13.1
   
   ```



## Step 3: Update Program.cs

### Task 3.1: Import Necessary Namespaces
Open Program.cs in your project.

Add the following using directives at the top of the file to import necessary namespaces

Explanation: These namespaces are required for accessing Azure services, handling kernel functionality, and managing HTTP requests and responses.

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
```

### Task 3.2: Define Custom Headers Policy

Add the following class definition in Program.cs:

Explanation: This class defines a custom policy to add specific headers to HTTP requests. In this case, it adds a header to enable preview features in Azure AI requests.

```csharp
internal class CustomHeadersPolicy : HttpPipelineSynchronousPolicy
{
    public override void OnSendingRequest(HttpMessage message)
    {
        message.Request.Headers.Add("x-ms-enable-preview", "true");
    }
}
```

### Task 3.3: Define Search Plugin

Let's break down the implementation of the Search Plugin into manageable steps:

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
         model: "gpt-4o-mini",
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
         "How does wikipedia explain Euler's Identity?");
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
            model: "gpt-4o-mini",
            name: "my-assistant",
            instructions: "You are a helpful assistant.",
            tools: new List<ToolDefinition> { bingGroundingTool });
        Azure.AI.Projects.Agent agent = agentResponse.Value;

        Azure.Response<AgentThread> threadResponse = await agentClient.CreateThreadAsync();
        AgentThread thread = threadResponse.Value;

        Azure.Response<ThreadMessage> messageResponse = await agentClient.CreateMessageAsync(
            thread.Id,
            MessageRole.User,
            "How does wikipedia explain Euler's Identity?");
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


### Task 3.4: Create Chat Completion Agent
Let's break down the implementation of the chat completion agent into steps:

1. **Define Program Class Structure**
    ```csharp
    public partial class Program
    {
         const string HostName = "SeachAssistant";
         const string HostInstructions = "Search information ";
    }
    ```
    - Creates a partial class to implement the main program logic
    - Defines constants for the assistant's name and instructions

2. **Set Up Main Method**
    ```csharp
    public static async Task Main(string[] args)
    {
         var deployment = "gpt-4o-mini";
         var endpoint = "Your AOAI endpoint";
         var key = "Your AOAI Key";
    }
    ```
    - Defines the entry point of the application
    - Configures Azure OpenAI deployment settings

3. **Initialize Semantic Kernel**
    ```csharp
    var kernel = Kernel.CreateBuilder()
         .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
         .Build();
    ```
    - Creates a new kernel instance
    - Adds Azure OpenAI chat completion capability

4. **Configure Chat Completion Agent**
    ```csharp
    #pragma warning disable SKEXP0110
    ChatCompletionAgent agent = new()
    {
         Instructions = HostInstructions,
         Name = HostName,
         Kernel = kernel,
         Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() 
         { 
              FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
         }),
    };
    ```
    - Creates a new chat completion agent
    - Sets up agent properties and execution settings

5. **Add Search Plugin**
    ```csharp
    KernelPlugin plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
    agent.Kernel.Plugins.Add(plugin);
    ```
    - Creates a plugin instance from SearchPlugin type
    - Adds the plugin to the kernel

6. **Initialize Chat and Process Input**
    ```csharp
    ChatHistory chat = new ChatHistory();
    var input = "Introduce South China Normal University";
    chat.Add(new ChatMessageContent(AuthorRole.User, input));
    ```
    - Creates a new chat history
    - Adds user input to the chat

7. **Process and Display Results**
    ```csharp
    var agentContent = agent.InvokeAsync(chat);
    await foreach (var message in agentContent)

    #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            Console.WriteLine($"# {message.AuthorName}: '{message.Content}'");
    #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    ```
    - Invokes the agent with the chat history
    - Displays messages as they are received


Complete Program class structure
```csharp
public partial class Program
{
    const string HostName = "SeachAssistant";
    const string HostInstructions = "Search information ";

    public static async Task Main(string[] args)
    {
    var deployment = "gpt-4o-mini";
    var endpoint = "AZURE_OPENAI_ENDPOINT"; // update to your Azure OpenAI endpoint
    var key = "AZURE_OPENAI_KEY"; // Updated key placeholder

    var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
        .Build();

#pragma warning disable SKEXP0110

    ChatCompletionAgent agent = new()
    {
        Instructions = HostInstructions,
        Name = HostName,
        Kernel = kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };

    KernelPlugin plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
    agent.Kernel.Plugins.Add(plugin);

    ChatHistory chat = new ChatHistory();

    var input = "Introduce South China Normal University";

    chat.Add(new ChatMessageContent(AuthorRole.User, input));
    Console.WriteLine($"# {AuthorRole.User}: '{input}'");

    var agentContent = agent.InvokeAsync(chat);

    await foreach (var message in agentContent)
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            Console.WriteLine($"# {message.AuthorName}: '{message.Content}'");
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}
```

Note: Replace the endpoint and key placeholders with your actual Azure OpenAI credentials before running the code.

#### Replace Placeholder Values
Replace "Your AOAI endpoint", "Your AOAI Key", and "Your Azure AI Agent Service Connection String" with your actual Azure OpenAI endpoint, key, and Azure AI Agent Service connection string.

## Step 4: Run the Application
Build and run the application.

In Visual Studio, press F5 to build and run the application.
In Visual Studio Code, use the terminal to run `dotnet run`.

Verify the output.

The application will output the results from the Azure AI Agent Service, displaying the search results for the query "Introduce South China Normal University".


## Troubleshooting

### Configuration Issues
- Verify all keys, endpoints, and connection strings are correctly configured
- Ensure Azure AI Project is properly set up with required connections and models
- Confirm Bing Grounding is configured correctly

### Authentication and Permissions
- Verify Azure CLI login status
- Check user permissions for all required services
- Review access control settings in Azure portal

### Connectivity
- Test network connectivity to Azure services
- Check for any firewall restrictions
- Verify VPN settings if applicable

### Documentation
- Review Azure AI Agent Service documentation for specific troubleshooting steps
- Check Microsoft Learn for updated guidance
- Monitor Azure Service Health for any ongoing issues
