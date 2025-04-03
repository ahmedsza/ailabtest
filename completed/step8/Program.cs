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

internal class CustomHeadersPolicy : HttpPipelineSynchronousPolicy
{
    public override void OnSendingRequest(HttpMessage message)
    {
        message.Request.Headers.Add("x-ms-enable-preview", "true");
    }
}

public sealed class SearchPlugin
{
    [KernelFunction, Description("Search by Bing")]
    public async Task<string> Search([Description("search Item")] string searchItem)
    {
        // Implementation of the search functionality will go here
        var connectionString = "ENTER_AZUREAI_PROJECT_CONNECTION_STRING";
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
        // can replace the string here
        Azure.Response<ThreadMessage> messageResponse = await agentClient.CreateMessageAsync(
             thread.Id,
             MessageRole.User,
             "How does wikipedia explain Euler's Identity?");

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

public partial class Program
{
    const string HostName = "SeachAssistant";
    const string HostInstructions = "Search information ";
    public static async Task Main(string[] args)
    {
        var deployment = "gpt-4o";
        var endpoint = "ENTER_ENDPOINT";
        var key = "ENTER_AZUREAI_KEY";
        var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
        .Build();

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

        KernelPlugin plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
        agent.Kernel.Plugins.Add(plugin);

        ChatHistory chat = new ChatHistory();
        var input = "PUT SOME INPUT HERE";
        chat.Add(new ChatMessageContent(AuthorRole.User, input));

        var agentContent = agent.InvokeAsync(chat);
        await foreach (var message in agentContent)
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            Console.WriteLine($"# {message.AuthorName}: '{message.Content}'");
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

}

