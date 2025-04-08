### Hands-On Lab: Azure AI Agent Service with Microsoft Semantic Kernel

#### Objective

Learn how to create and configure a web search agent using Semantic Kernel and Azure AI Agent Service to perform web searches.

#### Prerequisites
- Pre-requisites are documented in the [PreReq](prereq/prereq.md) document.

#### Step-by-Step Guide

1. **Create a New C# Console Application**

    Open your terminal or command prompt and run the following command to create a new C# console application:
    ```
    dotnet new console -n AzureAIAgent8
    cd AzureAIAgent8
    ```

1. **Open the project**

    - In Visual Studio, open the AzureAIAgent8.csproj .
    - In Visual Studio Code, open the folder `AzureAIAgent8`. You can use the command `code .` to open the project in VS Code.

1. **Add Necessary NuGet Packages**

    Add the required NuGet packages for Azure AI Agent Service and Azure Identity:
    ```
    dotnet add package Azure.AI.Projects --version 1.0.0-beta.6
    dotnet add package Azure.Identity
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Json
    dotnet add package Microsoft.SemanticKernel.Agents.Abstractions
    dotnet add package Microsoft.SemanticKernel.Agents.Core
    dotnet add package Microsoft.SemanticKernel.Agents.OpenAI --prerelease
    dotnet add package Microsoft.SemanticKernel.Connectors.AzureOpenAI
    ```

1. **Add Application Settings**

    Create a new file named `appsettings.json` in the root of your project and add the following content, Replace the BINGCONNECTIONNAME with your Bing Connection Name:
    ```json
    {
        "AzureAI": {
            "ProjectConnectionString": "<your-connection-string>",
            "ModelName": "<your-model-name>",
            "BingConnectionName": "<your-bing-connection-name>",
            "ModelDeploymentName": "<your-model-deployment-name>",
            "AzureOpenAIEndpoint": "<your-azure-openai-endpoint>"
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

1. **Add Compiler Warning Suppressions**

    The Agent Framework is experimental and requires warning suppression. This may addressed in as a property in the project file (.csproj):
    ```xml
        <PropertyGroup>
            <NoWarn>$(NoWarn);SKEXP0001;SKEXP0010;SKEXP0110</NoWarn>
        </PropertyGroup>
    ```

1. **Create an Azure AI Agent using Bing Grounding Tool**

    In this sample, we will create an Azure AI Agent that usese the Bing Grounding Tool, following the same pattern as used in the previous labs. To do this, create a new file called `SearchAgents.cs` and copy the following code into it:
    ```csharp
    using System.ComponentModel;
    using Azure.AI.Projects;
    using Azure.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.SemanticKernel;

    public sealed class SearchPlugin
    {
        [KernelFunction, Description("Search by Bing")]
        public static async Task<string> Search([Description("search Item")] string searchItem)
        {
            Console.WriteLine($"Searching for: {searchItem}");

            // intialize the return value
            string result = string.Empty;

            // Load environment variables
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Set up the project and agents client
            AIProjectClient projectClient = new AIProjectClient(
                configuration["AzureAI:ProjectConnectionString"],
                new DefaultAzureCredential());
            AgentsClient client = projectClient.GetAgentsClient();

            // Create a connection to the Bing Connection
            ConnectionResponse bingConnection = await projectClient.GetConnectionsClient().GetConnectionAsync(configuration["AzureAI:BingConnectionName"]);
            var bingGroundingTool = new BingGroundingToolDefinition(new ToolConnectionList
            {
                ConnectionList = { new ToolConnection(bingConnection.Id) }
            });

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

            // Create a thread for our interaction with the agent
            AgentThread thread = await client.CreateThreadAsync();

            // Create a message to send to the agent on the created thread
            ThreadMessage message = await client.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                searchItem
            );

            // Process the message with the agent, asynchronously
            ThreadRun run = await client.CreateRunAsync(thread.Id, agent.Id);
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                run = await client.GetRunAsync(thread.Id, run.Id);
            } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
            Console.WriteLine($"Run finished with status: {run.Status}");

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
                    result = lastMessage.Text;
                }
            }

            // Clean up resources
            await client.DeleteThreadAsync(thread.Id);
            await client.DeleteAgentAsync(agent.Id);

            // Return the result
            return result;
        }
    }
    ```

 1. **Create an Azure AI Agent using Code Interpreter Tool**

    In this step, we will create an Azure AI Agent that usese the Code Interpreter Tool, following the same pattern as used in the previous labs. To do this, create a new file called `SaveBlogAgent.cs` and copy the following code into it:
    ```csharp
    using System.ComponentModel;
    using Azure.AI.Projects;
    using Azure.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.SemanticKernel;

    public sealed class SaveBlogAgent
    {
        [KernelFunction, Description("Save blog")]
        public static async Task<string> Save([Description("save blog content")] string content)
        {
            Console.WriteLine($"Saving blog content");
            
            // Load environment variables
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Set up the project client
            AgentsClient client = new AgentsClient(
                configuration["AzureAI:ProjectConnectionString"],
                new DefaultAzureCredential());

            // Create a Code Interpreter tool
            var codeInterpreter = new CodeInterpreterToolDefinition();

            // Create an agent with the Code Interpreter tool
            Agent agent = await client.CreateAgentAsync(
                model: configuration["AzureAI:ModelName"],
                name: "ai-lab-agent5",
                instructions: "You are a helpful agent",
                tools: [codeInterpreter]
            );

            // Create a thread for our interaction with the agent
            AgentThread thread = await client.CreateThreadAsync();

            // Create a message to send to the agent on the created thread
            ThreadMessage message = await client.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                @$"
                    You are my Python programming assistant. Generate code and execute it according to the following requirements:

                    1. Save {content} to blog-{{YYMMDDHHMMSS}}.md
                    2. Give me the download link for this file
                "
            );

            // Process the message with the agent, asynchronously
            ThreadRun run = await client.CreateRunAsync(thread.Id, agent.Id);
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                run = await client.GetRunAsync(thread.Id, run.Id);
            } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
            Console.WriteLine($"Run finished with status: {run.Status}");

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

            // Clean up resources
            await client.DeleteThreadAsync(thread.Id);
            await client.DeleteAgentAsync(agent.Id);

            // Return the result
            return "Saved";
        }
    }
    ```

1. **Start Creating the Semantic Kernel Group Chat**

    Delete the contents of `Program.cs` and import the necessary namespaces for the Azure SDKs at the top of your `Program.cs` file:
    ```csharp
    using Azure.Identity;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
    using Microsoft.Extensions.Configuration;
    using Microsoft.SemanticKernel.Agents.Chat;
    ```

    Explanation: These namespaces are required for accessing Azure services, handling kernel functionality, and managing HTTP requests and responses.

1. **Load Configuration Settings**

    Load the configuration settings from `appsettings.json` and user secrets in your `Program.cs` file:
    ```csharp
    // Load environment variables
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    ```

1. **Initialize the Semantic Kernel**

    Create a new instance of the Semantic Kernel and configure it to use Azure OpenAI:
    ```csharp
    // Initialize the Semantic Kernel with Azure OpenAI
    var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(
            configuration["AzureAI:ModelDeploymentName"] ?? throw new ArgumentNullException("AzureAI:ModelDeploymentName"),
            configuration["AzureAI:AzureOpenAIEndpoint"] ?? throw new ArgumentNullException("AzureAI:AzureOpenAIEndpoint"),
            new DefaultAzureCredential())
        .Build();
    ```

1. **Create the Search Agent**

    Create the search agent using the `SearchPlugin` class and configure it with the SearchAgent tool as a plugin:
    ```csharp
    // Set up the Azure AI Agent as a Semantic Kernel assistant agent
    ChatCompletionAgent search_agent = new()
    {
        Name = "SearchAgent",
        Instructions = "You are a search expert, help me use tools to find relevant knowledge.",
        Kernel = kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };
    KernelPlugin search_plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
    search_agent.Kernel.Plugins.Add(search_plugin);
    ```

1. **Create the SaveBlog Agent**

    Create the save blog agent using the `SaveBlogAgent` class and configure it with the Code Interpreter tool as a plugin:
    ```csharp
    // Set up the save blog Azure AI Agent as a Semantic Kernel assistant agent
    ChatCompletionAgent save_blog_agent = new()
    {
        Name = "SaveBlogAgent",
        Instructions = "Save blog content. Respond with 'Saved' when your blog is saved.",
        Kernel = kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };
    KernelPlugin save_blog_plugin = KernelPluginFactory.CreateFromType<SaveBlogAgent>();
    save_blog_agent.Kernel.Plugins.Add(save_blog_plugin);
    ```

1. **Create the Write Agent**

    Create the write agent to orchestrate the search and save blog agents:
    ```csharp
    // Set up the write blog Semantic Kernel agent
    ChatCompletionAgent write_blog_agent = new()
    {
        Name = "WriteBlog",
        Instructions = "You are a blog writer, please help me write a blog based on bing search content.",
        Kernel = kernel
    };

1. **Create the Group Chat**

    Create a group chat with the search agent, write agent, and save blog agents, and reference a termination strategy that we will create later.
    ```csharp
    // Create a Semantic Kernel group chat with the agents
    AgentGroupChat chat =
        new(search_agent, write_blog_agent, save_blog_agent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy =
                        new ApprovalTerminationStrategy()
                        {
                            // Only the save blog agent can terminate.
                            Agents = [save_blog_agent],
                            // Limit total number of turns
                            MaximumIterations = 10,
                        }
            }
        };
    ```

1. **Prepare a Message to Start the Chat**

    Prepare the instructions for the agent group chat to work on:
    ```csharp
    // Prepare the message for the agent group chat
    ChatMessageContent input = new(AuthorRole.User, """
                I am writing a blog about GraphRAG. Search for the following 2 questions and write a blog based on the search results ,save it           
                    1. What is Microsoft GraphRAG?
                    2. Vector-based RAG vs GraphRAG
                """);
    chat.AddChatMessage(input);
    ```

1. **Run the Group Chat**

    Run the group chat and wait for the results:
    ```csharp
    // Process the message with the agent group chat, asynchronously and output the results
    await foreach (ChatMessageContent content in chat.InvokeAsync())
    {
        Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
    }
    ```

1. **Define the Termination Strategy**

    Define the termination strategy for the group chat. In this case, we will use an approval strategy that allows only the save blog agent to terminate the chat:
    ```csharp
    internal sealed class ApprovalTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "Saved"
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("Saved", StringComparison.OrdinalIgnoreCase) ?? false);
    }
    ```

1. **Run the Application**

    Save the changes and run your application using the following command:
    ```
    dotnet run
    ```


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
