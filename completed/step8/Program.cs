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

public partial class Program
{
    const string HostName = "SeachAssistant";
    const string HostInstructions = "Search information ";

    // Main method and other code will go here
    public static async Task Main(string[] args)
    {
        var deployment = "gpt-4o";
        var endpoint = "Your AOAI endpoint";
        var key = "Your AOAI Key";
        var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
        .Build();

        const string SearchHostName = "Search";
        const string SearchHostInstructions = "You are a search expert, help me use tools to find relevant knowledge";
#pragma warning disable SKEXP0110

        ChatCompletionAgent search_agent =
                    new()
                    {
                        Name = SearchHostName,
                        Instructions = SearchHostInstructions,
                        Kernel = kernel,
                        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
                    };
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

        const string WriteBlogName = "WriteBlog";
        const string WriteBlogInstructions =
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

#pragma warning disable SKEXP0110

        KernelPlugin search_plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
        search_agent.Kernel.Plugins.Add(search_plugin);

#pragma warning disable SKEXP0110

        KernelPlugin save_blog_plugin = KernelPluginFactory.CreateFromType<SavePlugin>();
        save_blog_agent.Kernel.Plugins.Add(save_blog_plugin);

#pragma warning disable SKEXP0110

        AgentGroupChat chat =
                    new(search_agent, write_blog_agent, save_blog_agent)
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
#pragma warning disable SKEXP0110

        ChatMessageContent input = new(AuthorRole.User, """
                    I am writing a blog about GraphRAG. Search for the following 2 questions and write an Afrikaans blog based on the search results ,save it           
                        1. What is Microsoft GraphRAG?
                        2. Vector-based RAG vs GraphRAG
                    """);
        chat.AddChatMessage(input);

#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001



        await foreach (ChatMessageContent content in chat.InvokeAsync())
        {
            Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
        }
        // code comes below 
    }
}

// ApprovalTerminationStrategy class will go here

#pragma warning disable SKEXP0110

internal sealed class ApprovalTerminationStrategy : TerminationStrategy
{
    // Terminate when the final message contains the term "approve"
    protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("Saved", StringComparison.OrdinalIgnoreCase) ?? false);
}
// Custom headers policy class will go here
internal class CustomHeadersPolicy : HttpPipelineSynchronousPolicy
{
    public override void OnSendingRequest(HttpMessage message)
    {
        message.Request.Headers.Add("x-ms-enable-preview", "true");
    }
}
// Search plugin class will go here
public sealed class SearchPlugin
{
    [KernelFunction, Description("Search by Bing")]
    public async Task<string> Search([Description("search Item")] string searchItem)
    {
        // Implementation of the search functionality will go here
        var connectionString = "Your Azure AI Agent Service Connection String";
        var clientOptions = new AIProjectClientOptions();
        clientOptions.AddPolicy(new CustomHeadersPolicy(), HttpPipelinePosition.PerCall);
        var projectClient = new AIProjectClient(connectionString, new DefaultAzureCredential(), clientOptions);
        var BING_CONNECTION_NAME = "ENTER_BING_CONNECTION_NAME";
        ConnectionResponse bingConnection = await projectClient.GetConnectionsClient().GetConnectionAsync(BING_CONNECTION_NAME);
        var connectionId = bingConnection.Id;
        AgentsClient agentClient = projectClient.GetAgentsClient();
        ToolConnectionList connectionList = new ToolConnectionList { ConnectionList = { new ToolConnection(connectionId) } };
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
            searchItem);
        Azure.Response<ThreadRun> runResponse = await agentClient.CreateRunAsync(thread, agent);
        // Poll until completion
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

            if (threadMessage.Role.ToString().ToLower() == "assistant")
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
// Save plugin class will go here

public sealed class SavePlugin
{
    [KernelFunction, Description("Save blog")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public async Task<string> Save([Description("save blog content")] string content)
    {
        // Implementation details will follow
        Console.Write("###" + content);
        var connectionString = "YOUR_CONNECTION_STRING";
        AgentsClient client = new AgentsClient(connectionString, new DefaultAzureCredential());
        Azure.Response<Azure.AI.Projects.Agent> agentResponse = await client.CreateAgentAsync(
            model: "gpt-4o",
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
                        System.IO.Directory.CreateDirectory("./blog");
                        using System.IO.FileStream stream = System.IO.File.OpenWrite($"./blog/{mdfile}");
                        fileBytes.Value.ToStream().CopyTo(stream);
                    }
                }
            }
        }
        return "Saved";

    }
}


