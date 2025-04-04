using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

// Load environment variables
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

// Set up the project client
AgentsClient client = new AgentsClient(
    configuration["AzureAI:ProjectConnectionString"],
    new DefaultAzureCredential());

// Upload the local file to Azure
AgentFile uploadedAgentFile = await client.UploadFileAsync(
    filePath: "../data/intro_rag.md",
    purpose: AgentFilePurpose.Agents
);

// Create a vector store with the uploaded file
VectorStore vectorStore = await client.CreateVectorStoreAsync(
    fileIds: new List<string> { uploadedAgentFile.Id },
    name: "sample_vector_store"
);
Console.WriteLine($"Created vector store, vector store ID: {vectorStore.Id}");

// Create a File Search tool, using the vector store as a data source
FileSearchToolDefinition fileSearchTool = new FileSearchToolDefinition();
FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);

// Create an agent with the File Search tool
Agent agent = await client.CreateAgentAsync(
    model: configuration["AzureAI:ModelName"],
    name: "ai-lab-agent6",
    instructions: "You are a helpful agent",
    tools: [fileSearchTool],
    toolResources: new ToolResources() { FileSearch = fileSearchToolResource }
);

// Create a thread for our interaction with the agent
AgentThread thread = await client.CreateThreadAsync();

// Create a message to send to the agent on the created thread
ThreadMessage message = await client.CreateMessageAsync(
    thread.Id,
    MessageRole.User,
    @"
        What is GraphRAG?
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
    }
}

// Clean up resources
await client.DeleteVectorStoreAsync(vectorStore.Id);
await client.DeleteThreadAsync(thread.Id);
await client.DeleteAgentAsync(agent.Id);
