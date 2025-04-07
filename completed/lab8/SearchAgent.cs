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